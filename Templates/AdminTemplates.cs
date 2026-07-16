using client.Helpers;
using client.Models;
namespace client.Templates
{
    public static class AdminTemplates
    {
        public static EmailMessage TaskCreated(TaskItem task)
        {
            return EmailLayout.Create(
                subject: $"New Task Created #{task.OrderNo}",
                title: "New Task Created",
                details: new Dictionary<string, string>
                {
                    { "Order No", task.OrderNo.ToString() },
                    { "Customer", task.Customer?.CustomerName ?? "" },
                    { "Phone", task.Customer?.PhoneNumber ?? "" },
                    { "Address", task.Customer?.Address ?? "" },
                    { "Task", task.TaskName },
                    { "Status", task.Status },
                    { "Amount", $"₹{task.GrandTotal:N0}" },
                    { "Created", DateTimeHelper.ToIndia(DateTimeHelper.UtcNow()).ToString("dd MMM yyyy hh:mm tt") }
                });
        }
        public static EmailMessage TaskUpdated(TaskItem task, string oldStatus)
        {
            return EmailLayout.Create(
                subject: $"Task Updated #{task.OrderNo}",
                title: "Task Status Changed",
                details: new Dictionary<string, string>
                {
                    { "Order No", task.OrderNo.ToString() },
                    { "Customer", task.Customer?.CustomerName ?? "" },
                    { "Old Status", oldStatus },
                    { "New Status", task.Status },
                    { "Amount", $"₹{task.GrandTotal:N0}" },
                    { "Updated", DateTimeHelper.ToIndia(DateTimeHelper.UtcNow()).ToString("dd MMM yyyy hh:mm tt") }
                });
        }
    }
}