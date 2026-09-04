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

        private readonly IEmailSender _emailSender;

        public LoginController(
            LOSTBOOKSContext context,
            ICurrentUserService currentUser,
            IEmailSender emailSender)
        {
            _context = context;
            _currentUser = currentUser;
            _emailSender = emailSender;
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
                .FirstOrDefaultAsync(u => u.Username == username  && u.Status == "Active");

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
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            ViewBag.RequestSubmitted = true;

            if (string.IsNullOrWhiteSpace(email))
            {
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email.ToLower() == email.ToLower() &&
                u.Status == "Active");

            if (user == null)
            {
                return View();
            }

            string token = Guid.NewGuid().ToString("N");
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiry = DateTime.Now.AddHours(1);
            await _context.SaveChangesAsync();

            string resetLink = Url.Action(
                "ResetPasswordEmail", "Login",
                new { token }, Request.Scheme)!;

            string body =
                $"<p>Hello {user.FullName},</p>" +
                $"<p>A password reset was requested for your Lost Books Cebu account.</p>" +
                $"<p><a href='{resetLink}'>Click here to reset your password</a></p>" +
                $"<p>This link expires in 1 hour. If you did not request this, ignore this email.</p>";

            await _emailSender.SendEmailAsync(user.Email, "Password Reset — Lost Books Cebu", body);

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResetPasswordEmail(string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.PasswordResetToken == token &&
                u.PasswordResetTokenExpiry > DateTime.Now);

            if (user == null)
            {
                ViewBag.TokenInvalid = true;
                return View();
            }

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordEmail(
            string token, string newPassword, string confirmPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.PasswordResetToken == token &&
                u.PasswordResetTokenExpiry > DateTime.Now);

            if (user == null)
            {
                ViewBag.TokenInvalid = true;
                return View();
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword != confirmPassword)
            {
                ViewBag.Token = token;
                ViewBag.ResetError = "Passwords do not match.";
                return View();
            }

            user.PasswordHash = _hasher.HashPassword(user, newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.MustChangePassword = false;
            await _context.SaveChangesAsync();

            TempData["ResetSuccess"] = "Your password has been reset. You can now log in.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string fullName, string username, string email,
            string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.RegisterError = "All fields are required.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.RegisterError = "Passwords do not match.";
                return View();
            }

            bool usernameTaken = await _context.Users.AnyAsync(u => u.Username == username);
            if (usernameTaken)
            {
                ViewBag.RegisterError = "That username is already taken.";
                return View();
            }

            var newUser = new User
            {
                FullName = fullName,
                Username = username,
                Email = email,
                Role = "Staff",        // self-registration always creates Staff; Manager can promote later
                Status = "Pending"     // cannot log in until a Manager approves
            };

            newUser.PasswordHash = _hasher.HashPassword(newUser, password);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            ViewBag.RegisterSuccess = true;
            return View();
        }
        [HttpGet]
        public IActionResult EmergencyRecovery()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmergencyRecovery(
            string username, string recoveryToken, string newPassword, string confirmPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username == username &&
                u.EmergencyRecoveryToken == recoveryToken &&
                u.EmergencyRecoveryTokenExpiry > DateTime.Now);

            if (user == null)
            {
                ViewBag.RecoveryError = "Invalid username or recovery token, or the token has expired.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword != confirmPassword)
            {
                ViewBag.RecoveryError = "Passwords do not match.";
                return View();
            }

            user.PasswordHash = _hasher.HashPassword(user, newPassword);
            user.EmergencyRecoveryToken = null;
            user.EmergencyRecoveryTokenExpiry = null;
            await _context.SaveChangesAsync();

            TempData["ResetSuccess"] = "Your password has been reset. You can now log in.";
            return RedirectToAction("Index");
        }
    }
}