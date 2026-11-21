using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinisterDiary.Api.Data;
using MinisterDiary.Api.DTOs;
using MinisterDiary.Api.Models;

namespace MinisterDiary.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiaryController : ControllerBase
{
    private readonly AppDbContext _context;

    public DiaryController(AppDbContext context)
    {
        _context = context;
    }

    // B1: GET /api/diary/minister - Get active diary for the minister
    [HttpGet("minister")]
    public async Task<IActionResult> GetMinisterDiary([FromQuery] int? ministerId)
    {
        // If ministerId is not provided, look for the logged-in user or first minister
        var query = _context.Diaries
            .Include(d => d.Minister)
            .Include(d => d.Pa)
            .Where(d => d.Status == "Active");

        if (ministerId.HasValue)
            query = query.Where(d => d.MinisterId == ministerId.Value);

        var diary = await query.FirstOrDefaultAsync();
        if (diary == null)
            return NotFound(new { message = "Active diary not found for minister." });

        return Ok(new
        {
            diaryId = diary.Id,
            ministerId = diary.MinisterId,
            ministerName = diary.Minister?.FullName,
            portfolio = diary.Minister?.Portfolio,
            paId = diary.PaId,
            paName = diary.Pa?.FullName,
            status = diary.Status,
            startDate = diary.StartDate
        });
    }

    // B2: GET /api/diary/{diaryId}/timeline - 5-day schedule / day meetings
    [HttpGet("{diaryId}/timeline")]
    public async Task<IActionResult> GetTimeline(int diaryId, [FromQuery] DateTime? date)
    {
        var targetDate = date ?? DateTime.Today;

        var meetings = await _context.Meetings
            .Where(m => m.DiaryId == diaryId && m.Date.Date == targetDate.Date)
            .OrderBy(m => m.StartTime)
            .Select(m => new
            {
                id = m.Id,
                title = m.Title,
                date = m.Date.ToString("yyyy-MM-dd"),
                startTime = m.StartTime,
                endTime = m.EndTime,
                venue = m.Venue,
                attendee = m.AttendeeName,
                status = m.Status, // Scheduled, Rescheduled, Cancelled, Overridden
                notes = m.Notes
            })
            .ToListAsync();

        return Ok(meetings);
    }

    // B5: GET /api/diary/inherited-meetings - Transferred meetings review
    [HttpGet("inherited-meetings")]
    public async Task<IActionResult> GetInheritedMeetings([FromQuery] int? diaryId)
    {
        var meetings = await _context.Meetings
            .Where(m => (diaryId == null || m.DiaryId == diaryId) &&
                        (m.Source == "Transferred" || m.InheritedStatus != null))
            .Select(m => new
            {
                id = m.Id,
                title = m.Title,
                date = m.Date.ToString("yyyy-MM-dd"),
                startTime = m.StartTime,
                endTime = m.EndTime,
                venue = m.Venue,
                attendee = m.AttendeeName,
                inheritedStatus = m.InheritedStatus ?? "Pending",
                status = m.Status
            })
            .ToListAsync();

        return Ok(meetings);
    }

    // B6: PUT /api/diary/inherited-meetings/{id}/action - Keep, Reschedule, or Cancel
    [HttpPut("inherited-meetings/{id}/action")]
    public async Task<IActionResult> HandleInheritedMeeting(int id, [FromBody] InheritedMeetingActionDto dto)
    {
        var meeting = await _context.Meetings.FindAsync(id);
        if (meeting == null)
            return NotFound(new { message = "Meeting not found." });

        if (dto.Action == "Keep")
        {
            meeting.InheritedStatus = "Kept";
            meeting.Status = "Scheduled";
        }
        else if (dto.Action == "Cancel")
        {
            meeting.InheritedStatus = "Cancelled";
            meeting.Status = "Cancelled";
            meeting.CancelReason = dto.Reason ?? "Cancelled by incoming minister.";
        }
        else if (dto.Action == "Reschedule")
        {
            meeting.InheritedStatus = "Rescheduled";
            meeting.Status = "RescheduleRequested";
            meeting.RescheduleReason = dto.Reason ?? "Reschedule requested by incoming minister.";
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Meeting marked as {dto.Action}.", meetingId = meeting.Id });
    }

    // B7: GET /api/diary/dashboard-stats - 2x2 stat cards for minister dashboard
    [HttpGet("dashboard-stats")]
    public async Task<IActionResult> GetDashboardStats([FromQuery] int? diaryId)
    {
        var today = DateTime.Today;
        var endOfWeek = today.AddDays(7);

        var query = _context.Meetings.AsQueryable();
        if (diaryId.HasValue)
            query = query.Where(m => m.DiaryId == diaryId.Value);

        var total = await query.CountAsync();
        var todayCount = await query.Where(m => m.Date.Date == today && m.Status != "Cancelled").CountAsync();
        var thisWeekCount = await query.Where(m => m.Date.Date >= today && m.Date.Date <= endOfWeek && m.Status != "Cancelled").CountAsync();
        var cancelledCount = await query.Where(m => m.Status == "Cancelled").CountAsync();

        return Ok(new
        {
            totalMeetings = total,
            today = todayCount,
            thisWeek = thisWeekCount,
            cancelled = cancelledCount
        });
    }
}
