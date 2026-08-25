using LOSTBOOKS.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
namespace LOSTBOOKS.Controllers
{
    public class CurrentUserController : Controller
    {
        private readonly ICurrentUserService
        _currentUser;
        public
        CurrentUserController(ICurrentUserService
        currentUser)
        {
            _currentUser = currentUser;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Switch(int userId,
        string? returnUrl)
        {
            // Switching the active user is not itself an Activity Log entry.
            _currentUser.SetCurrentUser(userId);
            if
            (!string.IsNullOrWhiteSpace(returnUrl) &&
            Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index",
            "Home");
        }
    }
}