using client.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace client.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var notifications = await _context.Notifications .OrderByDescending(x => x.CreatedAt) .ToListAsync();
            return Ok(notifications);
        }
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _context.Notifications.CountAsync(x => !x.IsRead);
            return Ok(count);
        }
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound();
            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            await _context.Notifications .Where(x => !x.IsRead)
                .ExecuteUpdateAsync(x => x.SetProperty( n => n.IsRead, true));
            return Ok();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var notification = await _context.Notifications .FirstOrDefaultAsync(x => x.NotificationId == id);
            if (notification == null) return NotFound(new
                {
                    message = "Notification not found", id
                });
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = "Deleted"
            });
        }
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearNotifications()
        {
            await _context.Notifications .ExecuteDeleteAsync();
            return Ok();
        }
    }
}