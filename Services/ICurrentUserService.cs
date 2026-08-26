namespace LOSTBOOKS.Services
{
    // Centralized "who is using the system right now" —
    // session-backed for now.
    // Designed so real Login can later replace
    // only CurrentUserService's internals without touching
    // anything that consumes this interface.
    public interface ICurrentUserService
    {
        int? UserID { get; }
        string? FullName { get; }
        string? Role { get; }
        bool IsManager { get; }
        bool MustChangePassword { get; }

        void SetCurrentUser(int userId);
    }
}