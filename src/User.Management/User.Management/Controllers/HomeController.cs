using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading.Tasks;
using User.Management.Entities;
using User.Management.Enum;
using User.Management.Models;

namespace User.Management.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<AppUser> _userManager;

        public HomeController(ILogger<HomeController> logger,UserManager<AppUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.Select(u => new UserViewModel
            {
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                JobTitle = u.JobTitle,
                LastSeenText = GetLastSeenText(u.LastLoginTime),
                IsActive = u.IsActive==Status.Active,
                IsSelected = false
            }).ToListAsync();
            return View(users);
        }

        private static string GetLastSeenText(DateTime? lastLogin)
        {
            if (!lastLogin.HasValue) return "never";
            var span = DateTime.UtcNow - lastLogin.Value;
            if (span.TotalMinutes < 1) return "less than a minute ago";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} minutes ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} hours ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} days ago";
            if (span.TotalDays < 30) return $"{(int)(span.TotalDays / 7)} weeks ago";

            return $"{(int)(span.TotalDays / 30)} months ago";
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> BlockUsers(string[] UserEmails)
        {
            try
            {
                foreach (var email in UserEmails)
                {
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user != null)
                    {
                        user.IsActive = Status.Blocked;
                        await _userManager.UpdateAsync(user);
                    }
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking users");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UnblockUsers(string[] UserEmails)
        {
            try
            {
                foreach (var email in UserEmails)
                {
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user != null)
                    {
                        user.IsActive = Status.Active;
                        await _userManager.UpdateAsync(user);
                    }
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unblocking users");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteUsers(string[] UserEmails)
        {
            try
            {
                // Your logic to delete users
                foreach (var email in UserEmails)
                {
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user != null)
                    {
                        //await _userManager.DeleteAsync(user);
                        await _userManager.DeleteAsync(user);
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting users");
                return Json(new { success = false, message = ex.Message });
            }
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
