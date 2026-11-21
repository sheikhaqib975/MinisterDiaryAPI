using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinisterDiary.Api.Data;
using MinisterDiary.Api.DTOs;
using MinisterDiary.Api.Models;

namespace MinisterDiary.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeetingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public MeetingsController(AppDbContext context)
    {
        _context = context;
    }

    // B3: GET /api/meetings/{id} - Full meeting details for popup and screen
    [HttpGet("{id}")]
    public async Task<IActionResult> GetMeetingDetails(int id)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Diary)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (meeting == null)
            return NotFound(new { message = "Meeting not found." });

        return Ok(new
        {
            id = meeting.Id,
            title = meeting.Title,
            date = meeting.Date.ToString("yyyy-MM-dd"),
            startTime = meeting.StartTime,
            endTime = meeting.EndTime,
            durationMinutes = meeting.DurationMinutes,
            venue = meeting.Venue,
            attendeeName = meeting.AttendeeName,
            attendeeOrganisation = meeting.AttendeeOrganisation,
            attendeeEmail = meeting.AttendeeEmail,
            notes = meeting.Notes,
            status = meeting.Status,
            scheduledByName = meeting.ScheduledByName,
            scheduledByRole = meeting.ScheduledByRole
        });
    }

    // B4: PUT /api/meetings/{id}/reschedule-request - Minister requests PA to reschedule
    [HttpPut("{id}/reschedule-request")]
    public async Task<IActionResult> RequestReschedule(int id, [FromBody] RescheduleRequestDto dto)
    {
        var meeting = await _context.Meetings.FindAsync(id);
        if (meeting == null)
            return NotFound(new { message = "Meeting not found." });

        meeting.Status = "RescheduleRequested";
        meeting.RescheduleReason = dto.Reason;

        // Notify PA
        var diary = await _context.Diaries.FindAsync(meeting.DiaryId);
        if (diary?.PaId != null)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = diary.PaId.Value,
                Title = "Minister requested reschedule",
                Message = $"Minister asked to reschedule '{meeting.Title}'. Reason: {dto.Reason}",
                Category = "Meeting",
                RelatedId = meeting.Id
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Reschedule request recorded. PA notified." });
    }

    // B5: PUT /api/meetings/{id}/cancel - Minister or PA cancels meeting
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelMeeting(int id, [FromBody] CancelMeetingDto dto)
    {
        var meeting = await _context.Meetings.FindAsync(id);
        if (meeting == null)
            return NotFound(new { message = "Meeting not found." });

        meeting.Status = "Cancelled";
        meeting.CancelReason = dto.Reason;

        // If it was linked to a free slot, open that slot again
        if (meeting.FreeSlotId != null)
        {
            var slot = await _context.FreeSlots.FindAsync(meeting.FreeSlotId);
            if (slot != null)
            {
                slot.IsBooked = false;
            }
        }

        // Notify PA
        var diary = await _context.Diaries.FindAsync(meeting.DiaryId);
        if (diary?.PaId != null)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = diary.PaId.Value,
                Title = "Meeting cancelled",
                Message = $"'{meeting.Title}' was cancelled. The slot is free again.",
                Category = "Meeting",
                RelatedId = meeting.Id
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Meeting cancelled. Slot released.", meetingId = meeting.Id });
    }
}
