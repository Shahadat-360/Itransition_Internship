using System.Text.Json.Serialization;

namespace FormsApp.Models
{
    public class SalesforceAccount
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        [JsonPropertyName("Website")]
        public string Website { get; set; }

        [JsonPropertyName("Industry")]
        public string Industry { get; set; }

        [JsonPropertyName("Description")]
        public string Description { get; set; }

        [JsonPropertyName("Phone")]
        public string Phone { get; set; }

        [JsonPropertyName("BillingCity")]
        public string City { get; set; }

        [JsonPropertyName("BillingCountry")]
        public string Country { get; set; }
    }
}