namespace DosProtection.DosProtection.Core.Events
{
    public class KeyPressEventArgs : EventArgs
    {
        public string Key { get; }

        public KeyPressEventArgs(string key)
        {
            Key = key;
        }
    }

    public class KeyPressEvent
    {
        public event EventHandler<KeyPressEventArgs> KeyPressReceived;

        internal protected virtual void OnKeyPressReceived(string key)
        {
            KeyPressReceived?.Invoke(this, new KeyPressEventArgs(key));
        }
    }
}