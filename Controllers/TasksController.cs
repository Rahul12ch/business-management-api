using client.Data;
using client.DTOs;
using client.Models;
using client.Services;
using System.Diagnostics;
using client.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace client.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly EmailService _emailService;
    public TasksController(AppDbContext context, EmailService emailService)
    {
        _context = context; _emailService = emailService;
    }
    [HttpGet]
    public async Task<IActionResult> GetTasks(string? search = null, int page = 1, int pageSize = 10)
    {
        var query = _context.Tasks
        .Include(x => x.Customer) .Include(x => x.TaskPayments) .AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.TaskName.Contains(search) || x.Status.Contains(search) || x.OrderNo.ToString().Contains(search) ||
            (x.Customer != null && x.Customer.CustomerName.Contains(search)));
        }
        var totalRecords = await query.CountAsync();
        var data = await query 
       .OrderByDescending(x => x.OrderNo) .Skip((page - 1) * pageSize) .Take(pageSize) .Select(x => new
        {
         x.TaskId, x.OrderNo, x.TaskName,CreatedDate = DateTimeHelper.ToIndia(x.CreatedDate), DueDate = x.DueDate.HasValue ? DateTimeHelper.ToIndia(x.DueDate.Value): (DateTime?)null,
         x.Status, x.SubTotal, x.IsGstApplied, x.GstPercent, x.GstAmount, x.GrandTotal, x.TotalAmount,
         CustomerName = x.Customer != null ? x.Customer.CustomerName : "", PhoneNumber = x.Customer != null ? x.Customer.PhoneNumber : "",
         PaidAmount = x.TaskPayments == null ? 0 : x.TaskPayments.Sum(p => p.AmountPaid),
         Balance = x.GrandTotal -  (x.TaskPayments == null ? 0 : x.TaskPayments.Sum(p => p.AmountPaid)),
         PaymentStatus = (x.TaskPayments == null ? 0 : x.TaskPayments.Sum(p => p.AmountPaid)) <= 0 ? "Pending" :
         (x.TaskPayments == null ? 0 : x.TaskPayments.Sum(p => p.AmountPaid)) < x.GrandTotal ? "Partial" : "Paid" })
        .ToListAsync();
        return Ok(new { totalRecords, page, pageSize, data });
    }
    [HttpGet("next-order-no")]
    public async Task<IActionResult> GetNextOrderNo()
    {
        var last = await _context.Tasks
        .OrderByDescending(x => x.OrderNo) .FirstOrDefaultAsync();
        return Ok(new
        {
            nextOrderNo = last == null ? 1001 : last.OrderNo + 1
        });
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTask(int id)
    {
        var task = await LoadTask(id);
        if (task == null) return NotFound();
        return Ok(CreateResponse(task));
    }
    [HttpPost]
    public async Task<IActionResult> AddTask(TaskItem task)
    {
        var sw = Stopwatch.StartNew(); Console.WriteLine("Task Save Started");
        if (task.CustomerId <= 0) return BadRequest();
        var last = await _context.Tasks.OrderByDescending(x => x.OrderNo).FirstOrDefaultAsync();
        task.OrderNo = last == null ? 1001 : last.OrderNo + 1; var utcNow = DateTimeHelper.UtcNow();
        task.CreatedDate = utcNow; task.TotalAmount = task.GrandTotal;
        task.Status = string.IsNullOrWhiteSpace(task.Status) ? "Pending" : task.Status; task.Customer = null;
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        Console.WriteLine($"Task saved : {sw.ElapsedMilliseconds} ms");
        var created = await LoadTask(task.TaskId);
        Console.WriteLine($"Task loaded : {sw.ElapsedMilliseconds} ms");
        _context.Notifications.Add(new Notification
        { Title = "Customer Added", Message = $"{created!.Customer?.CustomerName} added for Order #{created.OrderNo}.",
          Type = "Customer", ReferenceId = created.CustomerId, IsRead = false, CreatedAt = utcNow
        });
        _context.Notifications.Add(new Notification
        { Title = "Task Created", Message = $"Order #{created.OrderNo} created for {created.Customer?.CustomerName}.",
          Type = "Task", ReferenceId = created.TaskId, IsRead = false, CreatedAt = utcNow
        });
        await _context.SaveChangesAsync();
        Console.WriteLine("Before Email");
        _ = Task.Run(async () =>
        {
            try
            {
                await _emailService.SendTaskCreatedAsync(
                    created!,
                    CreateInvoiceDto(created!)
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        });

        return Ok(CreateResponse(created!));
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(int id, UpdateTaskDto dto)
    {
        var task = await _context.Tasks .Include(x => x.TaskDetails) .FirstOrDefaultAsync(x => x.TaskId == id);
        if (task == null) return NotFound(); var utcNow = DateTimeHelper.UtcNow();
        var oldStatus = task.Status; task.CustomerId = dto.CustomerId; task.TaskName = dto.TaskName; task.DueDate = dto.DueDate; task.Status = dto.Status; task.SubTotal = dto.SubTotal; 
        task.IsGstApplied = dto.IsGstApplied; task.GstPercent = dto.GstPercent; task.GstAmount = dto.GstAmount; task.GrandTotal = dto.GrandTotal; task.TotalAmount = dto.GrandTotal;
        if (task.TaskDetails != null) _context.TaskDetails.RemoveRange(task.TaskDetails);
        foreach (var item in dto.WorkDetails)
        {
            _context.TaskDetails.Add(new TaskDetail
            {
                TaskId = id, Description = item.Description, Qty = item.Qty, Rate = item.Rate, Amount = item.Amount
            });
        }
        var oldPayments = await _context.TaskPayments .Where(x => x.TaskId == id).ToListAsync();
        if (oldPayments.Any()) _context.TaskPayments.RemoveRange(oldPayments);
        var paymentStatus = dto.PaidAmount <= 0 ? "Pending" : dto.PaidAmount < dto.GrandTotal ? "Partial" : "Paid";
        if (dto.PaidAmount > 0)
        {
            _context.TaskPayments.Add(new TaskPayment
            {
               TaskId = id, AmountPaid = dto.PaidAmount, PaymentDate = utcNow, PaymentMode = dto.PaymentMode, PaymentStatus = paymentStatus
            });
        }
        await _context.SaveChangesAsync();
        var updated = await LoadTask(id);
        _context.Notifications.Add(new Notification
        { Title = "Task Updated", Message = $"Order #{updated!.OrderNo} updated. Status changed from {oldStatus} to {updated.Status}.",
          Type = "Task", ReferenceId = updated.TaskId, IsRead = false, CreatedAt = utcNow });
        await _context.SaveChangesAsync();
        try
        {
            await _emailService.SendTaskUpdatedAsync(  updated!, CreateInvoiceDto(updated!), oldStatus);
        }
        catch (Exception ex)
        { Console.WriteLine(ex); }
        return Ok(CreateResponse(updated!));
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var utcNow = DateTimeHelper.UtcNow();
        var task = await _context.Tasks .Include(x => x.Customer) .Include(x => x.TaskDetails) .Include(x => x.TaskPayments) .FirstOrDefaultAsync( x => x.TaskId == id );
        if (task == null) return NotFound();
        _context.Notifications.Add( new Notification
        { Title = "Task Deleted", Message = $"Order #{task.OrderNo} for {task.Customer?.CustomerName} deleted.",
          Type = "Task", ReferenceId = task.TaskId, IsRead = false, CreatedAt = utcNow });
        if (task.TaskPayments?.Any() == true)
        { _context.TaskPayments.RemoveRange( task.TaskPayments ); }
        if (task.TaskDetails?.Any() == true)
        { _context.TaskDetails.RemoveRange(task.TaskDetails ); }
        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        return Ok( new { message = "Task deleted successfully" });
    }
    private async Task<TaskItem?> LoadTask(int id)
    {
        return await _context.Tasks
        .Include(x => x.Customer).Include(x => x.TaskDetails) .Include(x => x.TaskPayments) .FirstOrDefaultAsync(x => x.TaskId == id);
    }
    private object CreateResponse(TaskItem task)
    {
        var paid = task.TaskPayments? .Sum(x => x.AmountPaid) ?? 0;
        var latestPayment = task.TaskPayments? .OrderByDescending(x => x.PaymentDate) .FirstOrDefault();
        return new
        {
        task.TaskId, task.OrderNo, task.CustomerId, task.TaskName, CreatedDate = DateTimeHelper.ToIndia(task.CreatedDate),
        DueDate = task.DueDate.HasValue ? DateTimeHelper.ToIndia(task.DueDate.Value) : (DateTime?)null, task.Status, task.SubTotal,
        task.IsGstApplied, task.GstPercent, task.GstAmount, task.GrandTotal, task.TotalAmount,
        CustomerName =  task.Customer?.CustomerName ?? "", PhoneNumber = task.Customer?.PhoneNumber ?? "",
        PaidAmount = paid, Balance = Math.Max( 0, task.GrandTotal - paid), PaymentMode = latestPayment?.PaymentMode ?? "N/A",
        PaymentStatus = paid <= 0? "Pending" : paid < task.GrandTotal ? "Partial" : "Paid",
        WorkDetails = task.TaskDetails?
         .Select(x => new
         {
          x.TaskDetailId, x.TaskId, x.Description, x.Qty, x.Rate, x.Amount })
          .ToList() ?? new(),
           TaskPayments =  task.TaskPayments?
         .Select(x => new
            {
          x.PaymentId, x.TaskId, x.AmountPaid, PaymentDate = DateTimeHelper.ToIndia(x.PaymentDate), x.PaymentMode, x.PaymentStatus })
           .ToList() ?? new() };
    }
    private InvoiceDto CreateInvoiceDto(TaskItem task)
    {
        var paid = task.TaskPayments? .Sum(x => x.AmountPaid) ?? 0;
        return new InvoiceDto
        {
            OrderNo = task.OrderNo, InvoiceDate = DateTimeHelper.ToIndia(DateTimeHelper.UtcNow()),
            CustomerName = task.Customer?.CustomerName ?? "", PhoneNumber = task.Customer?.PhoneNumber ?? "",
            Email = task.Customer?.Email ?? "", Address = task.Customer?.Address ?? "", TaskName = task.TaskName, SubTotal = task.SubTotal,
            IsGstApplied = task.IsGstApplied, GstPercent = task.GstPercent, GstAmount = task.GstAmount, GrandTotal = task.GrandTotal,
            PaymentStatus = paid <= 0 ? "Pending" : paid < task.GrandTotal ? "Partial" : "Paid", Items = task.TaskDetails?
            .Select(x => new InvoiceItemDto
            {
            Description = x.Description ?? "", Qty = x.Qty, Rate = x.Rate,Amount = x.Amount
            })
            .ToList() ?? new List<InvoiceItemDto>()
        };
    }
}