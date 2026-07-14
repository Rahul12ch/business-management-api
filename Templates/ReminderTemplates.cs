using client.Models;

namespace client.Templates
{
    public static class ReminderTemplates
    {
        public static EmailMessage TaskDueToday( string orderNo, string customer, string task, DateTime dueDate)
        {
            return EmailLayout.Create( subject: "Task Due Today", title: "Task Due Reminder", details: new Dictionary<string, string>
                {
                    { "Order No.", orderNo },
                    { "Customer", customer },
                    { "Task", task },
                    { "Due Date", dueDate.ToString("dd MMM yyyy") },
                    { "Reminder", "This task is due today." }
                });
        }
        public static EmailMessage TaskOverdue( string orderNo, string customer, string task, DateTime dueDate)
        {
            return EmailLayout.Create( subject: "Task Overdue", title: "Task Overdue", details: new Dictionary<string, string>
                {
                    { "Order No.", orderNo },
                    { "Customer", customer },
                    { "Task", task },
                    { "Due Date", dueDate.ToString("dd MMM yyyy") },
                    { "Status", "Overdue" }
                });
        }
        public static EmailMessage PendingPayment( string orderNo, string customer, decimal balance, DateTime dueDate)
        {
            return EmailLayout.Create( subject: "Pending Payment", title: "Payment Reminder", details: new Dictionary<string, string>
                {
                    { "Order No.", orderNo },
                    { "Customer", customer },
                    { "Pending Amount", $"₹{balance:N2}" },
                    { "Due Date", dueDate.ToString("dd MMM yyyy") },
                    { "Status", "Pending" }
                });
        }
        public static EmailMessage PaymentOverdue(string orderNo, string customer, decimal balance, DateTime dueDate)
        {
            return EmailLayout.Create( subject: "Payment Overdue", title: "Payment Overdue", details: new Dictionary<string, string>
                {
                    { "Order No.", orderNo },
                    { "Customer", customer },
                    { "Outstanding Amount", $"₹{balance:N2}" },
                    { "Due Date", dueDate.ToString("dd MMM yyyy") },
                    { "Status", "Overdue" }
                });
        }
        public static EmailMessage InvoiceDueToday( string orderNo, string customer, decimal amount, DateTime dueDate)
        {
            return EmailLayout.Create( subject: "Invoice Due Today",  title: "Invoice Due Reminder",  details: new Dictionary<string, string>
                {
                    { "Order No.", orderNo },
                    { "Customer", customer },
                    { "Invoice Amount", $"₹{amount:N2}" },
                    { "Due Date", dueDate.ToString("dd MMM yyyy") }
                });
        }
    }
}