using IS.ImageService.Api.Services.TaskPublisherService;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

using Contracts.Messages;
using IS.DbCommon;
using Contracts;
using System.Text.Json;
using Contracts.Serialization;

namespace IS.ImageService.Api.Controllers
{
    [ApiController]
    [Route("user")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly ITaskPublisher _taskPublisher;
        private readonly ImageServerEFContext _dbcontext;

        public UserController(ILogger<UserController> logger, ITaskPublisher taskPublisher, ImageServerEFContext imageServerEFContext)
        {
            _logger = logger;
            _taskPublisher = taskPublisher;
            _dbcontext = imageServerEFContext;
        }

        [HttpPost]
        public IActionResult Register([FromQuery(Name = "login")] string login, [FromQuery(Name = "password-hash")] string passwordHash, [FromQuery(Name = "system-key")] string systemKey)
        {
            _logger.Log(LogLevel.Information, "Someone is trying to access root images api route");

            if (_dbcontext.Accounts.Any(a => a.Login == login)) {
                return Conflict("User with such login already exists");
            }


            var jobId = _taskPublisher.PublishToQueueAsync(
                RBQ_Queues.AuthKeyValidation, 
                JsonSerializer.SerializeToUtf8Bytes(
                    new HardwareKeyValidation(systemKey, RBQ_Queues.AuthKeyValidation.ToString()),
                    MessageJsonContext.Default.HardwareKeyValidation
                )
            ).GetAwaiter().GetResult();

            return Ok("Logged");
        }
    }
}
