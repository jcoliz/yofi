using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfxWeb.Asp.Data;

namespace OfxWeb.Asp.Controllers
{
    [Produces("application/json")]
    [Route("api/tx")]
    public class ApiController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Api
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Api/5
        [HttpGet("{id}", Name = "Get")]
        public async Task<string> Get(int id)
        {
            var transaction = await _context.Transactions
                .SingleOrDefaultAsync(m => m.ID == id);
            if (transaction == null)
            {
                return JsonConvert.SerializeObject(new KeyNotFoundException());
            }

            return JsonConvert.SerializeObject(transaction);
        }

        // GET: Transactions/ApplyPayee/5
        [HttpGet("ApplyPayee/{id}")]
        public async Task<string> ApplyPayee(int id)
        {
            var transaction = await _context.Transactions.SingleOrDefaultAsync(m => m.ID == id);
            if (transaction == null)
            {
                return JsonConvert.SerializeObject(new KeyNotFoundException("No such transaction"));
            }

            // Handle payee auto-assignment

            // See if the payee exists
            var payee = await _context.Payees.FirstOrDefaultAsync(x => transaction.Payee.Contains(x.Name));

            if (payee == null)
                return JsonConvert.SerializeObject(new KeyNotFoundException("Payee unknown"));

            transaction.Category = payee.Category;
            transaction.SubCategory = payee.SubCategory;
            _context.Update(transaction);
            await _context.SaveChangesAsync();

            return JsonConvert.SerializeObject(payee);
        }

        // GET: Transactions/Hide/5
        [HttpGet("Hide/{id}")]
        public async Task<string> Hide(int id)
        {
            var transaction = await _context.Transactions.SingleOrDefaultAsync(m => m.ID == id);
            if (transaction == null)
            {
                return JsonConvert.SerializeObject(new KeyNotFoundException("No such transaction"));
            }

            transaction.Hidden = true;
            _context.Update(transaction);
            await _context.SaveChangesAsync();

            return JsonConvert.SerializeObject("OK");
        }

        // GET: Transactions/Show/5
        [HttpGet("Show/{id}")]
        public async Task<string> Show(int id)
        {
            var transaction = await _context.Transactions.SingleOrDefaultAsync(m => m.ID == id);
            if (transaction == null)
            {
                return JsonConvert.SerializeObject(new KeyNotFoundException("No such transaction"));
            }

            transaction.Hidden = false;
            _context.Update(transaction);
            await _context.SaveChangesAsync();

            return JsonConvert.SerializeObject("OK");
        }

        // POST: Transactions/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost("Edit/{id}")]
        public async Task<string> Edit(int id, bool? duplicate, [Bind("ID,Timestamp,Amount,Memo,Payee,Category,SubCategory,BankReference")] Models.Transaction transaction)
        {
            if (id != transaction.ID && duplicate != true)
            {
                return JsonConvert.SerializeObject(new Exception("not found"));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (duplicate == true)
                    {
                        transaction.ID = 0;
                        _context.Add(transaction);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        _context.Update(transaction);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    return JsonConvert.SerializeObject(ex);
                }
                return JsonConvert.SerializeObject("OK");
            }
            else
                return JsonConvert.SerializeObject(new Exception("invalid"));
        }


        // POST: api/Api
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }
        
        // PUT: api/Api/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }
        
        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
