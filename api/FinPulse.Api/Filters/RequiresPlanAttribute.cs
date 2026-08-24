using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FinPulse.Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequiresPlanAttribute : ActionFilterAttribute
{
    private readonly int _minimumPlan;

    public RequiresPlanAttribute(int minimumPlan = 1)
    {
        _minimumPlan = minimumPlan;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var planClaim = context.HttpContext.User.FindFirst("plan")?.Value;
        var plan = planClaim != null && int.TryParse(planClaim, out var p) ? p : 0;

        if (plan < _minimumPlan)
        {
            context.Result = new ObjectResult(new { message = "Plano insuficiente para acessar este recurso." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
