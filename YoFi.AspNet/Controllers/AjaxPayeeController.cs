using Ardalis.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.AspNet.Controllers
{
    [Produces("application/json")]
    [Route("ajax/payee")]
    public class AjaxPayeeController: Controller
    {
        #region Fields
        private readonly IPayeeRepository _repository;
        #endregion

        #region Constructor
        public AjaxPayeeController(IPayeeRepository repository) 
        {
            _repository = repository;
        }
        #endregion

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
            return new ObjectResult(new ApiResult(payee));
        }

        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateModel]
        [ValidatePayeeExists]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Name,Category")] Payee payee)
        {
            await _repository.UpdateAsync(payee);
            return new ObjectResult(new ApiResult(payee));
        }
    }
}
