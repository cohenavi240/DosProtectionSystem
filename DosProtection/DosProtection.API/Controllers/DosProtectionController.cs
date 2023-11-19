using System.Net;
using DosProtection.DosProtection.Core.Enums;
using DosProtection.DosProtection.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DosProtection.DosProtection.API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DosProtectionController : ControllerBase
    {
        private readonly IDosProtectionService _dosProtectionService;
        private readonly ILogger<DosProtectionController> _logger;

        public DosProtectionController(IDosProtectionService dosProtectionService,
            ILogger<DosProtectionController> logger)
        {
            _dosProtectionService = dosProtectionService;
            _logger = logger;
        }

        /// <summary>
        /// Handles requests for static window protection.
        /// </summary>
        // Use: https://localhost:44385/DosProtection/StaticWindow/1
        [HttpGet("StaticWindow/{clientId}")]
        public Task<HttpStatusCode> StaticWindow(string clientId) => HandleProtection(clientId, ProtectionType.Static);

        /// <summary>
        /// Handles requests for dynamic window protection.
        /// </summary>
        // Use: https://localhost:44385/DosProtection/DynamicWindow/1
        [HttpGet("DynamicWindow/{clientId}")]
        public Task<HttpStatusCode> DynamicWindow(string clientId) => HandleProtection(clientId, ProtectionType.Dynamic);

        /// <summary>
        /// Handles protection for a specific client based on protection type.
        /// </summary>
        private async Task<HttpStatusCode> HandleProtection(string clientId, ProtectionType protectionType)
        {
            try
            {
                _logger.LogInformation($"[DosProtectionController:{protectionType}Window] Starts validating if client ID: {clientId} is permitted.");

                if (HttpContext.Connection.RemoteIpAddress == null)
                {
                    throw new ArgumentNullException($"[DosProtectionController:{protectionType}Window] IP address is null.");
                }

                // Fetch the client's IP address.
                string clientIpAddress = HttpContext.Connection.RemoteIpAddress.ToString();

                // Process the client's request in a new thread.
                bool result = await Task.Run(() => _dosProtectionService.ProcessClientRequest(clientId, clientIpAddress, protectionType));

                if (result)
                {
                    _logger.LogInformation($"[DosProtectionController:{protectionType}Window] Client ID: {clientId} is permitted.");
                    return HttpStatusCode.OK;
                }
                else
                {
                    _logger.LogInformation($"[DosProtectionController:{protectionType}Window] Client ID: {clientId} is not permitted.");
                    return HttpStatusCode.ServiceUnavailable;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"[DosProtectionController:{protectionType}Window] An error occurred while validating if client ID: {clientId} is permitted. Error: {e}");
                return HttpStatusCode.InternalServerError;
            }
        }
    }
}