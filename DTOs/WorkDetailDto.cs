namespace client.DTOs;
public class WorkDetailDto
{
    public int TaskDetailId { get; set; }
    public string? Description { get; set; }
    public int Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}