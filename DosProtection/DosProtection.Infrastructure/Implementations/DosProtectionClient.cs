using DosProtection.DosProtection.Core.Constants;
using DosProtection.DosProtection.Core.Enums;
using DosProtection.DosProtection.Core.Interfaces;

namespace DosProtection.DosProtection.Infrastructure.Implementations
{
    public class DosProtectionClient : IDosProtectionClient
    {
        private readonly IConfiguration _config;
        private readonly ILogger<DosProtectionClient> _logger;

        private DateTime requestTime;
        private int requestCounter = 0;
        private static int MAX_REQUESTS_PER_FRAME;
        private static int TIME_FRAME_THRESHOLD;

        private readonly object lockObject = new object();

        public DosProtectionClient(IConfiguration config, ILogger<DosProtectionClient> logger)
        {
            _config = config;
            _logger = logger;
            // Validation for the configuration values is done in the Program.cs ValidateConfiguration method.
            MAX_REQUESTS_PER_FRAME = int.Parse(_config[ConfigConstants.MAX_REQUESTS_PER_FRAME]);
            TIME_FRAME_THRESHOLD = int.Parse(_config[ConfigConstants.TIME_FRAME_THRESHOLD]);
        }

        /// <summary>
        /// Checks if the client can make another request within the specified time frame and request limits.
        /// </summary>
        /// <returns>
        ///   True if the client is allowed to make another request within the defined limits; otherwise, false.
        /// </returns>
        public bool CheckRequestRate(ProtectionType protectionType)
        {
            try
            {
                // Lock the object to ensure thread safety.
                lock (lockObject)
                {
                    _logger.LogDebug("[DosProtectionClient:CheckRequestRate] Thread obtained lock. Starts validating.");

                    var now = DateTime.UtcNow;

                    // If the client hasn't made any requests or the last request was more than 5 seconds ago,
                    // start a new time frame.
                    if (requestCounter == 0 || now - requestTime > TimeSpan.FromSeconds(TIME_FRAME_THRESHOLD))
                    {
                        requestTime = now;  // Update the last request time.
                        requestCounter = 1;   // Reset the request count.
                    }
                    else
                    {
                        requestCounter++;

                        // If the client has made more than 5 requests within the time frame, return an error.
                        if (requestCounter > MAX_REQUESTS_PER_FRAME)
                        {
                            // If the protection type is dynamic, update the last request time.
                            if (protectionType == ProtectionType.Dynamic)
                            {
                                requestTime = now;
                            }
                            _logger.LogInformation("[DosProtectionClient:CheckRequestRate] Client is not allowed to make another request.");
                            return false;
                        }
                    }

                    // The client is allowed to make another request.
                    _logger.LogInformation("[DosProtectionClient:CheckRequestRate] Client is allowed to make another request.");
                    _logger.LogDebug("[DosProtectionClient:CheckRequestRate] Thread released lock.");
                    return true;
                }
            }
            catch (Exception e)
            {
                // Log and throw the exception up the call stack to return the user an internal server error (exception),
                // and not service unavailable error (false).
                _logger.LogError($"[DosProtectionClient:CheckRequestRate] An error occurred while validating if the client is allowed to make another request. Error: {e}");
                throw;
            }
        }
    }
}