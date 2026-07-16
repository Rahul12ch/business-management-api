using client.Data;
using client.Models;
using client.Services;
using client.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace client.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _context;
    public CustomersController( AppDbContext context, EmailSender emailSender)
    {
        _context = context;
    }
    [HttpGet]
    public async Task<IActionResult> GetCustomers(
        string? search = null,
        int page = 1, int pageSize = 10)
    {
        var query = _context.Customers
       .Include(c => c.Tasks).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c =>
                c.CustomerName.Contains(search) ||
                c.PhoneNumber.Contains(search) ||
                (c.Email != null && c.Email.Contains(search)) ||
                (c.Address != null && c.Address.Contains(search)));
        }
        var totalRecords = await query.CountAsync();
        var customers = await query
           .OrderByDescending(c => c.CustomerId) .Skip((page - 1) * pageSize) .Take(pageSize) .Select(c => new
            {  c.CustomerId,
               OrderNo = c.Tasks .OrderByDescending(t => t.OrderNo) .Select(t => t.OrderNo) .FirstOrDefault(),
                c.CustomerName, c.PhoneNumber, c.Email, c.Address })
            .ToListAsync();
        return Ok(new { totalRecords, page, pageSize, data = customers });
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomer(int id)
    {
        var customer = await _context.Customers .FirstOrDefaultAsync(c => c.CustomerId == id);
        if (customer == null) return NotFound("Customer not found");
        return Ok(customer);
    }
    [HttpPost]
    public async Task<IActionResult> AddCustomer(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.CustomerName))  return BadRequest("Customer name is required");
        if (string.IsNullOrWhiteSpace(customer.PhoneNumber))   return BadRequest("Phone number is required");
        _context.Customers.Add(customer); var utcNow = DateTimeHelper.UtcNow();
        _context.Notifications.Add(new Notification
        {
         Title = "Customer Added", Message = $"{customer.CustomerName} added.",
         Type = "Customer", ReferenceId = customer.CustomerId, IsRead = false, CreatedAt = utcNow });
        await _context.SaveChangesAsync();
        return Ok(customer);
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(int id, Customer customer)
    {
        if (id != customer.CustomerId) return BadRequest("Customer ID mismatch");
        var existingCustomer = await _context.Customers.FindAsync(id);
        var utcNow = DateTimeHelper.UtcNow();
        if (existingCustomer == null)  return NotFound("Customer not found");
        existingCustomer.CustomerName = customer.CustomerName;
        existingCustomer.PhoneNumber = customer.PhoneNumber;
        existingCustomer.Email = customer.Email;
        existingCustomer.Address = customer.Address;
        _context.Notifications.Add(new Notification
        { Title = "Customer Updated", Message = $"{existingCustomer.CustomerName} details updated.",
          Type = "Customer", ReferenceId = existingCustomer.CustomerId, IsRead = false, CreatedAt = utcNow });
        await _context.SaveChangesAsync();
        return Ok(existingCustomer);
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var customer = await _context.Customers .Include(c => c.Tasks) .ThenInclude(t => t.TaskPayments) .Include(c => c.Tasks) .ThenInclude(t => t.TaskDetails).FirstOrDefaultAsync(c => c.CustomerId == id);
        if (customer == null) return NotFound("Customer not found");
        var utcNow = DateTimeHelper.UtcNow();
        var taskCount = customer.Tasks?.Count ?? 0;
        if (customer.Tasks?.Any() == true)
        {
            foreach (var task in customer.Tasks)
            {
                if (task.TaskPayments?.Any() == true) _context.TaskPayments.RemoveRange(task.TaskPayments);
                if (task.TaskDetails?.Any() == true) _context.TaskDetails.RemoveRange(task.TaskDetails);
            }
            _context.Tasks.RemoveRange(customer.Tasks);
        }
        _context.Customers.Remove(customer);
        _context.Notifications.Add(new Notification
        {
        Title = "Customer Deleted", Message = $"{customer.CustomerName} deleted.",
        Type = "Customer", ReferenceId = customer.CustomerId, IsRead = false, CreatedAt = utcNow });
        await _context.SaveChangesAsync();
        return Ok(new
        {
            message = "Customer deleted successfully.",
            deletedTasks = taskCount
        });
    }
}
