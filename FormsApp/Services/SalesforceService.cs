using FormsApp.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace FormsApp.Services
{
    public class SalesforceService : ISalesforceService
    {
        private readonly HttpClient _httpClient;
        private readonly SalesforceConfig _config;
        private readonly ILogger<SalesforceService> _logger;
        private string _accessToken;
        private string _instanceUrl;

        public SalesforceService(
            HttpClient httpClient,
            IOptions<SalesforceConfig> config,
            ILogger<SalesforceService> logger)
        {
            _httpClient = httpClient;
            _config = config.Value;
            _logger = logger;
        }

        public async Task<string> AuthenticateAsync()
        {
            try
            {
                var authData = new Dictionary<string, string>
                {
                    { "grant_type", "password" },
                    { "client_id", _config.ClientId },
                    { "client_secret", _config.ClientSecret },
                    { "username", _config.Username },
                    { "password", _config.Password + _config.SecurityToken }
                };

                var content = new FormUrlEncodedContent(authData);
                var response = await _httpClient.PostAsync(_config.LoginUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Salesforce authentication failed with status {StatusCode}: {ErrorContent}",
                        response.StatusCode, errorContent);
                    throw new Exception($"Salesforce authentication failed: {response.StatusCode}");
                }

                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                _accessToken = result.GetProperty("access_token").GetString();
                _instanceUrl = result.GetProperty("instance_url").GetString();

                return _accessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating with Salesforce");
                throw;
            }
        }

        public async Task<string> CreateAccountAsync(SalesforceAccount account)
        {
            if (string.IsNullOrEmpty(_accessToken))
            {
                await AuthenticateAsync();
            }

            var url = $"{_instanceUrl}/services/data/{_config.ApiVersion}/sobjects/Account";
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.PostAsJsonAsync(url, account);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create Salesforce Account: {ErrorContent}", errorContent);
                throw new Exception($"Failed to create Salesforce Account: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            return result.GetProperty("id").GetString();
        }

        public async Task<string> CreateContactAsync(SalesforceContact contact)
        {
            if (string.IsNullOrEmpty(_accessToken))
            {
                await AuthenticateAsync();
            }

            var url = $"{_instanceUrl}/services/data/{_config.ApiVersion}/sobjects/Contact";
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.PostAsJsonAsync(url, contact);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create Salesforce Contact: {ErrorContent}", errorContent);
                throw new Exception($"Failed to create Salesforce Contact: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            return result.GetProperty("id").GetString();
        }
    }
}