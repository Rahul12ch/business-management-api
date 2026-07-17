using client.Models;
using Microsoft.Extensions.Options;
using Resend;

namespace client.Services
{
    public class EmailSender
    {
        private readonly IResend _resend;
        private readonly EmailSettings _settings;
        public EmailSender( IResend resend, IOptions<EmailSettings> options)
        {
            _resend = resend;
            _settings = options.Value;
        }
        public async Task SendAsync(client.Models.EmailMessage message)
        {
            var email = new Resend.EmailMessage
            {
                Subject = message.Subject
            };
          
            email.From = $"{_settings.DisplayName} <{_settings.From}>";
            if (!string.IsNullOrWhiteSpace(message.To))
            { email.To.Add(message.To); }
            else
            {
                foreach (var admin in _settings.AdminEmails)
                { email.To.Add(admin); }
            }

            if (message.IsHtml) email.HtmlBody = message.Body;
            else email.TextBody = message.Body;

            if (!string.IsNullOrWhiteSpace(message.AttachmentPath))
            {
                email.Attachments ??= new List<EmailAttachment>();
                email.Attachments.Add( EmailAttachment.From(message.AttachmentPath));
            }

            if (message.AttachmentBytes != null && !string.IsNullOrWhiteSpace(message.AttachmentName))
            {
                email.Attachments ??= new List<EmailAttachment>();
                email.Attachments.Add(new EmailAttachment
                {
                    Filename = message.AttachmentName,
                    Content = message.AttachmentBytes,
                    ContentType = "application/pdf"
                });
            }
            var result = await _resend.EmailSendAsync(email);
            if (!result.Success)
            {
                throw new Exception(result.Exception?.Message ?? "Failed to send email.");
            }
        }
    }
}