using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinisterDiary.Api.Data;
using MinisterDiary.Api.DTOs;
using MinisterDiary.Api.Models;

namespace MinisterDiary.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeetingRequestsController : ControllerBase
{
    private readonly AppDbContext _context;

    public MeetingRequestsController(AppDbContext context)
    {
        _context = context;
    }

    // D1: GET /api/meeting-requests/ministers - List active ministers for meeting request dropdown
    [HttpGet("ministers")]
    public async Task<IActionResult> GetMinisters()
    {
        var ministers = await _context.Users
            .Where(u => u.Role == "Minister" && u.IsActive)
            .Select(u => new
            {
                id = u.Id,
                fullName = u.FullName,
                portfolio = u.Portfolio ?? "Federal Ministry",
                designation = u.Designation
            })
            .ToListAsync();

        return Ok(ministers);
    }

    // D2: GET /api/meeting-requests/ministers/{id}/free-slots - Free slots of target minister
    [HttpGet("ministers/{id}/free-slots")]
    public async Task<IActionResult> GetMinisterFreeSlots(int id)
    {
        var diary = await _context.Diaries.FirstOrDefaultAsync(d => d.MinisterId == id && d.Status == "Active");
        if (diary == null)
            return NotFound(new { message = "Minister has no active diary." });

        var slots = await _context.FreeSlots
            .Where(s => s.DiaryId == diary.Id && !s.IsBooked)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .Select(s => new
            {
                id = s.Id,
                day = s.Date.ToString("dddd, d MMMM yyyy"),
                time = $"{s.StartTime}–{s.EndTime} · Free",
                date = s.Date.ToString("yyyy-MM-dd"),
                startTime = s.StartTime,
                endTime = s.EndTime
            })
            .ToListAsync();

        return Ok(slots);
    }

    // D3: POST /api/meeting-requests - Send request to other minister's PA
    [HttpPost]
    public async Task<IActionResult> CreateMeetingRequest([FromBody] CreateMeetingRequestDto dto)
    {
        var targetDiary = await _context.Diaries.FirstOrDefaultAsync(d => d.MinisterId == dto.TargetMinisterId && d.Status == "Active");
        if (targetDiary == null)
            return NotFound(new { message = "Target Minister diary not found." });

        var senderDiary = await _context.Diaries.FirstOrDefaultAsync(d => d.Status == "Active");
        if (senderDiary == null)
            return BadRequest(new { message = "Sender diary not found." });

        var request = new MeetingRequest
        {
            FromDiaryId = senderDiary.Id,
            ToDiaryId = targetDiary.Id,
            FromPaId = senderDiary.PaId ?? 1,
            ToPaId = targetDiary.PaId ?? 1,
            FreeSlotId = dto.FreeSlotId,
            Purpose = dto.Purpose,
            Status = "Pending"
        };

        _context.MeetingRequests.Add(request);

        // Notify Target PA
        if (targetDiary.PaId.HasValue)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = targetDiary.PaId.Value,
                Title = "Meeting request received",
                Message = $"Meeting request received: {dto.Purpose}",
                Category = "Request",
                RelatedId = request.Id
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Meeting request sent to PA.", requestId = request.Id });
    }

    // D4: GET /api/meeting-requests - List incoming and sent requests
    [HttpGet]
    public async Task<IActionResult> GetRequests([FromQuery] string type = "incoming")
    {
        var query = _context.MeetingRequests
            .Include(r => r.FromDiary).ThenInclude(d => d!.Minister)
            .Include(r => r.ToDiary).ThenInclude(d => d!.Minister)
            .Include(r => r.FreeSlot)
            .AsQueryable();

        var list = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                id = r.Id,
                status = r.Status, // Pending, Confirmed, Rejected
                title = type == "incoming" 
                    ? $"{r.FromDiary!.Minister!.FullName}'s PA wants a meeting"
                    : $"To {r.ToDiary!.Minister!.FullName}'s PA",
                schedule = r.FreeSlot != null ? $"{r.FreeSlot.Date:ddd, d MMM}, {r.FreeSlot.StartTime}–{r.FreeSlot.EndTime}" : "",
                purpose = $"Purpose: {r.Purpose}",
                createdAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(list);
    }

    // D5: PUT /api/meeting-requests/{id}/action - Confirm or Reject request
    [HttpPut("{id}/action")]
    public async Task<IActionResult> HandleRequest(int id, [FromBody] MeetingRequestActionDto dto)
    {
        var request = await _context.MeetingRequests
            .Include(r => r.FreeSlot)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
            return NotFound(new { message = "Request not found." });

        if (dto.Action == "Confirm")
        {
            request.Status = "Confirmed";
            request.RespondedAt = DateTime.UtcNow;

            // Create actual meeting
            if (request.FreeSlot != null)
            {
                request.FreeSlot.IsBooked = true;

                _context.Meetings.Add(new Meeting
                {
                    DiaryId = request.ToDiaryId,
                    FreeSlotId = request.FreeSlotId,
                    Title = request.Purpose,
                    Date = request.FreeSlot.Date,
                    StartTime = request.FreeSlot.StartTime,
                    EndTime = request.FreeSlot.EndTime,
                    DurationMinutes = 60,
                    Venue = "Minister's Office",
                    AttendeeName = "Visiting Minister",
                    AttendeeOrganisation = "Federal Government",
                    Status = "Scheduled",
                    Source = "PARequest"
                });
            }
        }
        else if (dto.Action == "Reject")
        {
            request.Status = "Rejected";
            request.RejectReason = dto.RejectReason ?? "Slot no longer available.";
            request.RespondedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Meeting request {dto.Action.ToLower()}ed successfully." });
    }
}
