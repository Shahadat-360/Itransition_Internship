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
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Collections.Generic;
using System.Security.Claims;
using FormsApp.Data;

namespace FormsApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _dbcontext;
        private readonly ILogger<AdminController> _logger;
        
        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            IMapper mapper,
            IWebHostEnvironment webHostEnvironment,
            ApplicationDbContext dbContext,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _mapper = mapper;
            _webHostEnvironment = webHostEnvironment;
            _dbcontext = dbContext;
            _logger = logger;
        }
        
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = await MapUsersToViewModelsAsync(users);
            ViewBag.AdminCount = userViewModels.Count(u => u.IsAdmin);
            return View(userViewModels);
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
            
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool currentUserIncluded = selectedUsers.Contains(currentUserId);
            
            // Get all admin users who aren't already blocked
            var activeAdmins = await _userManager.GetUsersInRoleAsync("Admin");
            activeAdmins = activeAdmins.Where(a => !a.IsBlocked).ToList();
            
            // Get all admin users who are selected for blocking
            var selectedAdminIds = selectedUsers.Intersect(activeAdmins.Select(a => a.Id)).ToList();
            
            // If all active admins are selected for blocking, prevent the operation
            if (selectedAdminIds.Count >= activeAdmins.Count)
            {
                TempData["ErrorMessage"] = "Cannot block all active admins. At least one admin must remain unblocked.";
                return RedirectToAction(nameof(Index));
            }

            int blockedCount = 0;
            
            foreach (var userId in selectedUsers)
            {
                // Now we can block even the current user if there are other active admins
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null && !user.IsBlocked)
                {
                    user.IsBlocked = true;
                    await _userManager.UpdateAsync(user);
                    blockedCount++;
                }
            }
            
            // If the current user blocked themselves, sign them out
            if (currentUserIncluded)
            {
                await _signInManager.SignOutAsync();
                TempData["SuccessMessage"] = "You've blocked your account and been signed out.";
                return RedirectToAction("Index", "Home");
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
            //ublocking selected users
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
            
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool currentUserIncluded = selectedUsers.Contains(currentUserId);
            
            // Get all admin users who aren't blocked
            var activeAdmins = await _userManager.GetUsersInRoleAsync("Admin");
            activeAdmins = activeAdmins.Where(a => !a.IsBlocked).ToList();
            
            // Find which selected users are admins
            var selectedAdminIds = new List<string>();
            foreach (var userId in selectedUsers)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null && await _userManager.IsInRoleAsync(user, "Admin") && !user.IsBlocked)
                {
                    selectedAdminIds.Add(userId);
                }
            }
            
            // If all active admins would be deleted, prevent the operation
            if (selectedAdminIds.Count >= activeAdmins.Count)
            {
                TempData["ErrorMessage"] = "Cannot delete all active admins. At least one unblocked admin must remain.";
                return RedirectToAction(nameof(Index));
            }
            
            int deletedCount = 0;
            
            if (_dbcontext == null)
            {
                TempData["ErrorMessage"] = "Error: Could not access database.";
                return RedirectToAction(nameof(Index));
            }
            
            foreach (var userId in selectedUsers)
            {
                // Now we can delete even the current user if there are other active admins
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = $"User with ID {userId} not found.";
                    _logger.LogWarning($"User with ID {userId} not found for deletion.");
                    continue;
                }
                
                try
                {
                    // Clean up all user-related entities using our helper method
                    await CleanupUserRelatedEntitiesAsync(user);
                    
                    // Delete the user
                    await _userManager.DeleteAsync(user);
                    
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing the other users
                    Console.WriteLine($"Error deleting user {userId}: {ex.Message}");
                }
            }
            
            // Save changes to delete all templates
            await _dbcontext.SaveChangesAsync();
            
            // If the current user deleted themselves, sign them out
            if (currentUserIncluded)
            {
                await _signInManager.SignOutAsync();
                TempData["SuccessMessage"] = "Your account has been deleted. You've been signed out.";
                return RedirectToAction("Index", "Home");
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
            
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool currentUserIncluded = selectedUsers.Contains(currentUserId);
            int removedCount = 0;
            
            // Count how many admins we have total
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            int currentAdminCount = admins.Count;
            
            // If current admin is trying to demote themselves, ensure there are at least 2 admins
            if (currentUserIncluded && currentAdminCount < 2)
            {
                TempData["ErrorMessage"] = "You cannot remove your own admin rights when you're the only admin.";
                return RedirectToAction(nameof(Index));
            }
            
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
                // We'll allow removing the current user's admin rights now, if there are enough admins
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Admin");
                    removedCount++;
                }
            }
            
            // If the current user removed their own admin rights, sign them out
            if (currentUserIncluded)
            {
                await _signInManager.SignOutAsync();
                TempData["SuccessMessage"] = "Your admin rights have been removed. You've been signed out.";
                return RedirectToAction("Index", "Home");
            }
            
            TempData["SuccessMessage"] = $"Successfully removed admin rights from {removedCount} user(s).";
            return RedirectToAction(nameof(Index));
        }

        private void DeleteTemplateImageFile(string imageUrl)
        {
            // Construct the full file path
            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('/'));

            // Check if the file exists
            if (System.IO.File.Exists(filePath))
            {
                // Delete the file
                System.IO.File.Delete(filePath);
            }
        }

        // Helper method to map users to view models with admin status
        private async Task<List<UserViewModel>> MapUsersToViewModelsAsync(IEnumerable<ApplicationUser> users)
        {
            var viewModels = new List<UserViewModel>();
            
            foreach (var user in users)
            {
                // Map basic properties using AutoMapper
                var viewModel = _mapper.Map<UserViewModel>(user);
                
                // Set IsAdmin property which requires user manager
                viewModel.IsAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                
                viewModels.Add(viewModel);
            }
            
            return viewModels;
        }

        // Helper method to handle user-related entity cleanup with AutoMapper
        private async Task CleanupUserRelatedEntitiesAsync(ApplicationUser user)
        {
            // Delete likes made by the user
            var userLikes = await _dbcontext.TemplateLikes
                .Where(l => l.UserId == user.Id)
                .ToListAsync();
                
            if (userLikes.Any())
            {
                _dbcontext.TemplateLikes.RemoveRange(userLikes);
            }
            
            // Add this code to delete comments authored by the user
            var userComments = await _dbcontext.Comments
                .Where(c => c.AuthorId == user.Id)
                .ToListAsync();
            
            if (userComments.Any())
            {
                _dbcontext.Comments.RemoveRange(userComments);
            }
            
            // Get all templates created by this user
            var userTemplates = await _dbcontext.FormTemplates
                .Include(t => t.Questions)
                    .ThenInclude(q => q.Options)
                .Include(t => t.Responses)
                    .ThenInclude(r => r.Answers)
                .Include(t => t.TemplateTags)
                    .ThenInclude(tt => tt.Tag)
                .Where(t => t.CreatorId == user.Id)
                .ToListAsync();
                
            // Use AutoMapper to map templates to view models for processing if needed
            var templateViewModels = _mapper.Map<List<FormTemplateViewModel>>(userTemplates);
            
            // Delete template images from file system
            foreach (var template in userTemplates.Where(t => !string.IsNullOrEmpty(t.ImageUrl)))
            {
                DeleteTemplateImageFile(template.ImageUrl);
            }
            
            // Handle tag usage counts
            var tagsToUpdate = new Dictionary<int, Tag>();
            foreach (var template in userTemplates)
            {
                foreach (var tt in template.TemplateTags)
                {
                    if (!tagsToUpdate.ContainsKey(tt.TagId))
                    {
                        tagsToUpdate.Add(tt.TagId, tt.Tag);
                    }
                }
            }
            
            // Update tag usage counts
            foreach (var tag in tagsToUpdate.Values)
            {
                tag.UsageCount = Math.Max(0, tag.UsageCount - 1);
                if (tag.UsageCount == 0)
                {
                    _dbcontext.Tags.Remove(tag);
                }
                else
                {
                    _dbcontext.Tags.Update(tag);
                }
            }
            
            // Remove template-related entities and templates
            foreach (var template in userTemplates)
            {
                // Remove answers from responses
                foreach (var response in template.Responses)
                {
                    _dbcontext.Answers.RemoveRange(response.Answers);
                }
                
                // Remove responses
                _dbcontext.FormResponses.RemoveRange(template.Responses);
                
                // Remove question options
                foreach (var question in template.Questions)
                {
                    _dbcontext.QuestionOptions.RemoveRange(question.Options);
                }
                
                // Remove questions
                _dbcontext.Questions.RemoveRange(template.Questions);
                
                // Remove template tags
                _dbcontext.TemplateTags.RemoveRange(template.TemplateTags);
            }
            
            // Finally remove the templates
            _dbcontext.FormTemplates.RemoveRange(userTemplates);
        }
    }
} 