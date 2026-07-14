using client.Models;

namespace client.Templates
{
    public static class CustomerTemplates
    {
        public static EmailMessage TaskCreated(TaskItem task)
        {
            var details = new Dictionary<string, string>
            {
                { "Customer Name", task.Customer?.CustomerName ?? "" },
                { "Phone Number", task.Customer?.PhoneNumber ?? "" },
                { "Email", task.Customer?.Email ?? "" },
                { "Address", task.Customer?.Address ?? "" },
                { "Order No", task.OrderNo.ToString() },
                { "Service", task.TaskName },
                { "Created Date", task.CreatedDate.ToString("dd MMM yyyy") },
                { "Due Date", task.DueDate?.ToString("dd MMM yyyy") ?? "" },
                { "Status", task.Status },
                { "Subtotal", $"₹{task.SubTotal:N2}" },
                { "GST", task.IsGstApplied ? $"₹{task.GstAmount:N2}" : "No GST" },
                { "Grand Total", $"₹{task.GrandTotal:N2}" },
                { "Message", "Your invoice bill is attached with this email." }
            };
            return EmailLayout.Create( $"Your Service Order #{task.OrderNo}", "Order Details", details);
        }
        public static EmailMessage TaskUpdated(TaskItem task)
        {
            return EmailLayout.Create( $"Order #{task.OrderNo} Updated", "Service Order Updated",
                new Dictionary<string, string>
                {
                    { "Customer", task.Customer?.CustomerName ?? "" },
                    { "Order No", task.OrderNo.ToString() },
                    { "Service", task.TaskName },
                    { "Current Status", task.Status },
                    { "Grand Total", $"₹{task.GrandTotal:N2}" },
                    { "Message", "Your updated invoice bill is attached with this email." }
                });
        }
    }
}