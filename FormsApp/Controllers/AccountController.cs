using FormsApp.Models;
using FormsApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;

namespace FormsApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AccountController>logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.UserName.Trim(),
                    Email = model.Email.Trim().ToLower()
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await AddEmailClaimIfMissingAsync(user, isPersistent: false);
                    TempData["SuccessMessage"] = "Your account has been created successfully.";
                    return RedirectToLocal(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }
        
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email.Trim().ToLower());

                if (user == null)
                {
                    TempData["ErrorMessage"] = "Invalid login attempt. User not found.";
                    return View(model);
                }

                if (user.IsBlocked)
                {
                    TempData["ErrorMessage"] = "Your account has been blocked by an administrator.";
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} logged in successfully.", model.Email);
                    await AddEmailClaimIfMissingAsync(user, model.RememberMe);
                    TempData["SuccessMessage"] = "You have successfully logged in.";
                    return RedirectToLocal(returnUrl);
                }

                TempData["ErrorMessage"] = "Invalid login attempt. Please check your email and password.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "There is an issue with {Email}", model.Email);
                TempData["ErrorMessage"] = "There's an issue with your account. Please contact the administrator.";
            }

            return View(model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["InfoMessage"] = "You have been logged out.";
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
        
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            if (Request.Cookies.TryGetValue("BlockedNotice", out string blockedMessage))
            {
                Response.Cookies.Delete("BlockedNotice");
                TempData["ErrorMessage"] = blockedMessage;
            }
            else
            {
                TempData["ErrorMessage"] = "You do not have permission to access this resource.";
            }
            
            return View();
        }
        
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            
            if (result.Succeeded)
            {
                // Refresh the sign-in cookie
                await _signInManager.RefreshSignInAsync(user);
                
                _logger.LogInformation("User {UserId} changed their password successfully.", user.Id);
                TempData["SuccessMessage"] = "Your password has been changed successfully.";
                return RedirectToAction("Index", "Home");
            }
            
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            
            return View(model);
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(HomeController.Index), "Home");
        }

        private async Task AddEmailClaimIfMissingAsync(ApplicationUser user, bool isPersistent)
        {
            var claims = await _userManager.GetClaimsAsync(user);

            if (!claims.Any(c => c.Type == ClaimTypes.Email))
            {
                _logger.LogInformation("Adding missing email claim for user {UserId}.", user.Id);
                await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Email, user.Email));
                await _signInManager.SignInAsync(user, isPersistent);
            }
        }

    }
} 