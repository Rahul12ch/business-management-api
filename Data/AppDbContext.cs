namespace client.Data;
using client.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TaskDetail> TaskDetails => Set<TaskDetail>();
    public DbSet<TaskPayment> TaskPayments => Set<TaskPayment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>().HasKey(x => x.Id);
        modelBuilder.Entity<Customer>().HasKey(x => x.CustomerId);
        modelBuilder.Entity<Customer>().HasIndex(x => x.CustomerName);
        modelBuilder.Entity<TaskItem>().HasKey(x => x.TaskId);
        modelBuilder.Entity<TaskDetail>().HasKey(x => x.TaskDetailId);
        modelBuilder.Entity<TaskPayment>().HasKey(x => x.PaymentId);
        modelBuilder.Entity<Notification>().HasKey(x => x.NotificationId);

        modelBuilder.Entity<User>().HasIndex(x => x.Username).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();

        modelBuilder.Entity<TaskItem>()
            .HasOne(x => x.Customer)
            .WithMany(x => x.Tasks)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskDetail>()
            .HasOne(x => x.Task)
            .WithMany(x => x.TaskDetails)
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskPayment>()
            .HasOne(x => x.Task)
            .WithMany(x => x.TaskPayments)
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Customer>().Property(x => x.CustomerName).HasMaxLength(150);
        modelBuilder.Entity<Customer>().Property(x => x.PhoneNumber).HasMaxLength(20);
        modelBuilder.Entity<Customer>().Property(x => x.Email).HasMaxLength(150);
        modelBuilder.Entity<Customer>().Property(x => x.Address).HasMaxLength(500);

        modelBuilder.Entity<TaskItem>().HasIndex(x => x.OrderNo).IsUnique();
        modelBuilder.Entity<TaskItem>().HasIndex(x => x.Status);
        modelBuilder.Entity<TaskItem>().HasIndex(x => x.DueDate);
        modelBuilder.Entity<TaskItem>().HasIndex(x => x.CreatedDate);
        modelBuilder.Entity<TaskItem>().HasIndex(x => x.CustomerId);
        modelBuilder.Entity<TaskItem>().Property(x => x.TaskName).HasMaxLength(200);
        modelBuilder.Entity<TaskItem>().Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Pending");
        modelBuilder.Entity<TaskItem>().Property(x => x.TotalAmount).HasColumnType("numeric(18,2)");
        modelBuilder.Entity<TaskItem>().Property(x => x.SubTotal).HasColumnType("numeric(18,2)");
        modelBuilder.Entity<TaskItem>().Property(x => x.GstPercent).HasColumnType("numeric(18,2)");
        modelBuilder.Entity<TaskItem>().Property(x => x.GstAmount).HasColumnType("numeric(18,2)");
        modelBuilder.Entity<TaskItem>().Property(x => x.GrandTotal).HasColumnType("numeric(18,2)");

        modelBuilder.Entity<TaskDetail>().Property(x => x.Description).HasMaxLength(500);
        modelBuilder.Entity<TaskDetail>().Property(x => x.Rate).HasColumnType("numeric(18,2)");
        modelBuilder.Entity<TaskDetail>().Property(x => x.Amount).HasColumnType("numeric(18,2)");

        modelBuilder.Entity<TaskPayment>().Property(x => x.AmountPaid).HasColumnType("numeric(18,2)");
        modelBuilder.Entity<TaskPayment>().Property(x => x.PaymentMode).HasMaxLength(50);
        modelBuilder.Entity<TaskPayment>().Property(x => x.PaymentStatus).HasMaxLength(20).HasDefaultValue("Pending");
        modelBuilder.Entity<TaskPayment>().HasIndex(x => x.PaymentDate);
        modelBuilder.Entity<TaskPayment>().HasIndex(x => x.TaskId);
        modelBuilder.Entity<TaskPayment>().HasIndex(x => x.PaymentStatus);

        modelBuilder.Entity<Notification>().Property(x => x.Title).HasMaxLength(150);
        modelBuilder.Entity<Notification>().Property(x => x.Message).HasMaxLength(500);
        modelBuilder.Entity<Notification>().Property(x => x.Type).HasMaxLength(50);
        modelBuilder.Entity<Notification>().Property(x => x.IsRead).HasDefaultValue(false);
        modelBuilder.Entity<Notification>().Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<Notification>().HasIndex(x => x.CreatedAt);
        modelBuilder.Entity<Notification>().HasIndex(x => x.IsRead);
    }
}
