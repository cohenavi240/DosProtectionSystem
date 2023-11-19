using DosProtection.DosProtection.Core.Enums;

namespace DosProtection.DosProtection.Core.Interfaces
{
    public interface IDosProtectionService
    {
        bool ProcessClientRequest(string clientIdentifier, string ipAddress, ProtectionType protectionType);
    }
}