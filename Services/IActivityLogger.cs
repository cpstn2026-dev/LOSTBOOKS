namespace LOSTBOOKS.Services
{
    public interface IActivityLogger
    {
        void Log(string module, string action,
        string description);
    }
}