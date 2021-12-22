namespace Shipwreck.Utilities
{
    public interface ILog
    {
        void Write(string format, params object[] args);
        void WriteWithTime(string str, params object[] args);
    }
}
