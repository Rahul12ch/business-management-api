using client.Helpers;
using client.Models;

namespace client.Templates
{
    public static class CustomerTemplates
    {
        public static EmailMessage TaskCreated(TaskItem task)
        {
            return EmailLayout.Create(
                $"RANA AIR CONDITIONING - Service Order #{task.OrderNo}",
                "Thank You for Choosing RANA AIR CONDITIONING",
                new Dictionary<string, string>
                {
                    { "Customer", task.Customer?.CustomerName ?? "" },
                    { "Order Number", task.OrderNo.ToString() },
                    { "Service", task.TaskName },
                    { "Status", task.Status },
                    { "Created", DateTimeHelper.ToIndia(task.CreatedDate).ToString("dd MMM yyyy") },
                    { "Due Date", task.DueDate?.ToString("dd MMM yyyy") ?? "N/A" },
                    { "Message", "Your service order has been created successfully. Please find your invoice attached as a PDF. Thank you for choosing RANA AIR CONDITIONING." }
                });
        }

        public static EmailMessage TaskUpdated(TaskItem task)
        {
            return EmailLayout.Create(
                $"RANA AIR CONDITIONING - Order #{task.OrderNo} Updated",
                "Your Service Order Has Been Updated",
                new Dictionary<string, string>
                {
                    { "Customer", task.Customer?.CustomerName ?? "" },
                    { "Order Number", task.OrderNo.ToString() },
                    { "Service", task.TaskName },
                    { "Status", task.Status },
                    { "Message", "Your service order has been updated. Please find the latest invoice attached as a PDF." }
                });
        }
    }
}