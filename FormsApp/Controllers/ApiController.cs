using FormsApp.Models;
using FormsApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

namespace FormsApp.Controllers
{
    [Route("api/v1")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly IApiTokenService _tokenService;
        private readonly FormAggregationService _aggregationService;
        private readonly ILogger<ApiController> _logger;

        public ApiController(
            IApiTokenService tokenService,
            FormAggregationService aggregationService, 
            ILogger<ApiController> logger)
        {
            _tokenService = tokenService;
            _aggregationService = aggregationService;
            _logger = logger;
        }

        // API endpoint to get all templates with aggregated results
        [HttpGet("templates")]
        public async Task<IActionResult> GetTemplates([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { error = "API token is required" });
            }

            // Validate the token
            var isValid = await _tokenService.ValidateTokenAsync(token);
            if (!isValid)
            {
                return Unauthorized(new { error = "Invalid or expired API token" });
            }

            try
            {
                // Get the user ID from the token
                var userId = await _tokenService.GetUserIdFromTokenAsync(token);
                if (userId == null)
                {
                    return Unauthorized(new { error = "Invalid token ownership" });
                }

                // Get aggregated results for all templates owned by the user
                var results = await _aggregationService.GetAggregatedResultsForUserAsync(userId);
                
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting aggregated results");
                return StatusCode(500, new { error = "An error occurred while processing your request" });
            }
        }

        // API endpoint to get aggregated results for a specific template
        [HttpGet("templates/{id}")]
        public async Task<IActionResult> GetTemplateById(int id, [FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { error = "API token is required" });
            }

            // Validate the token
            var isValid = await _tokenService.ValidateTokenAsync(token);
            if (!isValid)
            {
                return Unauthorized(new { error = "Invalid or expired API token" });
            }

            try
            {
                // Get the user ID from the token
                var userId = await _tokenService.GetUserIdFromTokenAsync(token);
                if (userId == null)
                {
                    return Unauthorized(new { error = "Invalid token ownership" });
                }

                // Get aggregated results for the specific template
                var result = await _aggregationService.GetAggregatedResultsByTemplateIdAsync(id, userId);
                if (result == null)
                {
                    return NotFound(new { error = "Template not found or you don't have access to it" });
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting aggregated results for template {TemplateId}", id);
                return StatusCode(500, new { error = "An error occurred while processing your request" });
            }
        }
    }
} 