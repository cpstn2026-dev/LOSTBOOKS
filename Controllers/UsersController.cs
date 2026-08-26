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
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
            .OrderBy(u => u.UserID)
            .ToListAsync();
            return View(users);
        }
        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
        string FullName,
        string Username,
        string Password,
        string ConfirmPassword,
        string Role,
        string Status)
        {
            if (string.IsNullOrWhiteSpace(FullName)
            ||
            string.IsNullOrWhiteSpace(Username)
            ||
            string.IsNullOrWhiteSpace(Password))
            {
                TempData["UserError"] = "All fields are required.";
            return
            RedirectToAction(nameof(Index));
            }
            if (Password != ConfirmPassword)
            {
                TempData["UserError"] = "Passwords  do not match.";
            return
            RedirectToAction(nameof(Index));
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
            bool usernameTaken = await
            _context.Users
            .AnyAsync(u => u.Username ==
            Username);
            if (usernameTaken)
            {
                TempData["UserError"] = "Username is already taken.";
            return
            RedirectToAction(nameof(Index));
            }
            var user = new User
            {
                FullName = FullName,
                Username = Username,
                Role = Role,
                Status = Status
            };
            user.PasswordHash =
            _hasher.HashPassword(user, Password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
        int id,
        string FullName,
        string Username,
        string Role,
        string Status)
        {
            var user = await
            _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            bool usernameTaken = await
            _context.Users
            .AnyAsync(u => u.Username ==
            Username && u.UserID != id);
            if (usernameTaken)
            {
                TempData["UserError"] = "Username is already taken.";
            return
            RedirectToAction(nameof(Index));
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
            user.Username = Username;
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

        // POST: Users/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            string tempPassword = GenerateTempPassword();

            user.PasswordHash = _hasher.HashPassword(user, tempPassword);
            user.MustChangePassword = true;

            await _context.SaveChangesAsync();

            TempData["TempPasswordDisplay"] =
                $"Temporary password for {user.FullName}: {tempPassword} — " +
                "share this with them directly. They will be required to set their own " +
                "permanent password on next login.";

            return RedirectToAction(nameof(Index));
        }

        private static string GenerateTempPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
            var random = new Random();

            return new string(
                Enumerable.Range(0, 10)
                    .Select(_ => chars[random.Next(chars.Length)])
                    .ToArray());
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
    }
}