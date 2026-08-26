using LOSTBOOKS.Data;
using LOSTBOOKS.Models;
using LOSTBOOKS.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LOSTBOOKS.Controllers
{
    public class LoginController : Controller
    {
        private readonly LOSTBOOKSContext _context;
        private readonly ICurrentUserService _currentUser;
        private static readonly PasswordHasher<User> _hasher = new PasswordHasher<User>();

        public LoginController(
            LOSTBOOKSContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Already logged in — no need to see the login screen again.
            if (_currentUser.UserID != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.LoginError = "Please enter both username and password.";
                return View();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.Status == "Active");

            if (user == null)
            {
                ViewBag.LoginError = "Invalid username or password.";
                return View();
            }

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);

            if (result == PasswordVerificationResult.Failed)
            {
                ViewBag.LoginError = "Invalid username or password.";
                return View();
            }

            _currentUser.SetCurrentUser(user.UserID);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }

        [HttpGet]
        public IActionResult SetNewPassword()
        {
            if (_currentUser.UserID == null)
            {
                return RedirectToAction("Index");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetNewPassword(string newPassword, string confirmPassword)
        {
            if (_currentUser.UserID == null)
            {
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword != confirmPassword)
            {
                ViewBag.SetPasswordError = "Passwords do not match.";
                return View();
            }

            var user = await _context.Users.FindAsync(_currentUser.UserID.Value);

            if (user == null)
            {
                return RedirectToAction("Index");
            }

            user.PasswordHash = _hasher.HashPassword(user, newPassword);
            user.MustChangePassword = false;

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }
    }
}