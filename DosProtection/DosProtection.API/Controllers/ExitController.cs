using DosProtection.DosProtection.Core.Events;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace DosProtection.DosProtection.API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ExitController : ControllerBase
    {
        private readonly KeyPressEvent _keyPressEventHandler;
        private readonly ILogger<ExitController> _logger;

        public ExitController(KeyPressEvent keyPressEventHandler, ILogger<ExitController> logger)
        {
            _keyPressEventHandler = keyPressEventHandler;
            _logger = logger;
        }

        // This API endpoint serves as a mock to simulate a key press event pressed by the server,
        // since Web API is stateless and cannot get user input (only in the form of HTTP requests).
        // Besides the trigger of the key press (input event vs HTTP request), the logic implemented is the same
        // once the event is triggered.
        // The listener for the key press event is implemented in the Program.cs file in a separate task running
        // throughout the application's lifetime in the background.
        // To simulate a real case scenario, there will be an IP-based validation to ensure the request (key press)
        // is coming from the server.
        // Use: https://localhost:44385/Exit/q (q for exit)
        [HttpGet("{key}")]
        public HttpStatusCode Exit(string key)
        {
            try
            {
                if (HttpContext.Connection.RemoteIpAddress == null)
                {
                    throw new ArgumentNullException("[ExitController:Exit] Remote IP address is null.");
                }

                // Allow operation only if the request is coming from the server itself (::1 as a loopback address).
                string clientIpAddress = HttpContext.Connection.RemoteIpAddress.ToString();
                if (IPAddress.IsLoopback(IPAddress.Parse(clientIpAddress)))
                {
                    _logger.LogInformation($"[ExitController:Exit] Key signal received: {key}");
                    _keyPressEventHandler.OnKeyPressReceived(key);
                    return HttpStatusCode.OK;
                }
                else
                {
                    _logger.LogInformation($"[ExitController:Exit] Key signal received: {key} from unauthorized IP address.");
                    return HttpStatusCode.Unauthorized;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"[ExitController:Exit] An error occurred while processing key signal: {e}");
                return HttpStatusCode.InternalServerError;
            }
        }
    }
}