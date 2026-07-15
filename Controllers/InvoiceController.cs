using client.Data;
using client.DTOs;
using client.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace client.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly PdfService _pdfService;
        private static readonly TimeZoneInfo IndiaTimeZone =
        OperatingSystem.IsWindows() ? TimeZoneInfo.FindSystemTimeZoneById("India Standard Time") : TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        public InvoiceController( AppDbContext context, EmailSender emailSender, IWebHostEnvironment env, PdfService pdfService )
        {
            _context = context; _env = env; _pdfService = pdfService;
        }
        [HttpGet("{taskId}")]
        public async Task<IActionResult> GetInvoice( int taskId)
        {
            var invoice = await CreateInvoice(taskId);
            if (invoice == null) return NotFound();
            return Ok(invoice);
        }
        [HttpGet("{taskId}/pdf")]
        public async Task<IActionResult> DownloadInvoice( int taskId)
        {
         var invoice = await CreateInvoice(taskId);
           if (invoice == null)  return NotFound();
            var logoPath = Path.Combine( _env.WebRootPath, "images", "logo.png");
            var pdf =  _pdfService.Generate( invoice, logoPath );
            return File( pdf, "application/pdf", $"Invoice-{invoice.OrderNo}.pdf" );
        }
        private async Task<InvoiceDto?> CreateInvoice( int taskId)
        {
            var task = await _context.Tasks
         .Include(t => t.Customer) .Include(t => t.TaskDetails) .Include(t => t.TaskPayments) .FirstOrDefaultAsync( t => t.TaskId == taskId);
            if (task == null) return null;
            var indiaNow = TimeZoneInfo.ConvertTimeFromUtc( DateTime.UtcNow, IndiaTimeZone);
            var paidAmount = task.TaskPayments .Sum(x => x.AmountPaid);
            var invoice = new InvoiceDto
                {
                    OrderNo = task.OrderNo, InvoiceDate = indiaNow, CustomerName = task.Customer?.CustomerName ?? "", PhoneNumber = task.Customer?.PhoneNumber ?? "",
                    Email = task.Customer?.Email ?? "", Address = task.Customer?.Address ?? "", TaskName = task.TaskName, IsGstApplied = task.IsGstApplied,
                    SubTotal =task.SubTotal, GstPercent = task.GstPercent, GstAmount = task.GstAmount, GrandTotal = task.GrandTotal,
                    PaymentStatus =paidAmount <= 0 ? "Pending" : paidAmount < task.GrandTotal ? "Partial" : "Paid",  Items = task.TaskDetails
                    .Select(x => new InvoiceItemDto
                    {
                        Description = x.Description ?? "", Qty = x.Qty, Rate = x.Rate, Amount = x.Amount
                    })
                    .ToList()
                };
            _context.Notifications.Add(
                new client.Models.Notification
                {
                    Title = "Invoice Generated", Message =  $"Invoice generated for Order #{task.OrderNo}.",
                    Type = "Invoice", ReferenceId = task.TaskId, IsRead = false, CreatedAt = indiaNow
                });
            await _context.SaveChangesAsync();
            return invoice;
        }
    }
}