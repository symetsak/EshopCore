namespace Eshop.SystemPanel.Models
{
    public class HealthCheckResponse
    {
        // Χρησιμοποιούμε το [JsonPropertyName] αν χρειαστεί στο μέλλον, αλλά το Blazor 
        // συνήθως κάνει αυτόματο mapping τα μικρά γράμματα του JSON στα κεφαλαία της C#
        public string Status { get; set; } = "";
        public List<HealthCheckItem> Checks { get; set; } = new();
    }

    public class HealthCheckItem
    {
        public string Name { get; set; } = "";
        public string Status { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
