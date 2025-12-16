using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace FitDodge.Filters
{
    public class ScannerAuthAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

            string apiKey = context.HttpContext.Request.Headers["Scanner-Key"];

            if (string.IsNullOrEmpty(apiKey))
            {
                context.Result = new UnauthorizedObjectResult("Scanner key missing.");
                return;
            }

            var scanner = await db.ScannerDevices.FirstOrDefaultAsync(s => s.ApiKey == apiKey);

            if (scanner == null)
            {
                context.Result = new UnauthorizedObjectResult("Invalid scanner key.");
                return;
            }

            context.HttpContext.Items["Scanner"] = scanner;

            await next();
        }
    }
}
