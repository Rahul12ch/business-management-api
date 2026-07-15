using client.Data;
using client.Models;
using client.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace client.Controllers;
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private static readonly TimeZoneInfo IndiaTimeZone =
    OperatingSystem.IsWindows()
        ? TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")
        : TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
    public PaymentsController(AppDbContext context)
    {  _context = context; }

    [HttpGet]
    public async Task<IActionResult> GetPayments(
        string? search = null, int page = 1, int pageSize = 10)
    {
        var query = _context.TaskPayments
        .Include(p => p.Task) .ThenInclude(t => t!.Customer) .AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        { search = search.Trim();
          query = query.Where(p =>  p.PaymentMode.Contains(search) ||
        p.PaymentStatus.Contains(search) || (p.Task != null &&  p.Task.TaskName.Contains(search)) ||
        (p.Task != null && p.Task.Customer != null && p.Task.Customer.CustomerName.Contains(search)));
        }
        var totalRecords = await query.CountAsync();
        var payments = await query .OrderByDescending(p => p.PaymentDate) .Skip((page - 1) * pageSize) .Take(pageSize) .Select(p => new
        { p.PaymentId, p.TaskId, p.AmountPaid, p.PaymentDate, p.PaymentMode, p.PaymentStatus,
          TaskName = p.Task != null ? p.Task.TaskName : "", Customer = p.Task != null && p.Task.Customer != null  ? p.Task.Customer.CustomerName : "" })
         .ToListAsync();
        return Ok(new
        {
            totalRecords, page, pageSize, data = payments
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPayment(int id)
    {
        var payment = await _context.TaskPayments
        .Include(p => p.Task) .ThenInclude(t => t!.Customer) .FirstOrDefaultAsync(p => p.PaymentId == id);
        if (payment == null)  return NotFound("Payment not found");
        return Ok(new
        { payment.PaymentId, payment.TaskId, payment.AmountPaid, payment.PaymentDate,payment.PaymentMode, payment.PaymentStatus,
          TaskName = payment.Task?.TaskName ?? "", CustomerName = payment.Task?.Customer?.CustomerName ?? ""
        });
    }

    [HttpPost]
    public async Task<IActionResult> AddPayment(TaskPayment payment)
    {
        var task = await _context.Tasks.Include(x => x.Customer).FirstOrDefaultAsync( x => x.TaskId == payment.TaskId );
        if (task == null) return BadRequest("Task not found");
        var indiaNow = TimeZoneInfo.ConvertTimeFromUtc( DateTime.UtcNow, IndiaTimeZone);
        decimal balance = Math.Max(0, task.GrandTotal - payment.AmountPaid);
        payment.PaymentDate = indiaNow;
        if (string.IsNullOrWhiteSpace(payment.PaymentStatus)) payment.PaymentStatus = "Pending";
        _context.TaskPayments.Add(payment);
        await _context.SaveChangesAsync();
        _context.Notifications.Add(new Notification
        {
        Title = "Payment Received", Message = $"₹{payment.AmountPaid:N2} received via {payment.PaymentMode} from {task.Customer?.CustomerName} for Order #{task.OrderNo}.",
            Type = "Payment", ReferenceId = payment.PaymentId, IsRead = false, CreatedAt = indiaNow });
        await _context.SaveChangesAsync();
        return Ok(new
        { payment.PaymentId, payment.TaskId, payment.AmountPaid, payment.PaymentDate, payment.PaymentMode, payment.PaymentStatus });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePayment(int id, TaskPayment payment)
    {
        if (id != payment.PaymentId) return BadRequest("Payment ID mismatch");
        var existingPayment = await _context.TaskPayments.FindAsync(id);
        if (existingPayment == null) return NotFound("Payment not found");
        var task = await _context.Tasks.FindAsync(existingPayment.TaskId);
        if (task == null) return BadRequest("Task not found");
        var indiaNow = TimeZoneInfo.ConvertTimeFromUtc( DateTime.UtcNow, IndiaTimeZone);
        existingPayment.AmountPaid = payment.AmountPaid; existingPayment.PaymentMode = payment.PaymentMode; existingPayment.PaymentStatus = payment.PaymentStatus;
        decimal balance = Math.Max( 0, task.GrandTotal - existingPayment.AmountPaid);
        _context.Notifications.Add(new Notification
        {
            Title = "Payment Updated", Message = $"Payment for Order #{task.OrderNo} updated.",
            Type = "Payment", ReferenceId = existingPayment.PaymentId, IsRead = false, CreatedAt = indiaNow });
        await _context.SaveChangesAsync();
        return Ok(new
        {
        existingPayment.PaymentId, existingPayment.TaskId, existingPayment.AmountPaid, existingPayment.PaymentDate, existingPayment.PaymentMode, existingPayment.PaymentStatus
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePayment(int id)
    {
        var payment = await _context.TaskPayments.FindAsync(id);
        if (payment == null) return NotFound("Payment not found");
        var task = await _context.Tasks.FindAsync(payment.TaskId);
        if (task == null) return BadRequest("Task not found");
        _context.TaskPayments.Remove(payment);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Payment deleted successfully" });
    }
}