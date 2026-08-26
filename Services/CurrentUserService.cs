using LOSTBOOKS.Data;
namespace LOSTBOOKS.Services
{
    public class CurrentUserService :
    ICurrentUserService
    {
        private const string SessionKey =
        "CurrentUserID";
        private readonly IHttpContextAccessor
        _httpContextAccessor;
        private readonly LOSTBOOKSContext _context;
        public CurrentUserService(
        IHttpContextAccessor
        httpContextAccessor,
        LOSTBOOKSContext context)
        {
            _httpContextAccessor =
            httpContextAccessor;
            _context = context;
        }
        private Models.User? GetUser()
        {
            var session = _httpContextAccessor.HttpContext?.Session;

            if (session == null)
            {
                return null;
            }

            int? id = session.GetInt32(SessionKey);

            if (id == null)
            {
                // No one is logged in — Login is now required, so there is
                // no fallback to "the first active user" anymore.
                return null;
            }

            return _context.Users
                .FirstOrDefault(u => u.UserID == id.Value && u.Status == "Active");
        }
        public int? UserID => GetUser()?.UserID;
        public string? FullName =>
        GetUser()?.FullName;
        public string? Role => GetUser()?.Role;
        public bool IsManager => Role == "Manager";

        public bool MustChangePassword => GetUser()?.MustChangePassword ?? false;
        public void SetCurrentUser(int userId)
        {
            _httpContextAccessor.HttpContext?.Session.SetInt32(
            SessionKey, userId);
        }
    }
}
