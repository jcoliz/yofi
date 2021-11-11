using Ardalis.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.AspNet.Controllers
{
    [Produces("application/json")]
    [Route("ajax/payee")]
    public class AjaxPayeeController: Controller
    {
        private readonly IPayeeRepository _repository;

        public AjaxPayeeController(IPayeeRepository repository) 
        {
            _repository = repository;
        }

        [HttpPost("select/{id}")]
        [ValidateAntiForgeryToken]
        [ValidatePayeeExists]
        public async Task<IActionResult> Select(int id, bool value)
        {
            var payee = await _repository.GetByIdAsync(id);
            payee.Selected = value;
            await _repository.UpdateAsync(payee);

            return new OkResult();
        }

        [HttpPost("add")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateModel]
        public async Task<IActionResult> Add([Bind("Name,Category")] Payee payee)
        {
            await _repository.AddAsync(payee);
            return new ObjectResult(payee);
        }

        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateModel]
        [ValidatePayeeExists]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Name,Category")] Payee payee)
        {
            await _repository.UpdateAsync(payee);
            return new ObjectResult(payee);
        }
    }
}
