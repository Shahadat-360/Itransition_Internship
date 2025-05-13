using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FormsApp.Models;

namespace FormsApp.Services
{
    public interface IDropboxService
    {
        Task<string> UploadJsonFileAsync(string jsonContent, string fileName);
        Task<string> UploadJsonFileAsync(SupportTicket ticket);
        Task<string> CreateFileRequestAsync(string title, string description, string destination = null);
    }

    public class DropboxService : IDropboxService
    {
        private readonly string _accessToken;
        private readonly HttpClient _httpClient;
        private readonly ILogger<DropboxService> _logger;
        private const string FOLDER_PATH = "/SupportTicketsForms";

        public DropboxService(IConfiguration configuration, HttpClient httpClient, ILogger<DropboxService> logger)
        {
            _accessToken = configuration["Dropbox:AccessToken"];
            _httpClient = httpClient;
            _logger = logger;

            if (string.IsNullOrEmpty(_accessToken))
            {
                _logger.LogError("Dropbox access token is not configured in appsettings.json");
                throw new InvalidOperationException("Dropbox access token is not configured");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }

        public async Task<string> UploadJsonFileAsync(string jsonContent, string fileName)
        {
            try
            {
                _logger.LogInformation("Starting upload to Dropbox for file {FileName}", fileName);

                // Check if the folder exists, and create it if not
                await EnsureFolderExistsAsync();

                // Create a unique filename
                string path = $"{FOLDER_PATH}/{fileName}.json";

                _logger.LogInformation("Target Dropbox path: {Path}", path);

                // Upload the file to Dropbox using the files_upload endpoint
                var uploadUrl = "https://content.dropboxapi.com/2/files/upload";
                
                var dropboxArg = new
                {
                    path = path,
                    mode = "add",
                    autorename = true,
                    mute = false
                };

                var argJson = JsonSerializer.Serialize(dropboxArg);
                _logger.LogDebug("Dropbox API arguments: {Arguments}", argJson);

                var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
                request.Headers.Add("Dropbox-API-Arg", argJson);
                
                // Convert string content to stream
                byte[] contentBytes = Encoding.UTF8.GetBytes(jsonContent);
                var stream = new MemoryStream(contentBytes);
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                request.Content = streamContent;

                _logger.LogInformation("Sending request to Dropbox API...");
                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to upload file to Dropbox. Status: {Status}, Response: {Response}", 
                        response.StatusCode, errorContent);
                    throw new Exception($"Failed to upload file to Dropbox: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Dropbox API response: {Response}", responseContent);
                
                var responseData = JsonSerializer.Deserialize<DropboxUploadResponse>(responseContent);
                _logger.LogInformation("File successfully uploaded to Dropbox. Path: {Path}, Name: {Name}", 
                    responseData?.PathDisplay, responseData?.Name);
                
                return path;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading to Dropbox");
                throw new Exception($"Error uploading to Dropbox: {ex.Message}", ex);
            }
        }

        public async Task<string> UploadJsonFileAsync(SupportTicket ticket)
        {
            // Convert the ticket to JSON
            string json = JsonSerializer.Serialize(ticket, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Create a unique filename
            string fileName = $"support_ticket_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            
            return await UploadJsonFileAsync(json, fileName);
        }

        public async Task<string> CreateFileRequestAsync(string title, string description, string destination = null)
        {
            try
            {
                _logger.LogInformation("Creating file request in Dropbox with title: {Title}", title);

                string folderPath = destination ?? FOLDER_PATH;
                
                // Create file request using the file_requests/create endpoint
                var createRequestUrl = "https://api.dropboxapi.com/2/file_requests/create";
                
                var requestBody = new
                {
                    title = title,
                    destination = folderPath,
                    description = description ?? "",
                    open = true
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                _logger.LogDebug("File request creation payload: {Payload}", jsonContent);

                var request = new HttpRequestMessage(HttpMethod.Post, createRequestUrl);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create file request. Status: {Status}, Response: {Response}", 
                        response.StatusCode, errorContent);
                    throw new Exception($"Failed to create file request: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("File request creation response: {Response}", responseContent);
                
                // Parse response to get the URL
                using var doc = JsonDocument.Parse(responseContent);
                string url = doc.RootElement.GetProperty("url").GetString();
                
                _logger.LogInformation("File request successfully created. URL: {Url}", url);
                
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating file request in Dropbox");
                throw new Exception($"Error creating file request in Dropbox: {ex.Message}", ex);
            }
        }

        private async Task EnsureFolderExistsAsync()
        {
            try
            {
                // Since the folder already exists in Dropbox, we'll just log this info and return
                _logger.LogInformation("Folder {FolderPath} already exists in Dropbox, skipping folder creation", FOLDER_PATH);
                return;
                
                // Keeping the commented code for reference
                /*
                // Try to get metadata for the folder
                var metadataUrl = "https://api.dropboxapi.com/2/files/get_metadata";
                var metadataBody = new
                {
                    path = FOLDER_PATH,
                    include_media_info = false,
                    include_deleted = false,
                    include_has_explicit_shared_members = false
                };

                var metadataJson = JsonSerializer.Serialize(metadataBody);
                var metadataRequest = new HttpRequestMessage(HttpMethod.Post, metadataUrl);
                metadataRequest.Content = new StringContent(metadataJson, Encoding.UTF8, "application/json");

                var metadataResponse = await _httpClient.SendAsync(metadataRequest);
                
                // If the folder exists, we're good
                if (metadataResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Folder {FolderPath} already exists in Dropbox", FOLDER_PATH);
                    return;
                }

                // If we get a 409 error, the folder doesn't exist and we need to create it
                _logger.LogInformation("Folder {FolderPath} doesn't exist, creating it", FOLDER_PATH);

                // Create the folder
                var createFolderUrl = "https://api.dropboxapi.com/2/files/create_folder_v2";
                var createFolderBody = new
                {
                    path = FOLDER_PATH,
                    autorename = false
                };

                var createFolderJson = JsonSerializer.Serialize(createFolderBody);
                var createFolderRequest = new HttpRequestMessage(HttpMethod.Post, createFolderUrl);
                createFolderRequest.Content = new StringContent(createFolderJson, Encoding.UTF8, "application/json");

                var createFolderResponse = await _httpClient.SendAsync(createFolderRequest);
                
                if (!createFolderResponse.IsSuccessStatusCode)
                {
                    var errorContent = await createFolderResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create folder in Dropbox. Status: {Status}, Response: {Response}", 
                        createFolderResponse.StatusCode, errorContent);
                    throw new Exception($"Failed to create folder in Dropbox: {createFolderResponse.StatusCode} - {errorContent}");
                }

                _logger.LogInformation("Successfully created folder {FolderPath} in Dropbox", FOLDER_PATH);
                */
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring folder exists in Dropbox");
                throw new Exception($"Error ensuring folder exists in Dropbox: {ex.Message}", ex);
            }
        }
    }

    // Response model for Dropbox upload
    public class DropboxUploadResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string PathLower { get; set; }
        public string PathDisplay { get; set; }
    }
} 