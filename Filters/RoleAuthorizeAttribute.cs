using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class RoleAuthorizeAttribute : ActionFilterAttribute
{
    private readonly string[] _roles;

    // Accept multiple roles
    public RoleAuthorizeAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var role = context.HttpContext.Session.GetString("Role");

        if (role == null || !_roles.Contains(role))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
