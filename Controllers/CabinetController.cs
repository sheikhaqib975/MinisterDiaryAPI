using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinisterDiary.Api.Data;
using MinisterDiary.Api.DTOs;
using MinisterDiary.Api.Models;

namespace MinisterDiary.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CabinetController : ControllerBase
{
    private readonly AppDbContext _context;

    public CabinetController(AppDbContext context)
    {
        _context = context;
    }

    // F1: POST /api/cabinet/schedule - PM office schedules Cabinet meeting, overrides minister slots
    [HttpPost("schedule")]
    public async Task<IActionResult> ScheduleCabinetMeeting([FromBody] ScheduleCabinetMeetingDto dto)
    {
        var cabinetMeeting = new CabinetMeeting
        {
            Title = dto.Title,
            Date = dto.Date,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Venue = dto.Venue,
            Agenda = dto.Agenda,
            CreatedByUserId = 1, // PM User
            Status = "Active"
        };

        _context.CabinetMeetings.Add(cabinetMeeting);
        await _context.SaveChangesAsync();

        // Get ministers to invite
        var ministers = await _context.Users
            .Where(u => u.Role == "Minister" && (dto.MinisterIds.Count == 0 || dto.MinisterIds.Contains(u.Id)))
            .ToListAsync();

        foreach (var minister in ministers)
        {
            var diary = await _context.Diaries.FirstOrDefaultAsync(d => d.MinisterId == minister.Id && d.Status == "Active");
            if (diary != null)
            {
                // Add Attendee record
                _context.CabinetMeetingAttendees.Add(new CabinetMeetingAttendee
                {
                    CabinetMeetingId = cabinetMeeting.Id,
                    MinisterId = minister.Id,
                    DiaryId = diary.Id,
                    Status = "Pending"
                });

                // Find any conflicting routine meetings at that date & time, override them!
                var conflictingMeetings = await _context.Meetings
                    .Where(m => m.DiaryId == diary.Id && m.Date.Date == dto.Date.Date && m.Status != "Cancelled")
                    .ToListAsync();

                foreach (var conf in conflictingMeetings)
                {
                    conf.Status = "Overridden";
                    conf.Notes += $" [Overridden by Cabinet meeting: {dto.Title}]";
                }

                // Send High-Priority Notification to Minister
                _context.Notifications.Add(new Notification
                {
                    UserId = minister.Id,
                    Title = "Cabinet Override",
                    Message = $"{dto.Title} on {dto.Date:d MMM}, {dto.StartTime} replaced conflicting meetings.",
                    Category = "Cabinet",
                    RelatedId = cabinetMeeting.Id
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Federal Cabinet meeting scheduled and ministers notified.", cabinetMeetingId = cabinetMeeting.Id });
    }

    // F2: GET /api/cabinet/active-request - Minister reviews cabinet override
    [HttpGet("active-request")]
    public async Task<IActionResult> GetActiveCabinetRequest([FromQuery] int? ministerId)
    {
        var attendee = await _context.CabinetMeetingAttendees
            .Include(a => a.CabinetMeeting)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        if (attendee == null || attendee.CabinetMeeting == null)
            return Ok(new { hasActiveRequest = false });

        var overriddenMeeting = await _context.Meetings
            .Where(m => m.DiaryId == attendee.DiaryId && m.Status == "Overridden")
            .FirstOrDefaultAsync();

        return Ok(new
        {
            hasActiveRequest = true,
            cabinetMeeting = new
            {
                id = attendee.CabinetMeeting.Id,
                title = attendee.CabinetMeeting.Title,
                date = attendee.CabinetMeeting.Date.ToString("ddd, d MMM yyyy"),
                time = $"{attendee.CabinetMeeting.StartTime}–{attendee.CabinetMeeting.EndTime}",
                venue = attendee.CabinetMeeting.Venue,
                status = attendee.Status
            },
            overriddenMeeting = overriddenMeeting != null ? new
            {
                id = overriddenMeeting.Id,
                title = overriddenMeeting.Title,
                schedule = $"{overriddenMeeting.Date:d MMM} · {overriddenMeeting.StartTime}–{overriddenMeeting.EndTime} · {overriddenMeeting.Venue}"
            } : null
        });
    }

    // F3: POST /api/cabinet/acknowledge - Minister acknowledges attendance or provides reason for absence
    [HttpPost("acknowledge")]
    public async Task<IActionResult> AcknowledgeCabinet([FromBody] AcknowledgeCabinetDto dto)
    {
        var attendee = await _context.CabinetMeetingAttendees
            .FirstOrDefaultAsync(a => a.CabinetMeetingId == dto.CabinetMeetingId);

        if (attendee == null)
            return NotFound(new { message = "Cabinet invitation record not found." });

        if (dto.Action == "Acknowledge")
        {
            attendee.Status = "Acknowledged";
        }
        else
        {
            attendee.Status = "Rejected";
            attendee.RejectReason = dto.Reason;
        }

        attendee.RespondedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Cabinet request marked as {attendee.Status}." });
    }

    // F4: POST /api/cabinet/cancel/{id} - PM cancels cabinet meeting, restores overridden meetings
    [HttpPost("cancel/{id}")]
    public async Task<IActionResult> CancelCabinetMeeting(int id)
    {
        var cabinet = await _context.CabinetMeetings
            .Include(c => c.Attendees)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cabinet == null)
            return NotFound(new { message = "Cabinet meeting not found." });

        cabinet.Status = "Cancelled";

        // Restore overridden meetings
        var diaryIds = cabinet.Attendees.Select(a => a.DiaryId).ToList();
        var overriddenMeetings = await _context.Meetings
            .Where(m => diaryIds.Contains(m.DiaryId) && m.Status == "Overridden")
            .ToListAsync();

        foreach (var m in overriddenMeetings)
        {
            m.Status = "Scheduled"; // Re-scheduled automatically!
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Cabinet meeting cancelled. Previous meetings restored to Scheduled status." });
    }
}
