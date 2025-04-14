using Contracts;
using Microsoft.AspNetCore.Mvc;

namespace CompanyEmployees.Controllers;

[Route("[controller]")]
[ApiController]
public class WeatherForecastController : ControllerBase
{
    private ILoggerManager _logger;
    public WeatherForecastController(ILoggerManager logger)
    {

        _logger = logger;
    }
    [HttpGet]
    public IEnumerable<string> Get()
    {
        _logger.LogInfo("¬от информационное сообщение от нашего контроллеразначений.");

        _logger.LogDebug("¬от отладочное сообщение от нашего контроллеразначений.");

        _logger.LogWarn("¬от сообщение предупреждени€ от нашего контроллера значений.");

        _logger.LogError("¬от сообщение об ошибке от нашего контроллеразначений.");
        return new string[] { "value1", "value2" };
    }
}