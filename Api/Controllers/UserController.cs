using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace IS.ImageService.Api.Controllers
{
    [ApiController]
    [Route("user")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;

        public UserController(ILogger<UserController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public IActionResult Register([FromQuery(Name = "login")] string login, [FromQuery(Name = "password-hash")] string passwordHash, [FromQuery(Name = "system-key-hash")] string systemKeyHash)
        {
            // Observer Logic
            _logger.Log(LogLevel.Information, "Someone is trying to access root images api route");
            return Ok("Logged");
        }
    }
}
