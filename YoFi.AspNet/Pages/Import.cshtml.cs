using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.AspNet;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YoFi.Core;
using YoFi.Core.Importers;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Core.SampleGen;

namespace YoFi.AspNet.Pages
{
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanRead")]
    public class ImportModel : PageModel
    {
        public const int PageSize = 25;
        public const int MaxOtherItemsToShow = 10;
        private readonly ITransactionRepository _repository;
        private readonly IAsyncQueryExecution _queryExecution;
        private readonly IAuthorizationService _authorizationService;

        public PageDivider Divider { get; private set; } = new PageDivider() { PageSize = PageSize };

        public IEnumerable<Transaction> Transactions { get; private set; } = Enumerable.Empty<Transaction>();

        public IEnumerable<BudgetTx> BudgetTxs { get; private set; } = Enumerable.Empty<BudgetTx>();
        public IEnumerable<Payee> Payees { get; private set; } = Enumerable.Empty<Payee>();
        public int NumBudgetTxsUploaded { get; private set; }
        public int NumPayeesUploaded { get; private set; }

        public HashSet<int> Highlights { get; private set; } = new HashSet<int>();

        public string Error { get; private set; }

        public ImportModel(ITransactionRepository repository, IAsyncQueryExecution queryExecution, IAuthorizationService authorizationService)
        {
            _repository = repository;
            _queryExecution = queryExecution;
            _authorizationService = authorizationService;
        }

        public async Task<IActionResult> OnGetAsync(int? p = null)
        {
            // TODO: Should add a DTO here
            IQueryable<Transaction> result = _repository.OrderedQuery.Where(x => x.Imported == true);

            //
            // Process PAGE (P) parameters
            //

            result = await Divider.ItemsForPage(result, p);

            Transactions = await _queryExecution.ToListNoTrackingAsync(result);

            return Page();
        }

        public async Task<IActionResult> OnPostGoAsync(string command)
        {
            try
            {
                if (string.IsNullOrEmpty(command))
                    throw new ArgumentException();

                // Sadly we cannot do "Authorize" filters on Page Hanlders. So we have to do this ourselves.
                // https://stackoverflow.com/questions/43231535/how-to-validate-user-agains-policy-in-code-in-aspnet-core
                var canwrite = await _authorizationService.AuthorizeAsync(User, "CanWrite");
                if (!canwrite.Succeeded)
                    throw new UnauthorizedAccessException();

                if (command == "cancel")
                {
                    await _repository.CancelImportAsync();
                }
                else if (command == "ok")
                {
                    await _repository.FinalizeImportAsync();
                    return RedirectToAction(nameof(Index),"Transactions");
                }
                else
                    throw new ArgumentException();
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
            catch (UnauthorizedAccessException)
            {
                // This more directly mimics what the authorize filter would have done
                return RedirectToPage("/Account/AccessDenied", new { area = "Identity" });
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUploadAsync(List<IFormFile> files, [FromServices] UniversalImporter importer)
        {
            try
            {
                // Sadly we cannot do "Authorize" filters on Page Hanlders. So we have to do this ourselves.
                // https://stackoverflow.com/questions/43231535/how-to-validate-user-agains-policy-in-code-in-aspnet-core
                var canwrite = await _authorizationService.AuthorizeAsync(User, "CanWrite");
                if (!canwrite.Succeeded)
                    // This more directly mimics what the authorize filter would have done
                    return RedirectToPage("/Account/AccessDenied", new { area = "Identity" });

                // Open each file in turn, and send them to the importer

                foreach (var formFile in files)
                {
                    using var stream = formFile.OpenReadStream();

                    var filetype = Path.GetExtension(formFile.FileName).ToLowerInvariant();

                    if (filetype == ".ofx")
                        await importer.QueueImportFromOfxAsync(stream);
                    else if (filetype == ".xlsx")
                        importer.QueueImportFromXlsx(stream);
                }

                // Process the imported files
                await importer.ProcessImportAsync();
                BudgetTxs = importer.ImportedBudgetTxs.Take(MaxOtherItemsToShow);
                Payees = importer.ImportedPayees.Take(MaxOtherItemsToShow);
                NumBudgetTxsUploaded = importer.ImportedBudgetTxs.Count();
                NumPayeesUploaded = importer.ImportedPayees.Count();

                // Prepare the visual results
                await OnGetAsync();

                // Set the highlights
                Highlights = importer.HighlightIDs.ToHashSet();

            }
            catch (Exception ex)
            {
                Error = $"The import failed. This error was given: {ex.GetType().Name}: {ex.Message}";
            }

            return Page();
        }

        public async Task<IActionResult> OnGetSampleAsync(string what, string how, [FromServices] IWebHostEnvironment e)
        {
            var dir = e.WebRootPath;
            // Load the full sample data off disk
            var instream = System.IO.File.OpenRead($"{dir}/sample/SampleData-Full.xlsx");

            IActionResult result = NotFound();
            if ("all" == what)
            {
                // Just return it!
                result = File(instream, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName: $"{what}.xlsx");
            }
            else if ("budget" == what)
            {
                // Load in just the budget into memory
                using var ssr = new SpreadsheetReader();
                ssr.Open(instream);
                var items = ssr.Deserialize<BudgetTx>();

                // Then write that back out
                var stream = new MemoryStream();
                using (var ssw = new SpreadsheetWriter())
                {
                    ssw.Open(stream);
                    ssw.Serialize(items);
                }
                stream.Seek(0, SeekOrigin.Begin);
                result = File(stream, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName: $"{what}.xlsx");
            }
            else if ("payees" == what)
            {
                // Load in just the payees into memory
                using var ssr = new SpreadsheetReader();
                ssr.Open(instream);
                var items = ssr.Deserialize<Payee>();

                // Then write that back out
                var stream = new MemoryStream();
                using (var ssw = new SpreadsheetWriter())
                {
                    ssw.Open(stream);
                    ssw.Serialize(items);
                }
                stream.Seek(0, SeekOrigin.Begin);
                result = File(stream, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName: $"{what}.xlsx");
            }
            else
            {
                // At this point, only transactions are of interest
                // Load in just the transactions and splits into memory
                using var ssr = new SpreadsheetReader();
                ssr.Open(instream);
                var txs = ssr.Deserialize<Transaction>();
                var splits = ssr.Deserialize<Split>();

                if (int.TryParse(what, out var month))
                {
                    // Narrow down to the required month
                    var outtxs = txs.Where(x => x.Timestamp.Month == month);
                    var outtxids = outtxs.Where(x=>x.ID > 0).Select(x => x.ID).ToHashSet();
                    var outsplits = splits.Where(x=>outtxids.Contains(x.TransactionID));

                    if ("xlsx" == how)
                    {
                        // Then write that back out
                        var stream = new MemoryStream();
                        using (var ssw = new SpreadsheetWriter())
                        {
                            ssw.Open(stream);
                            ssw.Serialize(outtxs);
                            ssw.Serialize(outsplits);
                        }
                        stream.Seek(0, SeekOrigin.Begin);
                        var monthname = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
                        result = File(stream, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName: $"{month:D2}-{monthname}.{how}");

                    }
                    else if ("ofx" == how)
                    {
                        // Write it as an OFX
                        var stream = new MemoryStream();
                        SampleDataOfx.WriteToOfx(outtxs, stream);

                        stream.Seek(0, SeekOrigin.Begin);
                        var monthname = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
                        result = File(stream, contentType: "application/ofx", fileDownloadName: $"{month:D2}-{monthname}.{how}");
                    }
                }
            }

            return result;
        }
    }
}
