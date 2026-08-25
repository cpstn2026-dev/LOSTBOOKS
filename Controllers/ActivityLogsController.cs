using LOSTBOOKS.Data;
using LOSTBOOKS.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace LOSTBOOKS.Controllers
{
    [RequireManager]
    public class ActivityLogsController :
    Controller
    {
        private readonly LOSTBOOKSContext _context;
        public
        ActivityLogsController(LOSTBOOKSContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(
        DateTime? fromDate,
        DateTime? toDate,
        int? userId,
        string? module,
        string? action)
        {
            var query = _context.ActivityLogs
            .Include(a => a.User)
            .AsQueryable();
            if (fromDate.HasValue)
            {
                query = query.Where(a => a.DateTime
                >= fromDate.Value.Date);
            }
            if (toDate.HasValue)
            {
                query = query.Where(a => a.DateTime
                < toDate.Value.Date.AddDays(1));
            }
            if (userId.HasValue)
            {
                query = query.Where(a => a.UserID
                == userId.Value);
            }
            if (!string.IsNullOrWhiteSpace(module))
            {
                query = query.Where(a => a.Module
                == module);
            }
            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(a => a.Action
                == action);
            }
            var logs = await query
            .OrderByDescending(a => a.DateTime)
            .ToListAsync();
            ViewBag.Users = new SelectList(
            _context.Users.OrderBy(u =>
            u.FullName),
            "UserID",
            "FullName",
            userId);
            ViewBag.Modules = new SelectList(
            new[]
            {
"POS", "Books", "Products",
"Merchandise",
"Services", "Consigners",
"Reports"
            },
            module);
            ViewBag.SelectedFromDate =
            fromDate?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.SelectedToDate =
            toDate?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.SelectedUserId = userId;
            ViewBag.SelectedModule = module ?? "";
            ViewBag.SelectedAction = action ?? "";
            return View(logs);
        }
    }
}