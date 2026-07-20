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
                "year" => new DateTime(today.Year, 1, 1),
                _ => new DateTime(today.Year, today.Month, 1)
            };

            var totalCustomers = await _context.Customers
                .AsNoTracking()
                .CountAsync(x => x.CreatedDate >= fromDate);

            var totalTasks = await _context.Tasks
                .AsNoTracking()
                .CountAsync(x => x.CreatedDate >= fromDate);

            var pendingTasks = await _context.Tasks
                .AsNoTracking()
                .CountAsync(x =>
                    x.CreatedDate >= fromDate &&
                    x.Status == "Pending");

            var completedTasks = await _context.Tasks
                .AsNoTracking()
                .CountAsync(x =>
                    x.CreatedDate >= fromDate &&
                    x.Status == "Completed");

            var overdueTasks = await _context.Tasks
                .AsNoTracking()
                .CountAsync(x =>
                    x.Status != "Completed" &&
                    x.DueDate.HasValue &&
                    x.DueDate.Value < today);

            var totalRevenue = await _context.TaskPayments
                .AsNoTracking()
                .Where(x => x.PaymentDate >= fromDate)
                .SumAsync(x => (decimal?)x.AmountPaid) ?? 0;

            var pendingAmount = await _context.Tasks
                .AsNoTracking()
                .Where(x => x.Status != "Completed")
                .SumAsync(x =>
                    (decimal?)(x.GrandTotal -
                    (x.TaskPayments!.Sum(p => (decimal?)p.AmountPaid) ?? 0))) ?? 0;

            var priorityTasks = await _context.Tasks
                .AsNoTracking()
                .Include(x => x.Customer)
                .Where(x => x.Status != "Completed")
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

            var pendingPayments = await _context.Tasks
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.TaskPayments)
                .Where(x =>
                    x.GrandTotal >
                    (x.TaskPayments.Sum(p => (decimal?)p.AmountPaid) ?? 0))
                .OrderBy(x => x.DueDate)
                .Take(5)
                .Select(x => new
                {
                    x.TaskId,
                    x.OrderNo,
                    Customer = x.Customer!.CustomerName,
                    DueDate = x.DueDate,
                    Balance = x.GrandTotal -
                        (x.TaskPayments.Sum(p => (decimal?)p.AmountPaid) ?? 0)
                })
                .ToListAsync();
            List<object> revenueChart;

            if (period.Equals("Year", StringComparison.OrdinalIgnoreCase))
            {
                revenueChart = await _context.TaskPayments
                    .AsNoTracking()
                    .Where(x => x.PaymentDate >= fromDate)
                    .GroupBy(x => new
                    {
                        x.PaymentDate.Year,
                        x.PaymentDate.Month
                    })
                    .OrderBy(x => x.Key.Year)
                    .ThenBy(x => x.Key.Month)
                    .Select(x => (object)new
                    {
                        Label = CultureInfo.InvariantCulture.DateTimeFormat
                            .GetAbbreviatedMonthName(x.Key.Month),
                        Revenue = x.Sum(p => p.AmountPaid)
                    })
                    .ToListAsync();
            }
            else
            {
                revenueChart = await _context.TaskPayments
                    .AsNoTracking()
                    .Where(x => x.PaymentDate >= fromDate)
                    .GroupBy(x => x.PaymentDate.Date)
                    .OrderBy(x => x.Key)
                    .Select(x => (object)new
                    {
                        Label = x.Key.ToString("dd MMM"),
                        Revenue = x.Sum(p => p.AmountPaid)
                    })
                    .ToListAsync();
            }

            var taskChart = await _context.Tasks
                .AsNoTracking()
                .Where(x => x.CreatedDate >= fromDate)
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
                    totalRevenue,
                    pendingAmount
                },

                priorityTasks,

                pendingPayments,

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