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
using YoFi.Core.Repositories.Wire;
using YoFi.Core.SampleData;

namespace YoFi.AspNet.Pages
{
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanRead")]
    public class ImportModel : PageModel
    {
        public const int MaxOtherItemsToShow = 10;
        private readonly ITransactionRepository _repository;
        private readonly IAuthorizationService _authorizationService;
        private readonly ISampleDataProvider _loader;

        public IWireQueryResult<Transaction> Transactions { get; private set; }
        public IEnumerable<ISampleDataDownloadOffering> Offerings { get; private set; }

        public IEnumerable<BudgetTx> BudgetTxs { get; private set; } = Enumerable.Empty<BudgetTx>();
        public IEnumerable<Payee> Payees { get; private set; } = Enumerable.Empty<Payee>();
        public int NumBudgetTxsUploaded { get; private set; }
        public int NumPayeesUploaded { get; private set; }

        public HashSet<int> Highlights { get; private set; } = new HashSet<int>();

        public string Error { get; private set; }

        public ImportModel(ITransactionRepository repository, IAuthorizationService authorizationService, ISampleDataProvider loader)
        {
            _repository = repository;
            _authorizationService = authorizationService;
            _loader = loader;
        }

        public async Task<IActionResult> OnGetAsync(int? p = null)
        {
            // TODO: Should add a DTO here
            Transactions = await _repository.GetByQueryAsync(new WireQueryParameters() { Query = "i=1", Page = p, View = "h" } );

            Offerings = await _loader.ListDownloadOfferingsAsync();

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

        public async Task<IActionResult> OnGetSampleAsync(string what) //, string how, [FromServices] IWebHostEnvironment e)
        {
            IActionResult result = NotFound();
            try
            {
                var offerings = await _loader.ListDownloadOfferingsAsync();

                var offering = offerings.Where(x => x.ID == what).Single();
                if (null != offering)
                {
                    var stream = await _loader.DownloadSampleDataAsync(what);
                    if (offering.FileType == SampleDataDownloadFileType.XLSX)
                        result = File(stream, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName: $"{offering.Description}.{offering.FileType}");
                    else if (offering.FileType == SampleDataDownloadFileType.OFX)
                        result = File(stream, contentType: "application/ofx", fileDownloadName: $"{offering.Description}.{offering.FileType}");
                    // else not found
                }
                // else not found
            }
            catch (Exception)
            {
                result = BadRequest();
            }

            return result;
        }
    }
}
