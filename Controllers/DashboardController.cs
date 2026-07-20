using System.Globalization;
using client.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace client.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard(string period = "Month")
    {
        try
        {
            var today = DateTime.UtcNow.Date;

            DateTime fromDate = period.ToLower() switch
            {
                "week" => today.AddDays(-7),
                "year" => today.AddYears(-1),
                _ => new DateTime(today.Year, today.Month, 1)
            };

            var totalCustomers = await _context.Customers
                .AsNoTracking()
                .CountAsync();

            var totalTasks = await _context.Tasks
                .AsNoTracking()
                .CountAsync();

            var pendingTasks = await _context.Tasks
                .AsNoTracking()
                .CountAsync(x => x.Status == "Pending");

            var completedTasks = await _context.Tasks
                .AsNoTracking()
                .CountAsync(x => x.Status == "Completed");

            var overdueTasks = await _context.Tasks
                .AsNoTracking()
                .CountAsync(x => x.DueDate < today && x.Status != "Completed");

            var totalRevenue = await _context.TaskPayments
                .AsNoTracking()
                .Where(x => x.PaymentDate >= fromDate)
                .SumAsync(x => (decimal?)x.AmountPaid);

            var pendingAmount = await _context.Tasks
                .AsNoTracking()
                .Where(x => x.Status != "Completed")
                .SumAsync(x => (decimal?)x.GrandTotal);

            var priorityTasks = await _context.Tasks
                .AsNoTracking()
                .Include(x => x.Customer)
                .OrderBy(x => x.DueDate)
                .Take(5)
                .Select(x => new
                {
                    x.TaskId,
                    x.OrderNo,
                    Customer = x.Customer!.CustomerName,
                    Task = x.TaskName,
                    x.Status,
                    x.DueDate,
                    Amount = x.GrandTotal,
                    Priority =
                        x.DueDate == null ? "Low" :
                        x.DueDate < today ? "Critical" :
                        x.DueDate <= today.AddDays(1) ? "High" :
                        x.DueDate <= today.AddDays(3) ? "Medium" :
                        "Low"
                })
                .ToListAsync();

            var recentPayments = await _context.TaskPayments
                .AsNoTracking()
                .Include(x => x.Task)
                .ThenInclude(t => t.Customer)
                .OrderByDescending(x => x.PaymentDate)
                .Take(5)
                .Select(x => new
                {
                    x.PaymentId,
                    Customer = x.Task!.Customer!.CustomerName,
                    x.AmountPaid,
                    x.PaymentDate,
                    x.PaymentMode,
                    x.PaymentStatus
                })
                .ToListAsync();

            var revenueChart = await _context.TaskPayments
                .AsNoTracking()
                .Where(x => x.PaymentDate >= fromDate)
                .GroupBy(x => new { x.PaymentDate.Year, x.PaymentDate.Month })
                .OrderBy(x => x.Key.Year)
                .ThenBy(x => x.Key.Month)
                .Select(x => new
                {
                    Month = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(x.Key.Month),
                    Revenue = x.Sum(p => p.AmountPaid)
                })
                .ToListAsync();

            var taskChart = await _context.Tasks
                .AsNoTracking()
                .GroupBy(x => x.Status)
                .Select(x => new
                {
                    Status = x.Key,
                    Count = x.Count()
                })
                .OrderBy(x => x.Status)
                .ToListAsync();

            return Ok(new
            {
                summary = new
                {
                    totalCustomers,
                    totalTasks,
                    pendingTasks,
                    completedTasks,
                    overdueTasks,
                    totalRevenue = totalRevenue ?? 0,
                    pendingAmount = pendingAmount ?? 0
                },
                priorityTasks,
                recentPayments,
                revenueChart,
                taskChart
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Failed to load dashboard.",
                error = ex.Message
            });
        }
    }
}