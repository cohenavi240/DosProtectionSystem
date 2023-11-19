using System.Collections.Concurrent;
using DosProtection.DosProtection.Core.Enums;
using DosProtection.DosProtection.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace DosProtection.DosProtection.API.Services
{
    public class DosProtectionService : IDosProtectionService
    {
        private readonly ILogger<DosProtectionService> _logger;
        private readonly IServiceProvider _serviceProvider;
        
        // Store IP as key and dosProtectionClient instance as value in Cache
        private readonly IMemoryCache _memoryCache;

        // Store client ID as key and dosProtectionClient instance as value, one for each window
        private readonly ConcurrentDictionary<string, IDosProtectionClient> _staticWindowClients = new ConcurrentDictionary<string, IDosProtectionClient>();
        private readonly ConcurrentDictionary<string, IDosProtectionClient> _dynamicWindowClients = new ConcurrentDictionary<string, IDosProtectionClient>();

        public DosProtectionService(IMemoryCache memoryCache, IServiceProvider serviceProvider,
            ILogger<DosProtectionService> logger)
        {
            _serviceProvider = serviceProvider;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        /// <summary>
        /// Processes a client request and applies DOS protection.
        /// </summary>
        /// <returns>True if the client is allowed to make another request within the defined limits; otherwise, false.</returns>
        public bool ProcessClientRequest(string clientId, string clientIpAddress, ProtectionType protectionType)
        {
            try
            {
                _logger.LogDebug($"[DosProtectionService:ProcessClientRequest] Starts processing the request of client ID: {clientId}.");

                // Get the relevant ConcurrentDictionary based on the protection type.
                var windowClients = protectionType == ProtectionType.Static ? _staticWindowClients : _dynamicWindowClients;

                // Get or add a DosProtectionClient instance for the clientId from the relevant ConcurrentDictionary.
                var dosClient = windowClients.GetOrAdd(clientId, entry => _serviceProvider.GetRequiredService<IDosProtectionClient>());

                // Get or create a DosProtectionClient instance for the client's IP address from cache.
                var dosClientIp = _memoryCache.GetOrCreate(clientIpAddress, entry => _serviceProvider.GetRequiredService<IDosProtectionClient>());

                // Check if the client is allowed to make another request based on his ID only.
                return dosClient.CheckRequestRate(protectionType) /*&& dosClientIp.CheckRequestRate(protectionType)*/;

                // Check if the client is allowed to make another request based on his ID and IP address.
                //return dosClient.CheckRequestRate(protectionType) && dosClientIp.CheckRequestRate(protectionType);
            }
            catch (Exception e)
            {
                // Log and throw the exception up the call stack to return the user an internal server error (exception),
                // and not service unavailable error (false).
                _logger.LogError($"[DosProtectionService:ProcessClientRequest] An error occurred while processing the request of client ID: {clientId}. Error: {e}");
                throw;
            }
        }
    }
}