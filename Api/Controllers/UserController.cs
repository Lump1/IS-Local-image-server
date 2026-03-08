using Microsoft.AspNetCore.Mvc;

using Contracts.Messages;
using IS.DbCommon;
using Contracts;
using System.Text.Json;
using Contracts.Serialization;
using IS.SharedServices.Services.TaskPublisherService;
using RabbitMQ.Client.Events;
using IS.SharedServices.Services.TaskReceiverService;

namespace IS.ImageService.Api.Controllers
{
    [ApiController]
    [Route("user")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly ITaskPublisher _taskPublisher;
        private readonly ImageServerEFContext _dbcontext;
        private readonly ITaskReceiver _taskReceiver;

        public UserController(ILogger<UserController> logger, ITaskPublisher taskPublisher, ImageServerEFContext imageServerEFContext, ITaskReceiver taskReceiver)
        {
            _logger = logger;
            _taskPublisher = taskPublisher;
            _dbcontext = imageServerEFContext;
            _taskReceiver = taskReceiver;
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

            _taskReceiver.ReceiveAsync(
                RBQ_Queues.AuthKeyValidation,
                DateTime.UtcNow.AddMinutes(10),
                Expression: async (sender, args) => await KeyValidationAsync(sender, args)
            );

            return Ok("Logged");
        }

        private async Task KeyValidationAsync(object? sender, BasicDeliverEventArgs args)
        {
            var messageJson = await JsonSerializer.DeserializeAsync<Contracts.Messages.HardwareKeyValidation>(
                new MemoryStream(args.Body.ToArray()),
                MessageJsonContext.Default.HardwareKeyValidation
            );
        }
    }
}
