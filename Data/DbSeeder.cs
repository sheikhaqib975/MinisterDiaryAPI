using BCrypt.Net;
using MinisterDiary.Api.Models;

namespace MinisterDiary.Api.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext context)
    {
        // 1. Seed Users if not present
        if (!context.Users.Any())
        {
            var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!");

            var admin = new User
            {
                FullName = "Admin Officer",
                Initials = "AO",
                Email = "admin@diary.gov",
                PasswordHash = defaultPasswordHash,
                Role = "Admin",
                Designation = "System Administrator",
                IsActive = true
            };

            var minister = new User
            {
                FullName = "Raza Bashir",
                Initials = "RB",
                Email = "minister@diary.gov",
                PasswordHash = defaultPasswordHash,
                Role = "Minister",
                Portfolio = "Ministry of Public Works & Infrastructure",
                Designation = "Cabinet Minister",
                IsActive = true
            };

            var pa = new User
            {
                FullName = "Kareem Farooqi",
                Initials = "KF",
                Email = "pa@diary.gov",
                PasswordHash = defaultPasswordHash,
                Role = "PA",
                Designation = "Personal Assistant to Minister",
                IsActive = true
            };

            var secretariat = new User
            {
                FullName = "Sajid Mehmood",
                Initials = "SM",
                Email = "secretariat@diary.gov",
                PasswordHash = defaultPasswordHash,
                Role = "Secretariat",
                Designation = "Protocol Officer",
                IsActive = true
            };

            var pm = new User
            {
                FullName = "Prime Minister Office",
                Initials = "PM",
                Email = "pm@diary.gov",
                PasswordHash = defaultPasswordHash,
                Role = "PM",
                Designation = "Cabinet Secretariat",
                IsActive = true
            };

            context.Users.AddRange(admin, minister, pa, secretariat, pm);
            context.SaveChanges();

            // 2. Seed Diary for Minister & Assign PA
            var diary = new Diary
            {
                MinisterId = minister.Id,
                PaId = pa.Id,
                Status = "Active",
                StartDate = DateTime.UtcNow
            };
            context.Diaries.Add(diary);
            context.SaveChanges();

            // 3. Link Secretariat staff to Diary
            var diaryStaff = new DiaryStaff
            {
                DiaryId = diary.Id,
                UserId = secretariat.Id
            };
            context.DiaryStaff.Add(diaryStaff);
            context.SaveChanges();

            // 4. Seed sample Free Slots
            var today = DateTime.Today;
            var slot1 = new FreeSlot
            {
                DiaryId = diary.Id,
                Date = today,
                StartTime = "10:00",
                EndTime = "11:00",
                IsBooked = true
            };
            var slot2 = new FreeSlot
            {
                DiaryId = diary.Id,
                Date = today,
                StartTime = "14:00",
                EndTime = "15:00",
                IsBooked = false
            };
            var slot3 = new FreeSlot
            {
                DiaryId = diary.Id,
                Date = today.AddDays(1),
                StartTime = "11:30",
                EndTime = "12:30",
                IsBooked = false
            };

            context.FreeSlots.AddRange(slot1, slot2, slot3);
            context.SaveChanges();

            // 5. Seed sample Meetings
            var meeting1 = new Meeting
            {
                DiaryId = diary.Id,
                FreeSlotId = slot1.Id,
                Title = "Cabinet Committee on Economic Affairs",
                Date = today,
                StartTime = "10:00",
                EndTime = "11:00",
                DurationMinutes = 60,
                Venue = "Cabinet Secretariat, Committee Room A",
                AttendeeName = "Chief Secretary",
                AttendeeOrganisation = "Planning Commission",
                AttendeeEmail = "secretary@planning.gov",
                Notes = "Review quarterly budget and project clearances.",
                Status = "Scheduled",
                Priority = "Urgent",
                Source = "Direct",
                ScheduledByUserId = pa.Id,
                ScheduledByName = pa.FullName,
                ScheduledByRole = "PA"
            };

            context.Meetings.Add(meeting1);

            // 6. Seed sample Notification
            var notif = new Notification
            {
                UserId = minister.Id,
                Title = "New Meeting Scheduled",
                Message = "PA scheduled 'Cabinet Committee on Economic Affairs' for 10:00 AM.",
                Category = "Meeting",
                RelatedId = meeting1.Id,
                IsRead = false
            };
            context.Notifications.Add(notif);

            context.SaveChanges();
        }
    }
}
