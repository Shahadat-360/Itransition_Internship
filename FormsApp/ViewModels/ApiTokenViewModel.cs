namespace FormsApp.ViewModels
{
    public class ApiTokenViewModel
    {
        public string? ApiToken { get; set; }
        public bool HasActiveToken { get; set; }
        public string ApiEndpointBaseUrl { get; set; } = string.Empty;
        
        // Sample API URLs for documentation
        public string GetAllTemplatesUrl => $"{ApiEndpointBaseUrl}/templates?token={ApiToken}";
        public string GetTemplateByIdUrl => $"{ApiEndpointBaseUrl}/templates/{{templateId}}?token={ApiToken}";
    }
} 