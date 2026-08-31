using System.Linq;
using LOSTBOOKS.Data;
using LOSTBOOKS.Filters;
using LOSTBOOKS.Models;
using LOSTBOOKS.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
namespace LOSTBOOKS.Controllers
{
    [RequireManager]
    public class UsersController : Controller
    {
        private readonly LOSTBOOKSContext _context;
        private static readonly
        PasswordHasher<User> _hasher = new
        PasswordHasher<User>();
        public UsersController(LOSTBOOKSContext
        context)
        {
            _context = context;
        }
        // GET: Users
        public async Task<IActionResult> Index(bool showInactive = false)
        {
            var query = _context.Users.AsQueryable();

            if (!showInactive)
            {
                query = query.Where(u => u.Status == "Active" || u.Status == "Pending");
            }
            else
            {
                query = query.Where(u => u.Status == "Active" || u.Status == "Inactive" || u.Status == "Pending");
            }

            var users = await query.OrderBy(u => u.UserID).ToListAsync();

            ViewBag.ShowInactive = showInactive;

            return View(users);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
        int id,
        string FullName,
        string Role,
        string Status)
        {
            var user = await
            _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            if (Role != "Staff" && Role !=
            "Manager")
            {
                TempData["UserError"] = "Invalid role.";
                return
                RedirectToAction(nameof(Index));
            }
            if (Status != "Active" && Status !=
            "Inactive")
            {
                TempData["UserError"] = "Invalid status.";
                return
                RedirectToAction(nameof(Index));
            }
            user.FullName = FullName;
            user.Role = Role;
            user.Status = Status;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        // POST: Users/ChangePassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
        ChangePassword(
        int id,
        string NewPassword,
        string ConfirmNewPassword)
        {
            var user = await
            _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            if
            (string.IsNullOrWhiteSpace(NewPassword) ||
            NewPassword != ConfirmNewPassword)
            {
                TempData["UserError"] = "Passwords do not match.";
            return
            RedirectToAction(nameof(Index));
            }
            user.PasswordHash =
            _hasher.HashPassword(user, NewPassword);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Users/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
        ToggleStatus(int id)
        {
            var user = await
            _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            user.Status = user.Status == "Active" ?
            "Inactive" : "Active";
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        // POST: Users/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user != null && user.Status == "Pending")
            {
                user.Status = "Active";
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Users/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user != null && user.Status == "Pending")
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}