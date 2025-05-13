using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using FormsApp.Models;
using FormsApp.Services;
using FormsApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FormsApp.Controllers
{
    [Authorize]
    public class SupportController : Controller
    {
        private readonly IDropboxService _dropboxService;
        private readonly ILogger<SupportController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public SupportController(
            IDropboxService dropboxService, 
            ILogger<SupportController> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _dropboxService = dropboxService;
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult CreateTicket(string template = null, string returnUrl = null)
        {
            // Get the current URL if returnUrl is not provided
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = Request.Headers["Referer"].ToString();
                if (string.IsNullOrEmpty(returnUrl))
                {
                    returnUrl = Url.Action("Index", "Home");
                }
            }

            var viewModel = new SupportTicketViewModel
            {
                ReportedBy = User.Identity.Name,
                Template = template ?? "NoTemplate", // Ensure Template is never null
                Link = returnUrl,
                ReturnUrl = returnUrl
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTicket(SupportTicketViewModel model)
        {
            _logger.LogInformation("CreateTicket POST received with model: {@Model}", model);

            // Check if there's a "Template" field in the form data
            if (Request.Form.ContainsKey("Template"))
            {
                var templateValue = Request.Form["Template"].ToString();
                _logger.LogInformation("Template value from form: {TemplateValue}", templateValue);
                
                // If Template wasn't included in model binding, set it manually
                if (model.Template == null)
                {
                    model.Template = templateValue;
                    _logger.LogInformation("Manually set Template to: {Template}", model.Template);
                }
            }

            // Ensure Template has a value in any case
            if (model.Template == null)
            {
                model.Template = "NoTemplate";
                _logger.LogInformation("Set default Template value: {Template}", model.Template);
            }

            // Log all validation errors in detail
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Any())
                    .Select(x => new { 
                        Property = x.Key, 
                        Errors = x.Value.Errors.Select(e => e.ErrorMessage).ToList() 
                    })
                    .ToList();

                foreach (var error in errors)
                {
                    _logger.LogWarning("Validation error for {Property}: {Errors}", 
                        error.Property, string.Join(", ", error.Errors));
                }

                return View(model);
            }

            try
            {
                // Create the support ticket
                var ticket = new SupportTicket
                {
                    ReportedBy = model.ReportedBy ?? User.Identity.Name,
                    Summary = model.Summary,
                    Template = model.Template, // Template should be set by now
                    Link = model.Link ?? Request.Headers["Referer"].ToString(),
                    Priority = model.Priority,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Uploading ticket to Dropbox: {@Ticket}", ticket);

                // Upload to Dropbox
                string path = await _dropboxService.UploadJsonFileAsync(ticket);

                _logger.LogInformation("Ticket successfully uploaded to Dropbox path: {Path}", path);

                TempData["SuccessMessage"] = "Your support ticket has been successfully submitted.";
                
                // Redirect back to the original page
                return Redirect(model.ReturnUrl ?? Url.Action("Index", "Home"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting support ticket");
                ModelState.AddModelError("", $"Failed to submit support ticket: {ex.Message}");
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult DiagnoseFormValidation()
        {
            var model = new SupportTicketViewModel
            {
                ReportedBy = User.Identity.Name,
                Summary = "Test Summary",
                Priority = "High",
                Link = Request.Headers["Referer"].ToString(),
                Template = "" // Empty Template to see if it fails validation
            };

            // Create a simple diagnostic result
            var validationResults = new List<string>
            {
                $"SupportTicketViewModel Properties:",
                $"ReportedBy: {model.ReportedBy}",
                $"Summary: {model.Summary}",
                $"Priority: {model.Priority}",
                $"Link: {model.Link}",
                $"Template: '{model.Template}' (Length: {model.Template?.Length ?? 0})",
                $"ReturnUrl: {model.ReturnUrl}",
                
                $"\nSupportTicket Required Fields:",
                $"Id: Not Required",
                $"ReportedBy: Required",
                $"Summary: Required",
                $"Template: Not Required",
                $"Link: Required",
                $"Priority: Required",
                $"CreatedAt: Not Required"
            };

            return Content(string.Join("\n", validationResults));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult TestDropbox()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestDropbox(string summary)
        {
            try
            {
                var ticket = new SupportTicket
                {
                    ReportedBy = User.Identity.Name,
                    Summary = summary ?? "Test ticket",
                    Template = "", // Make sure Template is not null
                    Link = Request.Headers["Referer"].ToString(),
                    Priority = "Low",
                    CreatedAt = DateTime.UtcNow
                };

                string path = await _dropboxService.UploadJsonFileAsync(ticket);

                ViewBag.SuccessMessage = $"File uploaded successfully to {path}";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error: {ex.Message}";
                return View();
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TestToken()
        {
            try
            {
                string accessToken = _configuration["Dropbox:AccessToken"];
                if (string.IsNullOrEmpty(accessToken))
                {
                    return Content("Dropbox:AccessToken is not configured in appsettings.json");
                }

                // Create HTTP client
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // Test API with a simple request - getting account info
                var url = "https://api.dropboxapi.com/2/users/get_current_account";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent("null", Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content($"Dropbox token is valid. Account info: {content}");
                }
                else
                {
                    return Content($"Dropbox token test failed. Status: {response.StatusCode}, Response: {content}");
                }
            }
            catch (Exception ex)
            {
                return Content($"Error testing Dropbox token: {ex.Message}");
            }
        }
    }
} 