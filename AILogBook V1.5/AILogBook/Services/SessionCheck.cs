using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class SessionCheck : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session.GetString("UserName");
        var controller = context.RouteData.Values["controller"]?.ToString();

        if (string.IsNullOrEmpty(session) && controller != "Login")
        {
            context.Result = new RedirectToActionResult("SignIn", "Login", null);
        }
        base.OnActionExecuting(context);
    }
}