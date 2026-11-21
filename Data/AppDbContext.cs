using Microsoft.EntityFrameworkCore;
using MinisterDiary.Api.Models;

namespace MinisterDiary.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // 11 Master Tables as specified in the Master Document
    public DbSet<User> Users { get; set; }
    public DbSet<Diary> Diaries { get; set; }
    public DbSet<DiaryStaff> DiaryStaff { get; set; }
    public DbSet<Meeting> Meetings { get; set; }
    public DbSet<FreeSlot> FreeSlots { get; set; }
    public DbSet<MeetingRequest> MeetingRequests { get; set; }
    public DbSet<MinisterChangeover> MinisterChangeovers { get; set; }
    public DbSet<CabinetMeeting> CabinetMeetings { get; set; }
    public DbSet<CabinetMeetingAttendee> CabinetMeetingAttendees { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map exact table names (snake_case / lowercase as per MySQL doc)
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Diary>().ToTable("diaries");
        modelBuilder.Entity<DiaryStaff>().ToTable("diary_staff");
        modelBuilder.Entity<Meeting>().ToTable("meetings");
        modelBuilder.Entity<FreeSlot>().ToTable("free_slots");
        modelBuilder.Entity<MeetingRequest>().ToTable("meeting_requests");
        modelBuilder.Entity<MinisterChangeover>().ToTable("minister_changeovers");
        modelBuilder.Entity<CabinetMeeting>().ToTable("cabinet_meetings");
        modelBuilder.Entity<CabinetMeetingAttendee>().ToTable("cabinet_meeting_attendees");
        modelBuilder.Entity<Notification>().ToTable("notifications");
        modelBuilder.Entity<AuditLog>().ToTable("audit_logs");

        // User unique email
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Diary relationships
        modelBuilder.Entity<Diary>()
            .HasOne(d => d.Minister)
            .WithMany()
            .HasForeignKey(d => d.MinisterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Diary>()
            .HasOne(d => d.Pa)
            .WithMany()
            .HasForeignKey(d => d.PaId)
            .OnDelete(DeleteBehavior.SetNull);

        // Meeting relationships
        modelBuilder.Entity<Meeting>()
            .HasOne(m => m.Diary)
            .WithMany(d => d.Meetings)
            .HasForeignKey(m => m.DiaryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Meeting>()
            .HasOne(m => m.FreeSlot)
            .WithMany()
            .HasForeignKey(m => m.FreeSlotId)
            .OnDelete(DeleteBehavior.SetNull);

        // MeetingRequest relationships
        modelBuilder.Entity<MeetingRequest>()
            .HasOne(r => r.FromDiary)
            .WithMany()
            .HasForeignKey(r => r.FromDiaryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MeetingRequest>()
            .HasOne(r => r.ToDiary)
            .WithMany()
            .HasForeignKey(r => r.ToDiaryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MeetingRequest>()
            .HasOne(r => r.FromPa)
            .WithMany()
            .HasForeignKey(r => r.FromPaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MeetingRequest>()
            .HasOne(r => r.ToPa)
            .WithMany()
            .HasForeignKey(r => r.ToPaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MeetingRequest>()
            .HasOne(r => r.FreeSlot)
            .WithMany()
            .HasForeignKey(r => r.FreeSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        // CabinetMeetingAttendee relationships
        modelBuilder.Entity<CabinetMeetingAttendee>()
            .HasOne(a => a.CabinetMeeting)
            .WithMany(c => c.Attendees)
            .HasForeignKey(a => a.CabinetMeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CabinetMeetingAttendee>()
            .HasOne(a => a.Minister)
            .WithMany()
            .HasForeignKey(a => a.MinisterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CabinetMeetingAttendee>()
            .HasOne(a => a.Diary)
            .WithMany()
            .HasForeignKey(a => a.DiaryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
