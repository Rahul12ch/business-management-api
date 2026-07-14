namespace client.Models
{
    public class EmailMessage
    {
        public string To { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Body { get; set; } = "";
        public bool IsHtml { get; set; } = true;
        public string? AttachmentPath { get; set; }
        public byte[]? AttachmentBytes { get; set; }
        public string? AttachmentName { get; set; }
    }
}