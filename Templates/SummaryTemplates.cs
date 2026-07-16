using client.Models;
using client.Helpers;
namespace client.Templates
{
    public static class SummaryTemplates
    {
        public static EmailMessage Daily( int customers, int tasksCreated, int tasksCompleted, decimal payments, decimal pending)
        {
            return EmailLayout.Create( "Daily Business Summary", "Daily Summary",
                new Dictionary<string, string>
                {
                    { "Date", DateTimeHelper.ToIndia(DateTimeHelper.UtcNow()).ToString("dd MMM yyyy") },
                    { "Customers", customers.ToString() },
                    { "Tasks Created", tasksCreated.ToString() },
                    { "Tasks Completed", tasksCompleted.ToString() },
                    { "Payments Received", $"₹{payments:N2}" },
                    { "Pending Amount", $"₹{pending:N2}" }
                });
        }

        public static EmailMessage Weekly( int customers, int tasks, int completed, decimal revenue, decimal pending)
        {
            return EmailLayout.Create( "Weekly Business Summary", "Weekly Summary",
                new Dictionary<string, string>
                {
                    { "Week Ending", DateTimeHelper.ToIndia(DateTimeHelper.UtcNow()).ToString("dd MMM yyyy") },
                    { "Customers", customers.ToString() },
                    { "Tasks", tasks.ToString() },
                    { "Completed Tasks", completed.ToString() },
                    { "Revenue", $"₹{revenue:N2}" },
                    { "Pending Amount", $"₹{pending:N2}" }
                });
        }

        public static EmailMessage Monthly( int customers, int tasks, int completed, decimal revenue, decimal pending)
        {
            return EmailLayout.Create( "Monthly Business Summary", "Monthly Summary",
                new Dictionary<string, string>
                {
                    { "Month", DateTimeHelper.ToIndia(DateTimeHelper.UtcNow()).ToString("MMMM yyyy") },
                    { "Customers", customers.ToString() },
                    { "Tasks", tasks.ToString() },
                    { "Completed Tasks", completed.ToString() },
                    { "Revenue", $"₹{revenue:N2}" },
                    { "Pending Amount", $"₹{pending:N2}" }
                });
        }
    }
}