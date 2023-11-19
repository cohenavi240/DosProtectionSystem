using DosProtection.DosProtection.Core.Enums;

namespace DosProtection.DosProtection.Core.Interfaces
{
    public interface IDosProtectionClient
    {
        public bool CheckRequestRate(ProtectionType protectionType);
    }
}