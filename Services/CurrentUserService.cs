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
            var session =
            _httpContextAccessor.HttpContext?.Session;
            if (session == null)
            {
                return null;
            }
            int? id = session.GetInt32(SessionKey);
            if (id == null)
            {
                // No user selected yet this session — default to the
            // first Active user in the database.
                var firstActive = _context.Users
                .Where(u => u.Status ==
                "Active")
                .OrderBy(u => u.UserID)
                .FirstOrDefault();

                if (firstActive == null)
                {
                    return null;
                }
                session.SetInt32(SessionKey,
                firstActive.UserID);
                return firstActive;
            }
            return _context.Users
            .FirstOrDefault(u => u.UserID ==
            id.Value);
        }
        public int? UserID => GetUser()?.UserID;
        public string? FullName =>
        GetUser()?.FullName;
        public string? Role => GetUser()?.Role;
        public bool IsManager => Role == "Manager";
        public void SetCurrentUser(int userId)
        {
            _httpContextAccessor.HttpContext?.Session.SetInt32(
            SessionKey, userId);
        }
    }
}
