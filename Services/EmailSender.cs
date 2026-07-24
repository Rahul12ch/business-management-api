using client.Models;
using Microsoft.Extensions.Options;
using Resend;

namespace client.Services
{
    public class EmailSender
    {
        private readonly IResend _resend;
        private readonly EmailSettings _settings;

        public EmailSender(IResend resend, IOptions<EmailSettings> options)
        {
            _resend = resend;
            _settings = options.Value;
        }

        public async Task SendAsync(client.Models.EmailMessage message)
        {
            var email = new Resend.EmailMessage
            {
                Subject = message.Subject,
                From = $"{_settings.DisplayName} <{_settings.From}>"
            };

            if (!string.IsNullOrWhiteSpace(message.To))
            {
                email.To.Add(message.To);
            }
            else
            {
                foreach (var admin in _settings.AdminEmails)
                {
                    if (!string.IsNullOrWhiteSpace(admin))
                    {
                        email.To.Add(admin);
                    }
                }
            }

            if (!email.To.Any())
                throw new InvalidOperationException("No email recipient specified.");

            if (message.IsHtml)
                email.HtmlBody = message.Body;
            else
                email.TextBody = message.Body;

            if (!string.IsNullOrWhiteSpace(message.AttachmentPath))
            {
                email.Attachments ??= new List<EmailAttachment>();
                email.Attachments.Add(EmailAttachment.From(message.AttachmentPath));
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

            // ===== Temporary diagnostics =====
            Console.WriteLine("========== EMAIL DEBUG ==========");
            Console.WriteLine($"DisplayName : {_settings.DisplayName}");
            Console.WriteLine($"From config : {_settings.From}");
            Console.WriteLine($"From header : {email.From}");
            Console.WriteLine($"Recipients  : {string.Join(", ", email.To)}");
            Console.WriteLine($"ApiKey len  : {_settings.ApiKey?.Length}");
            Console.WriteLine($"ApiKey head : {_settings.ApiKey?[..Math.Min(10, _settings.ApiKey.Length)]}");
            Console.WriteLine("=================================");
            // ================================

            var result = await _resend.EmailSendAsync(email);

            if (!result.Success)
            {
                Console.WriteLine(result.Exception?.ToString());
                throw new InvalidOperationException(
                    result.Exception?.Message ?? "Failed to send email.");
            }
        }
    }
}