using IS.ImageService.Api.Services.TaskPublisherService;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

using Contracts.Messages;

namespace IS.ImageService.Api.Controllers
{
    [ApiController]
    [Route("user")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly ITaskPublisher _taskPublisher;

        public UserController(ILogger<UserController> logger, ITaskPublisher taskPublisher)
        {
            _logger = logger;
            _taskPublisher = taskPublisher;
        }

        [HttpPost]
        public IActionResult Register([FromQuery(Name = "login")] string login, [FromQuery(Name = "password-hash")] string passwordHash, [FromQuery(Name = "system-key")] string systemKey)
        {
            _logger.Log(LogLevel.Information, "Someone is trying to access root images api route");

            //_taskPublisher.PublishToQueueAsync(new HardwareKeyValidation
            //{
            //    HardwareKey = systemKey,
            //    UserId = -1
            //}, Contracts.RBQ_Queues.UserRegistration.ToString()).GetAwaiter().GetResult();

            return Ok("Logged");
        }
    }
}
