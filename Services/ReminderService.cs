using client.Data;
using client.Models;
using client.Templates;
using Microsoft.EntityFrameworkCore;

namespace client.Services;

public class ReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly TimeZoneInfo IndiaTimeZone =
    OperatingSystem.IsWindows()
        ? TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")
        : TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
    public ReminderService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var nowUtc = DateTime.UtcNow;
            var nowIndia = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, IndiaTimeZone);
            var nextRunIndia = nowIndia.Date.AddHours(8);
            if (nowIndia >= nextRunIndia) nextRunIndia = nextRunIndia.AddDays(1);
            var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRunIndia, IndiaTimeZone);
            var delay = nextRunUtc - DateTime.UtcNow;
            if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;
            await Task.Delay(delay, stoppingToken);
            try
            { await RunReminderCheck(); }
            catch (Exception ex)
            { Console.WriteLine($"ReminderService Error: {ex}"); }
        }
    }
    private async Task RunReminderCheck()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<EmailSender>();
        var today = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow, IndiaTimeZone).Date;
        var tomorrow = today.AddDays(1);
        var dueTasks = await context.Tasks
            .Include(x => x.Customer) .Where(x => x.DueDate == today && x.Status != "Completed") .ToListAsync();
        foreach (var task in dueTasks)
        {
            if (await AlreadySent(context, "TaskDue", task.TaskId)) continue;
            await emailSender.SendAsync(
                ReminderTemplates.TaskDueToday(
                    task.OrderNo.ToString(),  task.Customer?.CustomerName ?? "",  task.TaskName,  task.DueDate ?? today));
            await SaveNotification(
                context, "Task Due Today", $"Order #{task.OrderNo} due today", "TaskDue", task.TaskId);
        }
        var overdueTasks = await context.Tasks
            .Include(x => x.Customer) .Where(x => x.DueDate < today && x.Status != "Completed") .ToListAsync();
        foreach (var task in overdueTasks)
        {
            if (await AlreadySent(context, "TaskOverdue", task.TaskId)) continue;
            await emailSender.SendAsync(
                ReminderTemplates.TaskOverdue(
                    task.OrderNo.ToString(), task.Customer?.CustomerName ?? "", task.TaskName, task.DueDate ?? today));
            await SaveNotification(
                context, "Task Overdue",$"Order #{task.OrderNo} overdue", "TaskOverdue", task.TaskId);
        }
        var paymentTasks = await context.Tasks
            .Include(x => x.Customer) .Include(x => x.TaskPayments) .ToListAsync();
        foreach (var task in paymentTasks)
        {
            var paid = task.TaskPayments?.Sum(x => x.AmountPaid) ?? 0;
            var balance = task.GrandTotal - paid;
            if (balance <= 0) continue;
            var type = task.DueDate < today ? "PaymentOverdue" : "PaymentPending";
            if (await AlreadySent(context, type, task.TaskId)) continue;
            if (type == "PaymentOverdue")
            {
                await emailSender.SendAsync(
                    ReminderTemplates.PaymentOverdue(
                        task.OrderNo.ToString(), task.Customer?.CustomerName ?? "", balance, task.DueDate ?? today));
            }
            else
            {
                await emailSender.SendAsync(
                    ReminderTemplates.PendingPayment(
                        task.OrderNo.ToString(),  task.Customer?.CustomerName ?? "",  balance,  task.DueDate ?? today));
            }
            await SaveNotification(
                context,  type,  $"Order #{task.OrderNo} balance ₹{balance:N2}",  type,  task.TaskId);
        }
        var customers = await context.Customers.CountAsync();
        var tasksCreated = await context.Tasks.CountAsync(x => x.CreatedDate >= today && x.CreatedDate < tomorrow);
        var completed = await context.Tasks.CountAsync(x => x.Status == "Completed");
        var payments = await context.TaskPayments
            .Where(x =>  x.PaymentDate >= today && x.PaymentDate < tomorrow)
            .SumAsync(x => x.AmountPaid);
        var pending = paymentTasks.Sum(t =>
        {
            var paid = t.TaskPayments?.Sum(x => x.AmountPaid) ?? 0; return Math.Max(0, t.GrandTotal - paid);
        });
        await emailSender.SendAsync(
            SummaryTemplates.Daily(
                customers,  tasksCreated,  completed,  payments,  pending));
    }
    private async Task<bool> AlreadySent( AppDbContext context, string type, int id)
    {
        var today = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            IndiaTimeZone).Date;
        return await context.Notifications.AnyAsync(x =>  x.Type == type &&  x.ReferenceId == id &&  x.CreatedAt.Date == today);
    }
    private async Task SaveNotification( AppDbContext context, string title, string message, string type, int id)
    {
        context.Notifications.Add(new Notification
        {
            Title = title, Message = message, Type = type, ReferenceId = id, IsRead = false,
            CreatedAt = TimeZoneInfo.ConvertTimeFromUtc( DateTime.UtcNow, IndiaTimeZone)
        });
        await context.SaveChangesAsync();
    }
}