using FormsApp.Models;
using FormsApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FormsApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        
        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }
        
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<UserViewModel>();
            
            foreach (var user in users)
            {
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    IsAdmin = isAdmin,
                    IsBlocked = user.IsBlocked
                });
            }
            
            ViewBag.AdminCount = userViewModels.Count(u => u.IsAdmin);
            return View(userViewModels);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdmin(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            var currentUserId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            var isCurrentUser = user.Id == currentUserId;
            
            if (isAdmin)
            {
                // Get the count of admin users to ensure at least one admin remains
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                
                if (admins.Count > 1 || !isCurrentUser)
                {
                    await _userManager.RemoveFromRoleAsync(user, "Admin");
                    
                    // If admin is removing their own admin rights, sign them out to refresh claims
                    if (isCurrentUser)
                    {
                        TempData["InfoMessage"] = "Your admin rights have been removed. Please sign in again.";
                        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                        return RedirectToAction("Login", "Account");
                    }
                }
                else
                {
                    // Cannot remove the last admin or yourself
                    TempData["ErrorMessage"] = "Cannot remove the last admin.";
                }
            }
            else
            {
                // Ensure the Admin role exists
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                }
                
                await _userManager.AddToRoleAsync(user, "Admin");
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlock(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            
            // Don't allow blocking yourself
            if (user.Id == User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value)
            {
                TempData["ErrorMessage"] = "You cannot block yourself.";
                return RedirectToAction(nameof(Index));
            }
            
            user.IsBlocked = !user.IsBlocked;
            
            await _userManager.UpdateAsync(user);
            
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            
            // Don't allow deleting yourself
            if (user.Id == User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value)
            {
                TempData["ErrorMessage"] = "You cannot delete yourself.";
                return RedirectToAction(nameof(Index));
            }
            
            try
            {
                // Get the DbContext to handle related entities
                var dbContext = HttpContext.RequestServices.GetService(typeof(FormsApp.Data.ApplicationDbContext)) as FormsApp.Data.ApplicationDbContext;
                
                if (dbContext != null)
                {
                    // Find all form templates created by this user
                    var userTemplates = dbContext.FormTemplates.Where(t => t.CreatorId == userId).ToList();
                    
                    // Remove them from the database
                    dbContext.FormTemplates.RemoveRange(userTemplates);
                    
                    // Save changes to delete the templates
                    await dbContext.SaveChangesAsync();
                }
                
                // Now delete the user
                await _userManager.DeleteAsync(user);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting user: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockSelected(List<string> selectedUsers)
        {
            if (selectedUsers == null || !selectedUsers.Any())
            {
                TempData["ErrorMessage"] = "No users selected.";
                return RedirectToAction(nameof(Index));
            }
            
            var currentUserId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            int blockedCount = 0;
            
            foreach (var userId in selectedUsers)
            {
                // Don't allow blocking yourself
                if (userId == currentUserId)
                {
                    continue;
                }
                
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null && !user.IsBlocked)
                {
                    user.IsBlocked = true;
                    await _userManager.UpdateAsync(user);
                    blockedCount++;
                }
            }
            
            TempData["SuccessMessage"] = $"Successfully blocked {blockedCount} user(s).";
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockSelected(List<string> selectedUsers)
        {
            if (selectedUsers == null || !selectedUsers.Any())
            {
                TempData["ErrorMessage"] = "No users selected.";
                return RedirectToAction(nameof(Index));
            }
            
            int unblockCount = 0;
            
            foreach (var userId in selectedUsers)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null && user.IsBlocked)
                {
                    user.IsBlocked = false;
                    await _userManager.UpdateAsync(user);
                    unblockCount++;
                }
            }
            
            TempData["SuccessMessage"] = $"Successfully unblocked {unblockCount} user(s).";
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected(List<string> selectedUsers)
        {
            if (selectedUsers == null || !selectedUsers.Any())
            {
                TempData["ErrorMessage"] = "No users selected.";
                return RedirectToAction(nameof(Index));
            }
            
            var currentUserId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            int deletedCount = 0;
            
            // Get the DbContext to handle related entities
            var dbContext = HttpContext.RequestServices.GetService(typeof(FormsApp.Data.ApplicationDbContext)) as FormsApp.Data.ApplicationDbContext;
            
            foreach (var userId in selectedUsers)
            {
                // Don't allow deleting yourself
                if (userId == currentUserId)
                {
                    continue;
                }
                
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    try
                    {
                        if (dbContext != null)
                        {
                            // Find all form templates created by this user
                            var userTemplates = dbContext.FormTemplates.Where(t => t.CreatorId == userId).ToList();
                            
                            // Remove them from the database
                            dbContext.FormTemplates.RemoveRange(userTemplates);
                        }
                        
                        // Delete the user
                        await _userManager.DeleteAsync(user);
                        deletedCount++;
                    }
                    catch (Exception)
                    {
                        // Log error and continue
                    }
                }
            }
            
            if (dbContext != null)
            {
                // Save changes to delete all templates
                await dbContext.SaveChangesAsync();
            }
            
            TempData["SuccessMessage"] = $"Successfully deleted {deletedCount} user(s).";
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeAdminSelected(List<string> selectedUsers)
        {
            if (selectedUsers == null || !selectedUsers.Any())
            {
                TempData["ErrorMessage"] = "No users selected.";
                return RedirectToAction(nameof(Index));
            }
            
            // Ensure the Admin role exists
            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            
            int adminCount = 0;
            
            foreach (var userId in selectedUsers)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null && !await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                    adminCount++;
                }
            }
            
            TempData["SuccessMessage"] = $"Successfully made {adminCount} user(s) admin.";
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAdminSelected(List<string> selectedUsers)
        {
            if (selectedUsers == null || !selectedUsers.Any())
            {
                TempData["ErrorMessage"] = "No users selected.";
                return RedirectToAction(nameof(Index));
            }
            
            var currentUserId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            int removedCount = 0;
            
            // Count how many admins we have total
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            int currentAdminCount = admins.Count;
            
            // Count how many admins are being removed
            int selectedAdminCount = 0;
            foreach (var userId in selectedUsers)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    selectedAdminCount++;
                }
            }
            
            // Make sure we're not removing all admins
            if (currentAdminCount <= selectedAdminCount)
            {
                TempData["ErrorMessage"] = "Cannot remove all admin users. At least one admin must remain.";
                return RedirectToAction(nameof(Index));
            }
            
            foreach (var userId in selectedUsers)
            {
                // Don't allow removing your own admin rights
                if (userId == currentUserId)
                {
                    continue;
                }
                
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Admin");
                    removedCount++;
                }
            }
            
            TempData["SuccessMessage"] = $"Successfully removed admin rights from {removedCount} user(s).";
            return RedirectToAction(nameof(Index));
        }
    }
} 