using client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace client.Services
{
    public class EmailSender
    {
        private readonly IResend _resend;
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailSender> _logger;
        public EmailSender( IResend resend, IOptions<EmailSettings> options, ILogger<EmailSender> logger)
        {
            _resend = resend; _settings = options.Value; _logger = logger;
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
            {
                throw new InvalidOperationException("No email recipient specified.");
            }
            if (message.IsHtml) email.HtmlBody = message.Body;
            else email.TextBody = message.Body;
         /*   if (!string.IsNullOrWhiteSpace(message.AttachmentPath))
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
            }*/
            try
            {
                var result = await _resend.EmailSendAsync(email);
                if (!result.Success)
                {
                    if (result.Exception != null) throw result.Exception;
                    throw new InvalidOperationException("Failed to send email.");
                }
                _logger.LogInformation( "Email sent successfully to {Recipients}", string.Join(", ", email.To));
            }
            catch (Exception ex)
            {
                _logger.LogError( ex, "Failed to send email to {Recipients}", string.Join(", ", email.To));
                throw;
            }
        }
    }
}