using FormsApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;

namespace FormsApp.Middleware
{
    public class BlockedUserMiddleware
    {
        private readonly RequestDelegate _next;
        
        public BlockedUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        
        public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                var userId = userManager.GetUserId(context.User);
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await userManager.FindByIdAsync(userId);
                    
                    // If the user is blocked, sign them out
                    if (user != null && user.IsBlocked)
                    {
                        await signInManager.SignOutAsync();
                        
                        // Add a message to be displayed after redirect
                        context.Response.Cookies.Append("BlockedNotice", "Your account has been blocked by an administrator.");
                        
                        // Redirect to the login page with access denied message
                        context.Response.Redirect("/Account/AccessDenied");
                        return;
                    }
                }
            }
            
            await _next(context);
        }
    }
    
    // Extension method to make it easier to add the middleware
    public static class BlockedUserMiddlewareExtensions
    {
        public static IApplicationBuilder UseBlockedUserCheck(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<BlockedUserMiddleware>();
        }
    }
} 