using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinisterDiary.Api.Data;
using MinisterDiary.Api.DTOs;
using MinisterDiary.Api.Models;

namespace MinisterDiary.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaController : ControllerBase
{
    private readonly AppDbContext _context;

    public PaController(AppDbContext context)
    {
        _context = context;
    }

    // C1: GET /api/pa/dashboard-stats - Stat cards for PA Dashboard
    [HttpGet("dashboard-stats")]
    public async Task<IActionResult> GetDashboardStats([FromQuery] int? diaryId)
    {
        var newSlots = await _context.FreeSlots.Where(s => !s.IsBooked).CountAsync();
        var reschedules = await _context.Meetings.Where(m => m.Status == "RescheduleRequested").CountAsync();
        var cancelled = await _context.Meetings.Where(m => m.Status == "Cancelled").CountAsync();
        var scheduled = await _context.Meetings.Where(m => m.Status == "Scheduled").CountAsync();

        return Ok(new
        {
            newSlots,
            reschedule = reschedules,
            cancel = cancelled,
            schedule = scheduled
        });
    }

    // C2: GET /api/pa/free-slots - Available free slots
    [HttpGet("free-slots")]
    public async Task<IActionResult> GetFreeSlots([FromQuery] int? diaryId, [FromQuery] DateTime? date)
    {
        var query = _context.FreeSlots.Where(s => !s.IsBooked);

        if (diaryId.HasValue)
            query = query.Where(s => s.DiaryId == diaryId.Value);

        if (date.HasValue)
            query = query.Where(s => s.Date.Date == date.Value.Date);

        var slots = await query
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .Select(s => new
            {
                id = s.Id,
                diaryId = s.DiaryId,
                date = s.Date.ToString("yyyy-MM-dd"),
                startTime = s.StartTime,
                endTime = s.EndTime,
                isBooked = s.IsBooked
            })
            .ToListAsync();

        return Ok(slots);
    }

