using AutoMapper;
using CompanyEmployees.ActionFilters;
using CompanyEmployees.ModelBinders;
using Contracts;
using Entities.DataTransferObjects;
using Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using static System.Collections.Specialized.BitVector32;

namespace CompanyEmployees.Controllers
{
    [Route("api/companies")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "v1")]
    public class CompaniesController : ControllerBase
    {
        private readonly IRepositoryManager _repository;
        private readonly ILoggerManager _logger;
        private readonly IMapper _mapper;
        public CompaniesController(IRepositoryManager repository, ILoggerManager logger, IMapper mapper)
        {
            _repository = repository;
            _logger = logger;
            _mapper = mapper;
        }
        /// <summary>
        /// Возвращает список всех компаний
        /// </summary>
        /// <returns> Список компаний</returns>.
        [HttpGet(Name = "GetCompanies"), Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetCompanies()
        {
           var companies = await _repository.Company.GetAllCompaniesAsync(trackChanges: false);
           var companiesDto = _mapper.Map<IEnumerable<CompanyDto>>(companies);
           return Ok(companiesDto);
           throw new Exception("Exception");

        }

        /// <summary>
        /// Возвращает компанию по ID
        /// </summary>
        /// <param name="id"></param>.
        /// <returns>Компания с указанным ID</returns>.
        /// <response code="200">Возвращает запрошенную компанию</response>.
        /// <response code="404">Если компания с указанным ID не найдена</response>.
        [HttpGet("{id}", Name = "CompanyById")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetCompany(Guid id)
        {
            var company = await _repository.Company.GetCompanyAsync(id, trackChanges: false);
            if (company == null)
            {
                _logger.LogInfo($"Company with id: {id} doesn't exist in the database.");
                return NotFound();
            }
            else
            {
                var companyDto = _mapper.Map<CompanyDto>(company);
                return Ok(companyDto);
            }
        }

        /// <summary>
        /// Создание вновь созданной компании
        /// </summary>
        /// <param name="company"></param>.
        /// <returns>Вновь созданная компания</returns>.
        /// <response code="201">Возвращает только что созданный элемент</response>.
        /// <response code="400">Если элемент равен null</response>.
        /// <response code="422">Если модель недействительна</response>.
        [HttpPost(Name = "CreateCompany")]
        [ProducesResponseType(200)]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> CreateCompany([FromBody] CompanyForCreationDto company)
        {
            var companyEntity = _mapper.Map<Company>(company);
            _repository.Company.CreateCompany(companyEntity);
            await _repository.SaveAsync();
            var companyToReturn = _mapper.Map<CompanyDto>(companyEntity);
            return CreatedAtRoute("CompanyById", new { id = companyToReturn.Id },
            companyToReturn);
        }
        /// <summary>
        /// Возвращает коллекцию компаний по ID
        /// </summary>
        /// <param name="ids"></param>.
        /// <returns>Коллекция компаний</returns>.
        /// <response code="400">Если параметр id равен null</response>.
        /// <response code="404">Если некоторые ids не найдены</response>.
        [HttpGet("collection/({ids})", Name = "CompanyCollection")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetCompanyCollection( [ModelBinder(BinderType = typeof(ArrayModelBinder))] IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                _logger.LogError("Parameter ids is null");
                return BadRequest("Parameter ids is null");
            }
            var companyEntities = await _repository.Company.GetByIdsAsync(ids, trackChanges: false);
            if (ids.Count() != companyEntities.Count())
            {
                _logger.LogError("Some ids are not valid in a collection");
                return NotFound();
            }
            var companiesToReturn = _mapper.Map<IEnumerable<CompanyDto>>(companyEntities);
            return Ok(companiesToReturn);
        }
        /// <summary>
        /// Создание коллекции из новых компаний
        /// </summary>
        /// <param name="companyCollection"></param>.
        /// <returns>Новая коллекция созданных компаний</returns>.
        /// <response code="201">Возвращает созданную коллекцию компаний</response>.
        /// <response code="400">Если переданная коллекция равна null</response>.
        /// <response code="422">Если модель данных невалидна</response>.
        [HttpPost("collection")]
        [ProducesResponseType(200)]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        public async Task<IActionResult> CreateCompanyCollection([FromBody] IEnumerable<CompanyForCreationDto> companyCollection)
        {
            if (companyCollection == null)
            {
                _logger.LogError("Company collection sent from client is null.");
                return BadRequest("Company collection is null");
            }
            var companyEntities = _mapper.Map<IEnumerable<Company>>(companyCollection);
            foreach (var company in companyEntities)
            {
                _repository.Company.CreateCompany(company);
            }
            await _repository.SaveAsync();
            var companyCollectionToReturn = _mapper.Map<IEnumerable<CompanyDto>>(companyEntities);
            var ids = string.Join(",", companyCollectionToReturn.Select(c => c.Id));
            return CreatedAtRoute("CompanyCollection", new { ids }, companyCollectionToReturn);
        }
        /// <summary>
        /// Удаление компании по ID
        /// </summary>
        /// /// <param name="id"></param>
        /// <returns>Пустой массив данных</returns>.
        /// <response code="204">Компания успешно удалена</response>.
        /// <response code="404">Если компания с указанным ID не найдена</response>.
        [HttpDelete("{id}")]
        [ServiceFilter(typeof(ValidateCompanyExistsAttribute))]
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteCompany(Guid id)
        {
            var company = HttpContext.Items["company"] as Company;
            _repository.Company.DeleteCompany(company);
            await _repository.SaveAsync();
            return NoContent();
        }
        /// <summary>
        /// Обновление данных компании
        /// </summary>
        /// <param name="id"></param>.
        /// <param name="company">Данные для обновления (CompanyForUpdateDto)</param>.
        /// <returns>Пустой массив данных</returns>.
        /// <response code="204">Данные компании успешно обновлены</response>.
        /// <response code="400">Некорректные входные данные</response>.
        /// <response code="404">Компания с указанным ID не найдена</response>.
        /// <response code="422">Ошибка валидации модели</response>.
        [HttpPut("{id}")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        [ServiceFilter(typeof(ValidateCompanyExistsAttribute))]
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(422)]
        public async Task<IActionResult> UpdateCompany(Guid id, [FromBody] CompanyForUpdateDto company)
        {
            var companyEntity = HttpContext.Items["company"] as Company;
            _mapper.Map(company, companyEntity);
            await _repository.SaveAsync();
            return NoContent();
        }
        /// <summary>
        /// Возвращает доступные методы
        /// </summary>
        /// <returns>Список разрешенных методов в заголовке Allow</returns>.
        /// <response code="200">Возвращает список доступных методов</response>.
        [HttpOptions]
        [ProducesResponseType(200)]
        public IActionResult GetCompaniesOptions()
        {
            Response.Headers.Add("Allow", "GET, OPTIONS, POST");
            return Ok();
        }
    }
}