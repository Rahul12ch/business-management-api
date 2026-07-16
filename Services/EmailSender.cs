using client.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace client.Services
{
    public class EmailSender
    {
        private readonly EmailSettings _settings;

        public EmailSender(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendAsync(EmailMessage message)
        {
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(
                _settings.DisplayName,
                _settings.From));

            if (!string.IsNullOrWhiteSpace(message.To))
            {
                email.To.Add(MailboxAddress.Parse(message.To));
            }
            else
            {
                foreach (var admin in _settings.AdminEmails)
                {
                    email.To.Add(MailboxAddress.Parse(admin));
                }
            }

            email.Subject = message.Subject;

            var body = new BodyBuilder();

            if (message.IsHtml)
                body.HtmlBody = message.Body;
            else
                body.TextBody = message.Body;

            if (!string.IsNullOrWhiteSpace(message.AttachmentPath))
            {
                body.Attachments.Add(message.AttachmentPath);
            }

            if (message.AttachmentBytes != null &&
                !string.IsNullOrWhiteSpace(message.AttachmentName))
            {
                body.Attachments.Add(
                    message.AttachmentName,
                    message.AttachmentBytes);
            }

            email.Body = body.ToMessageBody();

            using var smtp = new SmtpClient();

            try
            {
                Console.WriteLine("==================================");
                Console.WriteLine("SMTP Host: " + _settings.Host);
                Console.WriteLine("SMTP Port: " + _settings.Port);
                Console.WriteLine("SMTP User: " + _settings.Username);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                Console.WriteLine("Connecting SMTP...");

                await smtp.ConnectAsync(
      _settings.Host,
      _settings.Port,
      SecureSocketOptions.SslOnConnect,
      cts.Token);


                Console.WriteLine("SMTP Connected");

                Console.WriteLine("Authenticating...");
                await smtp.AuthenticateAsync(
                    _settings.Username,
                    _settings.Password,
                    cts.Token);

                Console.WriteLine("Authenticated");

                Console.WriteLine("Sending Email...");
                await smtp.SendAsync(email, cts.Token);

                Console.WriteLine("Email Sent Successfully");

                Console.WriteLine("Disconnecting...");
                await smtp.DisconnectAsync(true, cts.Token);

                Console.WriteLine("SMTP Disconnected");
                Console.WriteLine("==================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine("========== EMAIL ERROR ==========");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("=================================");
                throw;
            }
        }
    }
}