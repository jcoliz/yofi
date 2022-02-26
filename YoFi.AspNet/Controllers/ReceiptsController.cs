using Common.AspNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Repositories;

namespace YoFi.AspNet.Controllers
{
    [Authorize("CanRead")]
    public class ReceiptsController: Controller
    {
        private readonly IReceiptRepository _repository;

        public ReceiptsController(IReceiptRepository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var qresult = await _repository.GetAllAsync();
                return View(qresult);

            }
            catch
            {
                throw;
            }
        }

        [ValidateReceiptExists]
        public async Task<IActionResult> Details(int id)
        {
            var receipt = await _repository.GetByIdAsync(id);
            return PartialView(receipt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateReceiptExists]
        public async Task<IActionResult> Delete(int id)
        {
            await _repository.DeleteAsync(await _repository.GetByIdAsync(id));
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateReceiptExists]
        public async Task<IActionResult> Accept(int id)
        {
            var receipt = await _repository.GetByIdAsync(id);

            if (!receipt.Matches.Any())
                return BadRequest();

            await _repository.AssignReceipt(receipt,receipt.Matches.First());

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> AcceptAll()
        {
            await _repository.AssignAll();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateFilesProvided(multiplefilesok: true)]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
            foreach (var file in files)
            {
                using var stream = file.OpenReadStream();
                await _repository.UploadReceiptAsync(file.FileName, stream, file.ContentType);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
