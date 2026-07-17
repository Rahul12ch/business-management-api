namespace client.Models
{
    public class EmailSettings
    {
        public string DisplayName { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public List<string> AdminEmails { get; set; } = new();
        public string ApiKey { get; set; } = string.Empty;
    }
}