namespace client.Models;
public class Customer
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}