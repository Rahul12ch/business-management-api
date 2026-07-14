namespace client.Models;
public class TaskDetail
{
    public int TaskDetailId { get; set; }
    public int TaskId { get; set; }
    public string? Description { get; set; }
    public int Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public TaskItem? Task { get; set; }
}