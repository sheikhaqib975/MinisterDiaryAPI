using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinisterDiary.Api.Data;

namespace MinisterDiary.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SecretariatController : ControllerBase
{
    private readonly AppDbContext _context;

    public SecretariatController(AppDbContext context)
    {
        _context = context;
    }

    // E1: GET /api/secretariat/dashboard - Secretariat stats
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var total = await _context.Meetings.CountAsync();
        var reschedules = await _context.Meetings.Where(m => m.Status == "Rescheduled" || m.Status == "RescheduleRequested").CountAsync();
        var cancelled = await _context.Meetings.Where(m => m.Status == "Cancelled").CountAsync();
        var scheduled = await _context.Meetings.Where(m => m.Status == "Scheduled" || m.Status == "Accepted").CountAsync();

        return Ok(new
        {
            totalMeetings = total,
            reschedules,
            cancelled,
            scheduled
        });
    }

    // E2: GET /api/secretariat/schedule - Official meeting records (Read-Only with Filter)
    [HttpGet("schedule")]
    public async Task<IActionResult> GetSchedule([FromQuery] string? filter)
    {
        var query = _context.Meetings.AsQueryable();

        if (!string.IsNullOrEmpty(filter) && filter != "All")
        {
            if (filter == "Scheduled")
                query = query.Where(m => m.Status == "Scheduled" || m.Status == "Accepted");
            else
                query = query.Where(m => m.Status == filter);
        }

        var meetings = await query
            .OrderBy(m => m.Date).ThenBy(m => m.StartTime)
            .Select(m => new
            {
                id = m.Id,
                day = m.Date.ToString("dd"),
                month = m.Date.ToString("MMM").ToUpper(),
                date = m.Date.ToString("yyyy-MM-dd"),
                status = m.Status == "Scheduled" ? "Accepted" : m.Status,
                title = m.Title,
                subtitle = $"{m.StartTime}–{m.EndTime} · {m.Venue}",
                attendee = m.AttendeeName
            })
            .ToListAsync();

        return Ok(meetings);
    }

    // E3: GET /api/secretariat/meetings/{id} - Read-only meeting details
    [HttpGet("meetings/{id}")]
    public async Task<IActionResult> GetMeetingDetails(int id)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Diary).ThenInclude(d => d!.Minister)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (meeting == null)
            return NotFound(new { message = "Meeting record not found." });

        return Ok(new
        {
            id = meeting.Id,
            title = meeting.Title,
            date = meeting.Date.ToString("dddd, d MMMM yyyy"),
            time = $"{meeting.StartTime}–{meeting.EndTime} · 1 hour",
            venue = meeting.Venue,
            with = meeting.AttendeeName,
            attendeeEmail = meeting.AttendeeEmail,
            scheduledBy = $"{meeting.ScheduledByName ?? "Personal Assistant"} · Office of the Minister",
            notes = meeting.Notes,
            status = meeting.Status == "Scheduled" ? "Accepted" : meeting.Status
        });
    }
}
