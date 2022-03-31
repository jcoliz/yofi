using Microsoft.AspNetCore.Mvc;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Core.Repositories.Wire;

namespace YoFi.Experiments.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class TransactionController : ControllerBase
{
    private readonly ILogger<WeatherForecastController> _logger;
    private readonly ITransactionRepository _repository;

    public TransactionController(ILogger<WeatherForecastController> logger, ITransactionRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [HttpGet]
    public async Task<IWireQueryResult<Transaction>> Get()
    {
        return await _repository.GetByQueryAsync(new WireQueryParameters());
    }
}
