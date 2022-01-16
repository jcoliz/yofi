using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.AspNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YoFi.Core;
using YoFi.Core.Importers;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.AspNet.Pages
{
    public class ImportModel : PageModel
    {
        public const int PageSize = 25;
        private ITransactionRepository _repository;
        private IAsyncQueryExecution _queryExecution;

        public PageDivider Divider { get; private set; } = new PageDivider() { PageSize = PageSize };

        public IEnumerable<Transaction> Transactions { get; private set; } = Enumerable.Empty<Transaction>();

        public ImportModel(ITransactionRepository repository, IAsyncQueryExecution queryExecution)
        {
            _repository = repository;
            _queryExecution = queryExecution;
        }

        public async Task OnGetAsync(int? p = null)
        {
            // TODO: Should add a DTO here
            IQueryable<Transaction> result = _repository.OrderedQuery.Where(x => x.Imported == true);

            //
            // Process PAGE (P) parameters
            //

            result = await Divider.ItemsForPage(result, p);

#if false
            try
            {
                if (!string.IsNullOrEmpty(highlight))
                {
                    ViewData["Highlight"] = highlight.Split(':').Select(x => Convert.ToInt32(x)).ToHashSet();
                }
            }
            catch
            {
                // If this fails in any way, nevermind.
            }
#endif
            Transactions = await _queryExecution.ToListNoTrackingAsync(result);
        }

        public async Task<IActionResult> OnPostGoAsync(string command)
        {
            try
            {
                if (string.IsNullOrEmpty(command))
                    throw new ArgumentException();

                if (command == "cancel")
                {
                    await _repository.CancelImportAsync();
                }
                else if (command == "ok")
                {
                    await _repository.FinalizeImportAsync();
                    return RedirectToAction(nameof(Index));
                }
                else
                    throw new ArgumentException();
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUploadAsync(List<IFormFile> files, [FromServices] TransactionImporter importer)
        {
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

            // Prepare the visual results
            await OnGetAsync();

            return Page();
        }
    }
}
