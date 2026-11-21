using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinisterDiary.Api.Data;
using MinisterDiary.Api.DTOs;
using MinisterDiary.Api.Models;
using MinisterDiary.Api.Services;

namespace MinisterDiary.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;

    public AuthController(AppDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    // A1: POST /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.Trim().ToLower());
        if (user == null || !user.IsActive)
            return Unauthorized(new { message = "Invalid email or account is inactive." });

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
            return Unauthorized(new { message = "Invalid credentials." });

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var (token, expiresAt) = _jwtService.GenerateToken(user, request.RememberMe);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            FullName = user.FullName,
            Initials = user.Initials,
            Role = user.Role
        });
    }

    // A2: GET /api/auth/me
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        return Ok(new
        {
            id = user.Id,
            fullName = user.FullName,
            initials = user.Initials,
            email = user.Email,
            role = user.Role,
            designation = user.Designation,
            portfolio = user.Portfolio,
            isActive = user.IsActive,
            mustChangePassword = user.MustChangePassword
        });
    }

    // A3: PUT /api/auth/profile
    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        var oldName = user.FullName;
        user.FullName = request.FullName.Trim();
        user.Email = request.Email.Trim();

        // Audit log
        _context.AuditLogs.Add(new AuditLog
        {
            Action = "Profile updated",
            Category = "User",
            Description = $"User {user.Email} updated their profile.",
            SubjectName = user.FullName,
            SubjectRole = user.Role,
            ByUserId = user.Id,
            ByName = user.FullName,
            OldValue = oldName,
            NewValue = user.FullName
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = user.Id,
            fullName = user.FullName,
            email = user.Email,
            role = user.Role
        });
    }

    // A4: PUT /api/auth/change-password
    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Incorrect current password." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.MustChangePassword = false;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Password changed successfully." });
    }

    // A5: POST /api/auth/forgot-password
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.Trim().ToLower());

        // Spec: Same response even if email does not exist (security practice)
        if (user != null)
        {
            // Notify all admins
            var admins = await _context.Users.Where(u => u.Role == "Admin" && u.IsActive).ToListAsync();
            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Title = "Password Reset Request",
                    Message = $"User {user.FullName} ({user.Email}) requested a password reset.",
                    Category = "Password",
                    RelatedId = user.Id
                });
            }

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Password reset requested",
                Category = "User",
                Description = $"Password reset requested for {user.Email}",
                SubjectName = user.FullName,
                SubjectRole = user.Role,
                ByUserId = user.Id,
                ByName = user.FullName
            });

            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "If this email is registered, an admin has been notified to issue a reset." });
    }
}
