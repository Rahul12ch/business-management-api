namespace client.DTOs;
public class UpdateTaskDto
{
    public int TaskId { get; set; }
    public int CustomerId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = "Pending";
    public bool IsGstApplied { get; set; }
    public decimal GstPercent { get; set; }
    public decimal GstAmount { get; set; }
    public decimal SubTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public List<WorkDetailDto> WorkDetails { get; set; } = new();
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public string PaymentMode { get; set; } = "Cash";
}