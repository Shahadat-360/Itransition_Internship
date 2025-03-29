using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using User.Management.Data;
using User.Management.Entities;
using User.Management.Models;

namespace User.Management.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _context;
        public AccountController(UserManager<AppUser> userManager,ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }


        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid) {
                try
                {
                    var appUser = new AppUser
                    {
                        Id = Guid.NewGuid(),
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        UserName=model.Email,
                        Address = model.Address,
                        IsActive = model.IsActive,
                        LastLoginTime = model.LastLoginTime
                    };

                    var result = await _userManager.CreateAsync(appUser, model.Password);

                    if (result.Succeeded)
                    {
                        TempData["SuccessMessage"] = "Registration successful! Please login.";
                        return RedirectToAction(nameof(Success));
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError(string.Empty, "Email already exists");
                }
                catch (Exception ex)
                {
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

