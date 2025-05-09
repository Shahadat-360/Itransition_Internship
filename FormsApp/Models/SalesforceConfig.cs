namespace FormsApp.Models
{
    public class SalesforceConfig
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string SecurityToken { get; set; }
        public string LoginUrl { get; set; }
        public string ApiVersion { get; set; }
    }
}