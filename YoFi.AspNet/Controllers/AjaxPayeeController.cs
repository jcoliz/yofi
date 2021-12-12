using Ardalis.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.AspNet.Controllers
{
    /// <summary>
    /// Fulfill AJAX requests related to Payees
    /// </summary>
    [Produces("application/json")]
    [Route("ajax/payee")]
    public class AjaxPayeeController: Controller
    {
        /// <summary>
        /// Repository where we can take our actions
        /// </summary>
        private readonly IPayeeRepository _repository;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="repository"></param>
        public AjaxPayeeController(IPayeeRepository repository) 
        {
            _repository = repository;
        }

        /// <summary>
        /// Update the selection state for item #<paramref name="id"/>
        /// </summary>
        /// <param name="id">Which item</param>
        /// <param name="value">New selection state</param>
        [HttpPost("select/{id}")]
        [Authorize(Policy = "CanWrite")]
        [ValidateAntiForgeryToken]
        [ValidatePayeeExists]
        public async Task<IActionResult> Select(int id, bool value)
        {
            var payee = await _repository.GetByIdAsync(id);
            payee.Selected = value;
            await _repository.UpdateAsync(payee);

            return new OkResult();
        }

        /// <summary>
        /// Add a new item
        /// </summary>
        /// <param name="payee">Item to add</param>
        [HttpPost("add")]
        [Authorize(Policy = "CanWrite")]
        [ValidateAntiForgeryToken]
        [ValidateModel]
        public async Task<IActionResult> Add([Bind("Name,Category")] Payee payee)
        {
            await _repository.AddAsync(payee);
            return new ObjectResult(payee);
        }

        /// <summary>
        /// Update (edit) item #<paramref name="id"/> to new values
        /// </summary>
        /// <param name="id">Which item</param>
        /// <param name="payee">New values</param>
        [HttpPost("edit/{id}")]
        [Authorize(Policy = "CanWrite")]
        [ValidateAntiForgeryToken]
        [ValidateModel]
        [ValidatePayeeExists]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Name,Category")] Payee payee)
        {
            await _repository.UpdateAsync(payee);
            return new ObjectResult(payee);
        }
    }
}
