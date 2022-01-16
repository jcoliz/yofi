using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.AspNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YoFi.Core;
using YoFi.Core.Importers;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.AspNet.Pages
{
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanRead")]
    public class ImportModel : PageModel
    {
        public const int PageSize = 25;
        private ITransactionRepository _repository;
        private IAsyncQueryExecution _queryExecution;
        private IAuthorizationService _authorizationService;

        public PageDivider Divider { get; private set; } = new PageDivider() { PageSize = PageSize };

        public IEnumerable<Transaction> Transactions { get; private set; } = Enumerable.Empty<Transaction>();

        public HashSet<int> Highlights { get; private set; } = new HashSet<int>();

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
                    return RedirectToAction(nameof(Index));
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

        public async Task<IActionResult> OnPostUploadAsync(List<IFormFile> files, [FromServices] TransactionImporter importer)
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

            // Prepare the visual results
            await OnGetAsync();

            // Set the highlights
            Highlights = importer.HighlightIDs.ToHashSet();

            return Page();
        }
    }
}
