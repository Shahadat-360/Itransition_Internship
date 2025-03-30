using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using User.Management.Data;
using User.Management.Entities;
using User.Management.Enum;
using User.Management.Models;

namespace User.Management.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;
        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ApplicationDbContext context,
            ILogger<AccountController>logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }


        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            var model = new LoginViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _logger.LogInformation($"Attempting login for email: {model.Email}");
                    
                    // Use UserManager to find user
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    
                    if (user == null)
                    {
                        _logger.LogWarning($"No user found with email: {model.Email}");
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        return View(model);
                    }

                    _logger.LogInformation($"User found with ID: {user.Id}");

                    if (user.IsActive == Status.Blocked)
                    {
                        _logger.LogWarning($"Blocked user attempted login: {user.Email}");
                        ModelState.AddModelError(string.Empty, "Your account is Blocked.");
                        return View(model);
                    }

                    // Attempt to sign in
                    var result = await _signInManager.PasswordSignInAsync(
                        user.Email,  // Use UserName instead of Email
                        model.Password,
                        model.RememberMe,
                        lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"User logged in successfully: {user.Email}");
                        
                        // Update last login time
                        user.LastLoginTime = DateTime.UtcNow;
                        await _userManager.UpdateAsync(user);

                        return RedirectToAction(nameof(Index), "Home");
                    }

                    _logger.LogWarning($"Invalid password attempt for user: {user.Email}");
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during login attempt");
                    ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                }
            }
            else
            {
                _logger.LogWarning("ModelState is invalid: " + string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)));
            }

            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Register()
        {
            var model = new RegisterViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var appUser = new AppUser
                    {
                        Id = Guid.NewGuid(),
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        UserName = model.Email,
                        NormalizedEmail = model.Email.ToUpper(),
                        NormalizedUserName = model.Email.ToUpper(),
                        JobTitle = model.JobTitle,
                        IsActive = Status.Active,
                        LastLoginTime = DateTime.UtcNow,
                        EmailConfirmed = true
                    };

                    _logger.LogInformation($"Attempting to create user with email: {model.Email}");

                    var result = await _userManager.CreateAsync(appUser, model.Password);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"User created successfully: {model.Email}");
                        TempData["SuccessMessage"] = "Registration successful! Please login.";
                        return RedirectToAction(nameof(Success));
                    }

                    foreach (var error in result.Errors)
                    {
                        _logger.LogWarning($"User creation error: {error.Description}");
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Database error during registration");
                    ModelState.AddModelError(string.Empty, "Email already exists");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during registration");
                    ModelState.AddModelError(string.Empty, "An error occurred during registration. Please try again.");
                }
            }

            return View(model);
        }
        public IActionResult Success()
            {
                return View();
            }
        }
    }

