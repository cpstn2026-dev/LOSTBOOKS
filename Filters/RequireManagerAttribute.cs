using LOSTBOOKS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace LOSTBOOKS.Filters
{
    public class RequireManagerAttribute :
    ActionFilterAttribute
    {
        public override void
            OnActionExecuting(ActionExecutingContext context)
        {
            var currentUser =
            context.HttpContext.RequestServices
            .GetService(typeof(ICurrentUserService)) as
            ICurrentUserService;
            if (currentUser == null ||
            !currentUser.IsManager)
            {
                context.Result = new
                RedirectToActionResult("Index", "Home", null);
                return;
            }
            base.OnActionExecuting(context);
        }
    }
}
