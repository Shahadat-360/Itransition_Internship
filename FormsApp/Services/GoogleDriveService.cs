using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.IO;

namespace FormsApp.Services
{
    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<GoogleDriveService> _logger;
        private readonly string _environment;

        public GoogleDriveService(IConfiguration configuration, IHttpClientFactory clientFactory, ILogger<GoogleDriveService> logger)
        {
            _configuration = configuration;
            _clientFactory = clientFactory;
            _logger = logger;
            _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        }

        public async Task<string> UploadJsonFileAsync(string jsonContent, string fileName)
        {
            try
            {
                // Get the API key and other settings from configuration
                var apiKey = _configuration["GoogleDrive:ApiKey"];
                var folderId = _configuration["GoogleDrive:FolderId"];
                var uploadEndpoint = _configuration["GoogleDrive:UploadEndpoint"];

                _logger.LogInformation($"Starting upload to Google Drive - API Key: {apiKey?.Substring(0, 5)}..., Folder ID: {folderId}");

                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(uploadEndpoint))
                {
                    _logger.LogError("Google Drive API key or upload endpoint not configured");
                    return string.Empty;
                }

                if (string.IsNullOrEmpty(folderId))
                {
                    _logger.LogWarning("Google Drive folder ID is empty. File will be uploaded to the root folder.");
                }

                // Skip actual upload in development environment to avoid API rate limits
                if (_environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Development mode: Simulating successful upload to Google Drive");
                    // Return the filename that would have been used
                    return $"{DateTime.UtcNow:yyyyMMddHHmmss}_{fileName}.json";
                }

                // Create a unique filename
                var uniqueFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{fileName}.json";
                
                // Log the file info
                _logger.LogInformation($"Preparing to upload: {uniqueFileName}");
                _logger.LogDebug($"Content: {jsonContent}");

                // Create metadata for the file
                var metadata = new
                {
                    name = uniqueFileName,
                    mimeType = "application/json",
                    parents = folderId != null ? new[] { folderId } : null
                };

                var metadataJson = JsonSerializer.Serialize(metadata);
                _logger.LogInformation($"File metadata: {metadataJson}");

                // Create HTTP client
                var client = _clientFactory.CreateClient();
                
                // Set up the request URL
                var requestUrl = $"{uploadEndpoint}?uploadType=multipart&key={apiKey}&supportsAllDrives=true";
                _logger.LogInformation($"Request URL: {requestUrl}");
                
                // Generate a unique boundary string
                string boundary = $"----WebKitFormBoundary{Guid.NewGuid():N}";
                
                // Create multipart content with metadata and file parts
                using var content = new MultipartContent("related", boundary);
                
                // Add metadata part
                var metadataContent = new StringContent(metadataJson, Encoding.UTF8, "application/json");
                metadataContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                content.Add(metadataContent);
                
                // Add file content part
                var fileContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                content.Add(fileContent);
                
                // Send the request
                _logger.LogInformation("Sending request to Google Drive API...");
                var response = await client.PostAsync(requestUrl, content);
                
                // Log response details
                _logger.LogInformation($"Response status: {(int)response.StatusCode} {response.StatusCode}");
                
                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"File uploaded successfully: {responseContent}");
                    return uniqueFileName;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to upload file. Status: {response.StatusCode}, Error: {errorContent}");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to Google Drive");
                return string.Empty;
            }
        }
    }
} 