using client.DTOs;
using client.Models;
using client.Templates;

namespace client.Services
{
    public class EmailService
    {
        private readonly EmailSender _emailSender;
        private readonly PdfService _pdfService;
        private readonly IWebHostEnvironment _env;
        public EmailService( EmailSender emailSender, PdfService pdfService, IWebHostEnvironment env)
        {
            _emailSender = emailSender; _pdfService = pdfService; _env = env;
        }
        public async Task SendTaskCreatedAsync( TaskItem task, InvoiceDto invoice)
        {
            var admin = AdminTemplates.TaskCreated(task);
            await _emailSender.SendAsync(admin);
            if (invoice.Items.Count == 0)
            {
                invoice.Items = task.TaskDetails? .Select(x => new InvoiceItemDto
                { Description = x.Description ?? "", Qty = x.Qty, Rate = x.Rate, Amount = x.Amount })
                .ToList() ?? new List<InvoiceItemDto>();
            }
            var logoPath = Path.Combine(  _env.WebRootPath, "images", "logo.png");
            var pdf = _pdfService.Generate( invoice, logoPath);
            var customer = CustomerTemplates.TaskCreated(task);
            customer.To = task.Customer?.Email ?? "";
            customer.AttachmentBytes = pdf;
            customer.AttachmentName = $"Invoice-{task.OrderNo}.pdf";
            if (!string.IsNullOrWhiteSpace(customer.To))
            {
                await _emailSender.SendAsync( customer);
            }
        }
        public async Task SendTaskUpdatedAsync( TaskItem task, InvoiceDto invoice, string oldStatus)
        {
            var admin = AdminTemplates.TaskUpdated( task, oldStatus);
            await _emailSender.SendAsync(admin);
            if (invoice.Items.Count == 0)
            {
                invoice.Items = task.TaskDetails? .Select(x => new InvoiceItemDto
        { Description = x.Description ?? "", Qty = x.Qty, Rate = x.Rate, Amount = x.Amount })
        .ToList() ?? new List<InvoiceItemDto>();
        }
            var logoPath = Path.Combine( _env.WebRootPath, "images", "logo.png");
            var pdf = _pdfService.Generate( invoice, logoPath);
            var customer = CustomerTemplates.TaskUpdated(task);
            customer.To = task.Customer?.Email ?? "";
            customer.AttachmentBytes = pdf;
            customer.AttachmentName = $"Invoice-{task.OrderNo}.pdf";
            if (!string.IsNullOrWhiteSpace(customer.To))
            {
                await _emailSender.SendAsync( customer);
            }
        }
    }
}