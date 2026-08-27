using LOSTBOOKS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LOSTBOOKS.Filters
{
    public class RequireLoginFilter : IAsyncActionFilter
    {
        private readonly ICurrentUserService _currentUser;

        public RequireLoginFilter(ICurrentUserService currentUser)
        {
            _currentUser = currentUser;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            string? controllerName = context.RouteData.Values["controller"]?.ToString();
            // Let the Login controller itself through — otherwise nobody
            // could ever reach the login page (this also covers SetNewPassword,
            // since that action lives inside LoginController).
            if (controllerName == "Login")
            {
                await next();
                return;
            }
            if (_currentUser.UserID == null)
            {
                context.Result = new RedirectToActionResult("Index", "Login", null);
                return;
            }

            await next();
        }
    }
}