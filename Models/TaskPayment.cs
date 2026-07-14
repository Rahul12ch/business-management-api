namespace client.Models;
public class TaskPayment
{
    public int PaymentId { get; set; }
    public int TaskId { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Now;
    public string PaymentMode { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = "Pending";
    public TaskItem? Task { get; set; }
}