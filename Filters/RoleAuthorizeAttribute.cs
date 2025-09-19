using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace StarTickets.Filters
{
    public class RoleAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string _role;

        public RoleAuthorizeAttribute(string role)
        {
            _role = role;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var sessionRole = context.HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(sessionRole) || sessionRole != _role)
            {
                // Redirect to login if not authorized
                context.Result = new RedirectToActionResult("Login", "Auth", null);
            }

            base.OnActionExecuting(context);
        }
    }
}