    // C3: POST /api/pa/free-slots - Publish free slots (supports repeat options)
    [HttpPost("free-slots")]
    public async Task<IActionResult> AddFreeSlot([FromBody] AddFreeSlotDto dto)
    {
        if (dto.DiaryId <= 0)
        {
            var defaultDiary = await _context.Diaries.FirstOrDefaultAsync(d => d.Status == "Active");
            if (defaultDiary != null) dto.DiaryId = defaultDiary.Id;
        }

        var slotsToCreate = new List<FreeSlot>();

        if (dto.Repeat == "Daily")
        {
            // Add for 5 consecutive days
            for (int i = 0; i < 5; i++)
            {
                slotsToCreate.Add(new FreeSlot
                {
                    DiaryId = dto.DiaryId,
                    Date = dto.Date.AddDays(i),
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                    IsBooked = false
                });
            }
        }
        else if (dto.Repeat == "Weekdays")
        {
            // Add for next 5 weekdays
            var curr = dto.Date;
            int added = 0;
            while (added < 5)
            {
                if (curr.DayOfWeek != DayOfWeek.Saturday && curr.DayOfWeek != DayOfWeek.Sunday)
                {
                    slotsToCreate.Add(new FreeSlot
                    {
                        DiaryId = dto.DiaryId,
                        Date = curr,
                        StartTime = dto.StartTime,
                        EndTime = dto.EndTime,
                        IsBooked = false
                    });
                    added++;
                }
                curr = curr.AddDays(1);
            }
        }
        else
        {
            // Once
            slotsToCreate.Add(new FreeSlot
            {
                DiaryId = dto.DiaryId,
                Date = dto.Date,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                IsBooked = false
            });
        }

        _context.FreeSlots.AddRange(slotsToCreate);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"{slotsToCreate.Count} slot(s) published successfully." });
    }

    // C4: DELETE /api/pa/free-slots/{id} - Delete unbooked free slot
    [HttpDelete("free-slots/{id}")]
    public async Task<IActionResult> DeleteFreeSlot(int id)
    {
        var slot = await _context.FreeSlots.FindAsync(id);
        if (slot == null)
            return NotFound(new { message = "Free slot not found." });

        if (slot.IsBooked)
            return BadRequest(new { message = "Cannot delete a slot that already has a booked meeting." });

        _context.FreeSlots.Remove(slot);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Free slot deleted." });
    }

    // C5: POST /api/pa/schedule-meeting - Book a meeting inside a free slot
    [HttpPost("schedule-meeting")]
    public async Task<IActionResult> ScheduleMeeting([FromBody] ScheduleMeetingDto dto)
    {
        if (dto.DiaryId <= 0)
        {
            var defaultDiary = await _context.Diaries.FirstOrDefaultAsync(d => d.Status == "Active");
            if (defaultDiary != null) dto.DiaryId = defaultDiary.Id;
        }

        var meeting = new Meeting
        {
            DiaryId = dto.DiaryId,
            FreeSlotId = dto.FreeSlotId,
            Title = dto.Title,
            Date = dto.Date,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            DurationMinutes = 60,
            Venue = dto.Venue,
            AttendeeName = dto.AttendeeName,
            AttendeeOrganisation = dto.AttendeeOrganisation,
            AttendeeEmail = dto.AttendeeEmail,
            Notes = dto.Notes,
            Status = "Scheduled",
            ScheduledByName = "Personal Assistant",
            ScheduledByRole = "PA"
        };

        // Mark slot as booked
        if (dto.FreeSlotId.HasValue)
        {
            var slot = await _context.FreeSlots.FindAsync(dto.FreeSlotId.Value);
            if (slot != null) slot.IsBooked = true;
        }

        _context.Meetings.Add(meeting);

        // Notify Minister
        var diary = await _context.Diaries.FindAsync(dto.DiaryId);
        if (diary != null)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = diary.MinisterId,
                Title = "Meeting added by PA",
                Message = $"{dto.Title} · {dto.Date:d MMM}, {dto.StartTime}. Please confirm or change it.",
                Category = "Meeting",
                RelatedId = meeting.Id
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Meeting scheduled successfully.", meetingId = meeting.Id });
    }

    // C6: PUT /api/pa/reschedule-meeting/{id} - Re-book meeting into new free slot
    [HttpPut("reschedule-meeting/{id}")]
    public async Task<IActionResult> RescheduleMeeting(int id, [FromBody] RescheduleMeetingDto dto)
    {
        var meeting = await _context.Meetings.FindAsync(id);
        if (meeting == null)
            return NotFound(new { message = "Meeting not found." });

        // Release old slot
        if (meeting.FreeSlotId.HasValue)
        {
            var oldSlot = await _context.FreeSlots.FindAsync(meeting.FreeSlotId.Value);
            if (oldSlot != null) oldSlot.IsBooked = false;
        }

        // Book new slot
        var newSlot = await _context.FreeSlots.FindAsync(dto.NewFreeSlotId);
        if (newSlot != null)
        {
            newSlot.IsBooked = true;
        }

        meeting.PreviousDate = meeting.Date;
        meeting.PreviousStartTime = meeting.StartTime;
        meeting.PreviousEndTime = meeting.EndTime;

        meeting.Date = dto.NewDate;
        meeting.StartTime = dto.NewStartTime;
        meeting.EndTime = dto.NewEndTime;
        meeting.FreeSlotId = dto.NewFreeSlotId;
        meeting.Status = "Rescheduled";

        await _context.SaveChangesAsync();
        return Ok(new { message = "Meeting rescheduled successfully.", meetingId = meeting.Id });
    }

    // C7: POST /api/pa/inform-attendee - Mark attendee as informed
    [HttpPost("inform-attendee")]
    public async Task<IActionResult> InformAttendee([FromBody] InformAttendeeDto dto)
    {
        var meeting = await _context.Meetings.FindAsync(dto.MeetingId);
        if (meeting == null)
            return NotFound(new { message = "Meeting not found." });

        meeting.AttendeeInformed = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Attendee {meeting.AttendeeEmail} has been marked as informed." });
    }
}
