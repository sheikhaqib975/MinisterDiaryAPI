using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinisterDiary.Api.Data;
using MinisterDiary.Api.DTOs;
using MinisterDiary.Api.Models;

namespace MinisterDiary.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    // G1: GET /api/admin/dashboard - System Overview 2x2 Stat Cards
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var totalUsers = await _context.Users.CountAsync();
        var activeUsers = await _context.Users.Where(u => u.IsActive).CountAsync();
        var inactiveUsers = await _context.Users.Where(u => !u.IsActive).CountAsync();
        var activeDiaries = await _context.Diaries.Where(d => d.Status == "Active").CountAsync();

        return Ok(new
        {
            totalUsers,
            active = activeUsers,
            inactive = inactiveUsers,
            diary = activeDiaries
        });
    }

    // G2: GET /api/admin/users - List users with status & role filters
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? status, [FromQuery] string? role, [FromQuery] string? search)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(status) && status != "All")
        {
            bool isActive = status == "Active";
            query = query.Where(u => u.IsActive == isActive);
        }

        if (!string.IsNullOrEmpty(role) && role != "All roles")
        {
            query = query.Where(u => u.Role == role);
        }

        if (!string.IsNullOrEmpty(search))
        {
            var s = search.ToLower().Trim();
            query = query.Where(u => u.FullName.ToLower().Contains(s) || u.Email.ToLower().Contains(s));
        }

        var users = await query
            .OrderBy(u => u.FullName)
            .Select(u => new
            {
                id = u.Id,
                initials = u.Initials,
                name = u.FullName,
                roleAndEmail = $"{u.Role} · {u.Email}",
                status = u.IsActive ? "Active" : "Inactive",
                role = u.Role,
                email = u.Email
            })
            .ToListAsync();

        return Ok(users);
    }

    // G3: POST /api/admin/users - Add new user
    [HttpPost("users")]
    public async Task<IActionResult> AddUser([FromBody] CreateUserDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower()))
            return BadRequest(new { message = "User with this email already exists." });

        var user = new User
        {
            FullName = dto.FullName,
            Initials = string.IsNullOrWhiteSpace(dto.Initials) ? dto.FullName.Substring(0, 2).ToUpper() : dto.Initials,
            Email = dto.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            Designation = dto.Designation,
            Portfolio = dto.Portfolio,
            IsActive = true
        };

        _context.Users.Add(user);

        // Audit log
        _context.AuditLogs.Add(new AuditLog
        {
            Action = "Account created",
            Category = "User",
            Description = $"Admin created new {user.Role} account for {user.FullName}",
            SubjectName = user.FullName,
            SubjectRole = user.Role,
            ByUserId = 1,
            ByName = "System Administrator"
        });

        await _context.SaveChangesAsync();
        return Ok(new { message = "User account created successfully.", userId = user.Id });
    }

    // G4: PUT /api/admin/users/{id} - Edit user
    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found." });

        user.FullName = dto.FullName;
        user.Email = dto.Email.Trim().ToLower();
        user.Role = dto.Role;
        user.Designation = dto.Designation;
        user.Portfolio = dto.Portfolio;

        await _context.SaveChangesAsync();
        return Ok(new { message = "User updated successfully." });
    }

    // G5: PUT /api/admin/users/{id}/status - Toggle activate / deactivate
    [HttpPut("users/{id}/status")]
    public async Task<IActionResult> ToggleUserStatus(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found." });

        user.IsActive = !user.IsActive;

        _context.AuditLogs.Add(new AuditLog
        {
            Action = user.IsActive ? "Account activated" : "Account deactivated",
            Category = "User",
            Description = $"User {user.FullName} was {(user.IsActive ? "activated" : "deactivated")}.",
            SubjectName = user.FullName,
            SubjectRole = user.Role,
            ByUserId = 1,
            ByName = "System Administrator"
        });

        await _context.SaveChangesAsync();
        return Ok(new { message = $"User status changed to {(user.IsActive ? "Active" : "Inactive")}." });
    }

    // G6: POST /api/admin/changeover - Minister Changeover
    [HttpPost("changeover")]
    public async Task<IActionResult> MinisterChangeover([FromBody] MinisterChangeoverDto dto)
    {
        var oldDiary = await _context.Diaries.FirstOrDefaultAsync(d => d.MinisterId == dto.OutgoingMinisterId && d.Status == "Active");
        if (oldDiary == null)
            return NotFound(new { message = "Active diary not found for outgoing minister." });

        // 1. Mark old diary as ReadOnly
        oldDiary.Status = "ReadOnly";
        oldDiary.EndDate = dto.TransitionDate;

        // 2. Create Changeover record
        var changeover = new MinisterChangeover
        {
            OutgoingMinisterId = dto.OutgoingMinisterId,
            IncomingMinisterId = dto.IncomingMinisterId,
            EndDate = dto.TransitionDate,
            StartDate = dto.TransitionDate,
            Reason = dto.Reason,
            Status = "Completed"
        };
        _context.MinisterChangeovers.Add(changeover);
        await _context.SaveChangesAsync();

        // 3. Create new diary for incoming minister
        var newDiary = new Diary
        {
            MinisterId = dto.IncomingMinisterId,
            PaId = oldDiary.PaId,
            Status = "Active",
            StartDate = dto.TransitionDate
        };
        _context.Diaries.Add(newDiary);
        await _context.SaveChangesAsync();

        // 4. Transfer future scheduled meetings to new diary
        var upcomingMeetings = await _context.Meetings
            .Where(m => m.DiaryId == oldDiary.Id && m.Date >= dto.TransitionDate && m.Status != "Cancelled")
            .ToListAsync();

        foreach (var m in upcomingMeetings)
        {
            m.DiaryId = newDiary.Id;
            m.Source = "Transferred";
            m.InheritedStatus = "Pending";
            m.ChangeoverId = changeover.Id;
        }

        // 5. Audit log
        _context.AuditLogs.Add(new AuditLog
        {
            Action = "Minister changeover completed",
            Category = "Changeover",
            Description = $"Transferred {upcomingMeetings.Count} meetings from minister {dto.OutgoingMinisterId} to {dto.IncomingMinisterId}",
            ByUserId = 1,
            ByName = "System Administrator"
        });

        await _context.SaveChangesAsync();
        return Ok(new
        {
            message = "Minister changeover completed successfully.",
            transferredMeetingsCount = upcomingMeetings.Count,
            newDiaryId = newDiary.Id
        });
    }

    // G7: GET /api/admin/audit-logs - View security audit logs
    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs()
    {
        var logs = await _context.AuditLogs
            .OrderByDescending(a => a.CreatedAt)
            .Take(50)
            .Select(a => new
            {
                id = a.Id,
                action = a.Action,
                category = a.Category,
                description = a.Description,
                byName = a.ByName,
                createdAt = a.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            })
            .ToListAsync();

        return Ok(logs);
    }
}
