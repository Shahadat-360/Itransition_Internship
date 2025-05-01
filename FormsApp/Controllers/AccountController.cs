using FormsApp.Models;
using FormsApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

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
                var user = new ApplicationUser { UserName = model.UserName, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);
                
                if (result.Succeeded)
                {
                    await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, model.Email));
                    
                    await _signInManager.SignInAsync(user, isPersistent: false);
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
            if (ModelState.IsValid)
            {
                try
                {
                    // Attempt to find user by email
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    
                    // If user exists and is blocked, prevent login
                    if (user != null && user.IsBlocked)
                    {
                        TempData["ErrorMessage"] = "Your account has been blocked by an administrator.";
                        ModelState.AddModelError(string.Empty, "Your account has been blocked by an administrator.");
                        return View(model);
                    }
                    
                    // Check if user was found
                    if (user == null)
                    {
                        TempData["ErrorMessage"] = "Invalid login attempt. User not found.";
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        return View(model);
                    }
                    
                    // Proceed with sign-in using username/password
                    var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);
                    
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User {Email} logged in successfully.", model.Email);
                        var claims = await _userManager.GetClaimsAsync(user);
                        foreach (var claim in claims)
                        {
                            Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
                        }

                        // Check if email claim exists, if not, add it
                        if (!claims.Any(c => c.Type == System.Security.Claims.ClaimTypes.Email))
                        {
                            Console.WriteLine($"Adding email claim for user {user.Id}");
                            await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email));
                            
                            // Sign in again to refresh claims
                            await _signInManager.SignInAsync(user, model.RememberMe);
                        }

                        TempData["SuccessMessage"] = "You have successfully logged in.";
                        return RedirectToLocal(returnUrl);
                    }
                    
                    TempData["ErrorMessage"] = "Invalid login attempt. Please check your email and password.";
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Sequence contains more than one element"))
                {
                    // Handle the case of duplicate emails
                    TempData["ErrorMessage"] = "There's an issue with your account. Please contact the administrator.";
                    ModelState.AddModelError(string.Empty, "Account conflict detected. Please contact support.");
                    
                    // Log the error for administrators
                    // Todo: Add proper logging here
                    Console.WriteLine($"Duplicate email detected: {model.Email}");
                }
                
                return View(model);
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
            // Check if the user was redirected here because they were blocked
            if (Request.Cookies.TryGetValue("BlockedNotice", out string blockedMessage))
            {
                // Remove the cookie
                Response.Cookies.Delete("BlockedNotice");
                
                // Set the error message
                TempData["ErrorMessage"] = blockedMessage;
            }
            else
            {
                TempData["ErrorMessage"] = "You do not have permission to access this resource.";
            }
            
            return View();
        }
        
        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
    }
} 