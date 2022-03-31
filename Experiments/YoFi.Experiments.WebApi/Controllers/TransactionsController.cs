using Microsoft.AspNetCore.Mvc;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Core.Repositories.Wire;

namespace YoFi.Experiments.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ILogger<WeatherForecastController> _logger;
    private readonly ITransactionRepository _repository;

    public TransactionsController(ILogger<WeatherForecastController> logger, ITransactionRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [HttpGet]
    public async Task<IWireQueryResult<Transaction>> Get([FromQuery] WireQueryParameters parameters)
    {
        return await _repository.GetByQueryAsync(parameters);
    }

    [HttpGet("{id}")]
    public async Task<Transaction> Get(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    [HttpDelete("{id}")]
    public async Task Delete(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        await _repository.RemoveAsync(item);
    }

    [HttpPost]
    public async Task<int> Create([Bind("Timestamp,Amount,Memo,Payee,Category,BankReference")] Transaction transaction )
    {
        await _repository.AddAsync(transaction);
        return transaction.ID;
    }

    [HttpPut("{id}")]
    public async Task Edit(int id, [Bind("Timestamp,Amount,Memo,Payee,Category,BankReference")] Transaction transaction )
    {
        var item = await _repository.GetByIdAsync(id);
        transaction.ID = id;
        await _repository.UpdateAsync(transaction);
    }

}
