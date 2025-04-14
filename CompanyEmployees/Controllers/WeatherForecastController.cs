using Contracts;
using Microsoft.AspNetCore.Mvc;

namespace CompanyEmployees.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILoggerManager _logger;
        private readonly IRepositoryManager _repository;

        public WeatherForecastController(ILoggerManager logger, IRepositoryManager repository)
        {
            _logger = logger;
            _repository = repository;
        }

        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            _logger.LogInfo("¬от информационное сообщение от нашего контроллера значений.");
            _logger.LogDebug("¬от отладочное сообщение от нашего контроллера значений.");
            _logger.LogWarn("¬от сообщение предупреждени€ от нашего контроллера значений.");
            _logger.LogError("¬от сообщение об ошибке от нашего контроллера значений.");

            return new string[] { "value1", "value2" };
        }
    }
}