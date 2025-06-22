using AutoMapper;
using CompanyEmployees.ActionFilters;
using Contracts;
using Entities.DataTransferObjects;
using Entities.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CompanyEmployees.Controllers
{
    [Route("api/authentication")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly ILoggerManager _logger;
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;

        private readonly IAuthenticationManager _authManager;
        public AuthenticationController(ILoggerManager logger, IMapper mapper,UserManager<User> userManager, IAuthenticationManager authManager)
        {
            _logger = logger;
            _mapper = mapper;
            _userManager = userManager;
            _authManager = authManager;
        }
        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        /// <param name="userForRegistration"></param>.
        /// <returns>Результат регистрации</returns>.
        /// <response code="201">Пользователь успешно зарегистрирован</response>.
        /// <response code="400">Пользователь уже существует</response>..
        [HttpPost]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        [ProducesResponseType(200)]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> RegisterUser([FromBody] UserForRegistrationDto userForRegistration)
        {
            var user = _mapper.Map<User>(userForRegistration);
            var result = await _userManager.CreateAsync(user, userForRegistration.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.TryAddModelError(error.Code, error.Description);
                }
                return BadRequest(ModelState);
            }
            await _userManager.AddToRolesAsync(user, userForRegistration.Roles);
            return StatusCode(201);
        }
        /// <summary>
        /// Аутентификация пользователя и возвращает JWT токен
        /// </summary>
        /// <param name="user"></param>.
        /// <returns>JWT токен для авторизации</returns>.
        /// <response code="400">Некорректные входные данные</response>.
        /// <response code="401">Неверное имя пользователя или пароль</response>.
        /// <response code="422">Ошибка валидации модели</response>.
        [HttpPost("login")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(422)]
        public async Task<IActionResult> Authenticate([FromBody] UserForAuthenticationDto user)
        {
            if (!await _authManager.ValidateUser(user))
            {
                _logger.LogWarn($"{nameof(Authenticate)}: Authentication failed. Wrong user name or password.");
            return Unauthorized();
            }
            return Ok(new { Token = await _authManager.CreateToken() });
        }
    }
}
