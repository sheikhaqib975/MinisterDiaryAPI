using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinisterDiary.Api.Data;

namespace MinisterDiary.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotificationsController(AppDbContext context)
    {
        _context = context;
    }

    // H1: GET /api/notifications - List user alerts/notifications with filter (All, Unread, Requests)
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int? userId, [FromQuery] string filter = "All")
    {
        var query = _context.Notifications.AsQueryable();

        if (userId.HasValue)
            query = query.Where(n => n.UserId == userId.Value);

        if (filter == "Unread")
            query = query.Where(n => !n.IsRead);
        else if (filter == "Requests")
            query = query.Where(n => n.Category == "Request");

        var list = await query
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new
            {
                id = n.Id,
                title = n.Title,
                body = n.Message,
                category = n.Category,
                unread = !n.IsRead,
                time = n.CreatedAt.ToString("MMM d, HH:mm"),
                relatedId = n.RelatedId
            })
            .ToListAsync();

        return Ok(list);
    }

    // H2: GET /api/notifications/unread-count - Badge count for top bar bell icon
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount([FromQuery] int? userId)
    {
        var query = _context.Notifications.Where(n => !n.IsRead);
        if (userId.HasValue)
            query = query.Where(n => n.UserId == userId.Value);

        var count = await query.CountAsync();
        return Ok(new { unreadCount = count });
    }

    // H3: PUT /api/notifications/{id}/read - Mark single notification as read
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var notif = await _context.Notifications.FindAsync(id);
        if (notif == null)
            return NotFound(new { message = "Notification not found." });

        notif.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Notification marked as read." });
    }

    // H4: PUT /api/notifications/read-all - Mark all notifications as read
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead([FromQuery] int? userId)
    {
        var query = _context.Notifications.Where(n => !n.IsRead);
        if (userId.HasValue)
            query = query.Where(n => n.UserId == userId.Value);

        var unread = await query.ToListAsync();
        foreach (var item in unread)
        {
            item.IsRead = true;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Marked {unread.Count} notification(s) as read." });
    }
}
