using LOSTBOOKS.Data;
using LOSTBOOKS.Models;
namespace LOSTBOOKS.Services
{
    public class ActivityLogger : IActivityLogger
    {
        private readonly LOSTBOOKSContext _context;
        private readonly ICurrentUserService
        _currentUser;
        public ActivityLogger(
        LOSTBOOKSContext context,
        ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }
        public void Log(string module, string
action, string description)
        {
            int? userId = _currentUser.UserID;

            System.Diagnostics.Debug.WriteLine(
                $"===== ActivityLogger.Log called — module={module}, userId={(userId.HasValue ? userId.Value.ToString() : "NULL")} =====");

            // No current-user context yet (e.g.Users table is still empty) —
            // skip rather than fail the operation that triggered this log.
            if (userId == null)
            {
                return;
            }
            _context.ActivityLogs.Add(new
            ActivityLog
            {
                UserID = userId.Value,
                DateTime = DateTime.Now,
                Module = module,
                Action = action,
                Description = description
            });
            _context.SaveChanges();
        }
    }
}
