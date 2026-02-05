using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Todo.Commons.Helpers;
using Todo.DTOs.Authentication.Requests;
using Todo.Services.Interfaces;

namespace Todo.API.Controllers
{
    [Route("authentication")]
    [ApiController]
    public class AuthenticationsController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IHostEnvironment _environment;

        public AuthenticationsController(IAuthenticationService authenticationService, IHostEnvironment environment)
        {
            _authenticationService = authenticationService;
            _environment = environment;
        }

        private void SetAuthCookie(string token)
        {
            var cookieOptions = CookieHelper.GetSecureCookieOptions(_environment);
            Response.Cookies.Append("AuthToken", token, cookieOptions);
        }

        private void DeleteAuthCookie()
        {
            var cookieOptions = CookieHelper.GetDeleteCookieOptions(_environment);
            Response.Cookies.Delete("AuthToken", cookieOptions);
        }

        [HttpPost]
        [Route("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest logIn)
        {
            var result = await _authenticationService.LoginAsync(logIn);

            if (result.IsSuccess && result.Data != null)
            {
                SetAuthCookie(result.Data.AccessToken);
                result.Data.AccessToken = null!;
                result.Data.RefreshToken = null!;
            }

            return Ok(result);
        }

        [HttpPost]
        [Route("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest signUp)
        {
            var result = await _authenticationService.RegisterAsync(signUp);

            if (result.IsSuccess && result.Data != null)
            {
                SetAuthCookie(result.Data.AccessToken);
                result.Data.AccessToken = null!;
                result.Data.RefreshToken = null!;
            }

            return Ok(result);
        }

        [HttpPost]
        [Route("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _authenticationService.LogoutAsync();
            DeleteAuthCookie();
            return Ok(new { message = "Logged out successfully" });
        }
    }
}
