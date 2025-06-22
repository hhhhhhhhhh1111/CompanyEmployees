using AutoMapper;
using CompanyEmployees.ActionFilters;
using Contracts;
using Entities.DataTransferObjects;
using Entities.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Entities.RequestFeatures;
using Newtonsoft.Json;

namespace CompanyEmployees.Controllers
{
    [Route("api/companies/{companyId}/employees")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly IRepositoryManager _repository;
        private readonly ILoggerManager _logger;
        private readonly IMapper _mapper;
        private readonly IDataShaper<EmployeeDto> _dataShaper;
        public EmployeesController(IRepositoryManager repository, ILoggerManager logger, IMapper mapper, IDataShaper<EmployeeDto> dataShaper)
        {
            _repository = repository;
            _logger = logger;
            _mapper = mapper;
            _dataShaper = dataShaper;
        }
        /// <summary>
        /// Возвращает список сотрудников для указанной компании с поддержкой фильтрации, сортировки и пагинации
        /// </summary>
        /// <param name="companyId"></param>.
        /// <param name="employeeParameters">/param>.
        /// <returns>Список сотрудников</returns>.
        /// <response code="400">Если максимальный возраст меньше минимального</response>.
        /// <response code="404"> Если компания не найдена </response>.
        [HttpGet]
        [HttpHead]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetEmployeesForCompany(Guid companyId, [FromQuery] EmployeeParameters employeeParameters)
        {
            if (!employeeParameters.ValidAgeRange)
            return BadRequest("Max age can't be less than min age.");
            var company = await _repository.Company.GetCompanyAsync(companyId, trackChanges: false);
            if (company == null)
            {
                _logger.LogInfo($"Company with id: {companyId} doesn't exist in the database.");
                return NotFound();
            }
            var employeesFromDb = await _repository.Employee.GetEmployeesAsync(companyId, employeeParameters, trackChanges: false);
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(employeesFromDb.MetaData));
            var employeesDto = _mapper.Map<IEnumerable<EmployeeDto>>(employeesFromDb);
            return Ok(_dataShaper.ShapeData(employeesDto, employeeParameters.Fields));
        }
        /// <summary>
        /// Возвращает сотрудника по ID по компании
        /// </summary>
        /// <param name="companyId"></param>.
        /// <param name="id"></param>.
        /// <returns>Сотрудник с указанным ID</returns>.
        /// <response code="404">Если компания или сотрудник не найдены</response>.
        [HttpGet("{id}", Name = "GetEmployeeForCompany")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task <IActionResult> GetEmployeeForCompany(Guid companyId, Guid id)
        {
            var company = await _repository.Company.GetCompanyAsync(companyId, trackChanges: false);
            if (company == null)
            {
                _logger.LogInfo($"Company with id: {companyId} doesn't exist in the database.");
            return NotFound();
            }
            var employeeDb = await _repository.Employee.GetEmployeeAsync(companyId, id, trackChanges: false);
            if (employeeDb == null)
            {
                _logger.LogInfo($"Employee with id: {id} doesn't exist in the database.");
            return NotFound();
            }
            var employee = _mapper.Map<EmployeeDto>(employeeDb);
            return Ok(employee);
        }
        /// <summary>
        /// Создание сотрудника компании
        /// </summary>
        /// <param name="companyId"></param>.
        /// <param name="employee"></param>.
        /// <returns>Созданный сотрудник</returns>.
        /// <response code="201">Возвращает созданного сотрудника</response>.
        /// <response code="400">Если возраст меньше 18</response>.
        /// <response code="404">Если компания не найдена</response>.
        [HttpPost]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task <IActionResult> CreateEmployeeForCompany(Guid companyId, [FromBody] EmployeeForCreationDto employee)
        {
            if (employee.Age < 18)
            {
                ModelState.AddModelError("Age", "Age must be at least 18.");
            }
            var company = await _repository.Company.GetCompanyAsync(companyId, trackChanges: false);
            if (company == null)
            {
                _logger.LogInfo($"Company with id: {companyId} doesn't exist in the database.");
            return NotFound();
            }
            var employeeEntity = _mapper.Map<Employee>(employee);
            _repository.Employee.CreateEmployeeForCompany(companyId, employeeEntity);
            await _repository.SaveAsync();
            var employeeToReturn = _mapper.Map<EmployeeDto>(employeeEntity);
            return CreatedAtRoute("GetEmployeeForCompany", new
            {
                companyId,
                id = employeeToReturn.Id
            }, employeeToReturn);
        }
        /// <summary>
        /// Удаление сотрудника компании
        /// </summary>
        /// <param name="companyId"></param>.
        /// <param name="id"></param>.
        /// <returns>Пустой массив данных</returns>.
        /// <response code="204">Сотрудник удален</response>.
        /// <response code="404">Если сотрудник не найден</response>.
        [HttpDelete("{id}")]
        [ServiceFilter(typeof(ValidateEmployeeForCompanyExistsAttribute))]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteEmployeeForCompany(Guid companyId, Guid id)
        {
            var employeeForCompany = HttpContext.Items["employee"] as Employee;
            _repository.Employee.DeleteEmployee(employeeForCompany);
            await _repository.SaveAsync();
            return NoContent();
        }
        /// <summary>
        /// Обновление данных сотрудника для компании
        /// </summary>
        /// <param name="companyId"></param>.
        /// <param name="id"></param>.
        /// <param name="employee"></param>.
        /// <returns>Пустой массив данных</returns>.
        /// <response code="204">Данные сотрудника успешно обновлены</response>.
        /// <response code="404">Если данные о конпании или сотруднике не найдены</response>param>.
        [HttpPut("{id}")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        [ServiceFilter(typeof(ValidateEmployeeForCompanyExistsAttribute))]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateEmployeeForCompany(Guid companyId, Guid id, [FromBody] EmployeeForUpdateDto employee)
        {
            var employeeEntity = HttpContext.Items["employee"] as Employee;
            _mapper.Map(employee, employeeEntity);
            await _repository.SaveAsync();
            return NoContent();
        }
        /// <summary>
        /// Частичное обновление данных сотрудника в указанной компании
        /// </summary>
        /// <param name="companyId"></param>.
        /// <param name="id"></param>.
        /// <param name="patchDoc"></param>.
        /// <returns>Пустой массив данных</returns>.
        /// <response code = "204">Сотрудник успешно частично обновлен</response>.
        /// <response code="400">Если документ с обновлениями равен null</response>.
        /// <response code="422">Ошибка валидации модели после применения изменений</response>.
        [HttpPatch("{id}")]
        [ServiceFilter(typeof(ValidateEmployeeForCompanyExistsAttribute))]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        public async Task<IActionResult> PartiallyUpdateEmployeeForCompany(Guid companyId, Guid id, [FromBody] JsonPatchDocument<EmployeeForUpdateDto> patchDoc)
        {
            if (patchDoc == null)
            {
                _logger.LogError("patchDoc object sent from client is null.");
                return BadRequest("patchDoc object is null");
            }
            var employeeEntity = HttpContext.Items["employee"] as Employee;
            var employeeToPatch = _mapper.Map<EmployeeForUpdateDto>(employeeEntity);
            patchDoc.ApplyTo(employeeToPatch, ModelState);
            TryValidateModel(employeeToPatch);
            if (!ModelState.IsValid)
            {
                _logger.LogError("Invalid model state for the patch document");
                return UnprocessableEntity(ModelState);
            }
            _mapper.Map(employeeToPatch, employeeEntity);
            await _repository.SaveAsync();
            return NoContent();
        }
    }
}