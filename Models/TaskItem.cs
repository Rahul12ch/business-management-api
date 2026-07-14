namespace client.Models;
public class TaskItem
{
    public int TaskId { get; set; }
    public int OrderNo { get; set; }
    public int CustomerId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = "Pending";
    public bool IsGstApplied { get; set; }
    public decimal GstPercent { get; set; } = 0;
    public decimal GstAmount { get; set; } = 0;
    public decimal SubTotal { get; set; } = 0;
    public decimal GrandTotal { get; set; } = 0;
    public decimal TotalAmount { get; set; } = 0;
    public Customer? Customer { get; set; }
    public ICollection<TaskDetail>? TaskDetails { get; set; }
    public ICollection<TaskPayment>? TaskPayments { get; set; }
}