using System;
using System.Collections.Generic;
using HackathonCoordinator.WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Task = HackathonCoordinator.WebAPI.Models.Task;
using TaskStatus = HackathonCoordinator.WebAPI.Models.TaskStatus;

namespace HackathonCoordinator.WebAPI.Data;

public partial class HackathonCoordinatorContext : DbContext
{
    public HackathonCoordinatorContext()
    {
    }

    public HackathonCoordinatorContext(DbContextOptions<HackathonCoordinatorContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Chat> Chats { get; set; }

    public virtual DbSet<ChatType> ChatTypes { get; set; }

    public virtual DbSet<Competition> Competitions { get; set; }

    public virtual DbSet<FinalTeamMember> FinalTeamMembers { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<NotificationType> NotificationTypes { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<ProfileIcon> ProfileIcons { get; set; }

    public virtual DbSet<Result> Results { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Stage> Stages { get; set; }

    public virtual DbSet<Task> Tasks { get; set; }

    public virtual DbSet<TaskStatus> TaskStatuses { get; set; }

    public virtual DbSet<TaskType> TaskTypes { get; set; }

    public virtual DbSet<Team> Teams { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=HackathonCoordinatorDb;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Chats__3214EC0723074ED1");

            entity.HasIndex(e => e.TypeId, "IX_Chats_TypeId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(150);

            entity.HasOne(d => d.Type).WithMany(p => p.Chats)
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Chats_ChatTypes");
        });

        modelBuilder.Entity<ChatType>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Competition>(entity =>
        {
            entity.HasIndex(e => e.CreatedById, "IX_Competitions_CreatedById");

            entity.HasIndex(e => e.EndDate, "IX_Competitions_EndDate");

            entity.HasIndex(e => e.IsArchived, "IX_Competitions_IsArchived");

            entity.HasIndex(e => new { e.IsArchived, e.StartDate }, "IX_Competitions_IsArchived_StartDate");

            entity.HasIndex(e => e.StartDate, "IX_Competitions_StartDate");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.ResultsCreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ResultsUpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.CreatedBy).WithMany(p => p.CompetitionCreatedBies)
                .HasForeignKey(d => d.CreatedById)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Competitions_CreatedById");

            entity.HasOne(d => d.ResultsCreatedBy).WithMany(p => p.CompetitionResultsCreatedBies)
                .HasForeignKey(d => d.ResultsCreatedById)
                .HasConstraintName("FK_Competitions_Users");

            entity.HasOne(d => d.ResultsUpdatedBy).WithMany(p => p.CompetitionResultsUpdatedBies)
                .HasForeignKey(d => d.ResultsUpdatedById)
                .HasConstraintName("FK_Competitions_Users1");
        });

        modelBuilder.Entity<FinalTeamMember>(entity =>
        {
            entity.HasIndex(e => e.TeamId, "IX_FinalTeamMembers_TeamId");

            entity.HasIndex(e => e.UserId, "IX_FinalTeamMembers_UserId");

            entity.HasIndex(e => new { e.UserId, e.FixedAt }, "IX_FinalTeamMembers_UserId_FixedAt").IsDescending(false, true);

            entity.Property(e => e.FixedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PositionName).HasMaxLength(50);
            entity.Property(e => e.Username).HasMaxLength(100);

            entity.HasOne(d => d.Role).WithMany(p => p.FinalTeamMembers)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FinalTeamMembers_Roles");

            entity.HasOne(d => d.Team).WithMany(p => p.FinalTeamMembers)
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("FK_FinalTeamMembers_Teams");

            entity.HasOne(d => d.User).WithMany(p => p.FinalTeamMembers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_FinalTeamMembers_Users");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Messages__3214EC07CA949DD0");

            entity.HasIndex(e => e.ChatId, "IX_Messages_ChatId");

            entity.HasIndex(e => new { e.ChatId, e.SentAt }, "IX_Messages_ChatId_SentAt").IsDescending(false, true);

            entity.HasIndex(e => e.UserId, "IX_Messages_UserId");

            entity.Property(e => e.EditedAt).HasColumnType("datetime");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Text).HasMaxLength(1000);

            entity.HasOne(d => d.Chat).WithMany(p => p.Messages)
                .HasForeignKey(d => d.ChatId)
                .HasConstraintName("FK_Messages_ChatId");

            entity.HasOne(d => d.User).WithMany(p => p.Messages)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Messages_UserId");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__3214EC07CE823C32");

            entity.HasIndex(e => e.NotificationTypeId, "IX_Notifications_NotificationTypeId");

            entity.HasIndex(e => e.UserId, "IX_Notifications_UserId");

            entity.HasIndex(e => new { e.UserId, e.IsRead }, "IX_Notifications_UserId_IsRead");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "IX_Notifications_UserId_Unread")
                .IsDescending(false, true)
                .HasFilter("([IsRead]=(0))");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.ReadAt).HasColumnType("datetime");
            entity.Property(e => e.RelatedEntityType).HasMaxLength(20);
            entity.Property(e => e.Title).HasMaxLength(50);

            entity.HasOne(d => d.NotificationType).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.NotificationTypeId)
                .HasConstraintName("FK_Notifications_NotificationTypeId");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Notifications_UserId");
        });

        modelBuilder.Entity<NotificationType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__3214EC07C4B8AEF3");

            entity.HasIndex(e => e.Name, "UQ__Notifica__737584F66F727CA5").IsUnique();

            entity.Property(e => e.Category).HasMaxLength(15);
            entity.Property(e => e.Icon).HasMaxLength(10);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<ProfileIcon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ProfileI__3214EC07ADEC564C");

            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasIndex(e => e.CompetitionId, "IX_Results_CompetitionId");

            entity.HasIndex(e => new { e.CompetitionId, e.Place }, "IX_Results_CompetitionId_Place");

            entity.HasIndex(e => e.TeamId, "IX_Results_TeamId");

            entity.Property(e => e.Comment).HasMaxLength(300);
            entity.Property(e => e.PlaceDisplay).HasMaxLength(10);

            entity.HasOne(d => d.Competition).WithMany(p => p.Results)
                .HasForeignKey(d => d.CompetitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Results_Competitions");

            entity.HasOne(d => d.Team).WithMany(p => p.Results)
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("FK_Results_Teams");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC074348EF0D");

            entity.HasIndex(e => e.Name, "UQ__Roles__737584F652306A2F").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(25);
        });

        modelBuilder.Entity<Stage>(entity =>
        {
            entity.HasIndex(e => e.CompetitionId, "IX_Stages_CompetitionId");

            entity.HasIndex(e => new { e.CompetitionId, e.Order }, "IX_Stages_CompetitionId_Order");

            entity.HasIndex(e => new { e.StartTime, e.EndTime }, "IX_Stages_StartTime_EndTime");

            entity.HasIndex(e => new { e.CompetitionId, e.Order }, "UQ_Stages_Competition_Order").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.EndTime).HasColumnType("datetime");
            entity.Property(e => e.Location).HasMaxLength(150);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.StartTime).HasColumnType("datetime");

            entity.HasOne(d => d.Competition).WithMany(p => p.Stages)
                .HasForeignKey(d => d.CompetitionId)
                .HasConstraintName("FK_Stages_Competitions");
        });

        modelBuilder.Entity<Task>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tasks__3214EC071FB6BCF1");

            entity.HasIndex(e => e.AssignedToId, "IX_Tasks_AssignedToId");

            entity.HasIndex(e => new { e.AssignedToId, e.StatusId }, "IX_Tasks_AssignedToId_StatusId");

            entity.HasIndex(e => e.ChatId, "IX_Tasks_ChatId");

            entity.HasIndex(e => e.Deadline, "IX_Tasks_Deadline").HasFilter("([Deadline] IS NOT NULL)");

            entity.HasIndex(e => e.StatusId, "IX_Tasks_StatusId");

            entity.HasIndex(e => e.TeamId, "IX_Tasks_TeamId");

            entity.HasIndex(e => new { e.TeamId, e.StatusId }, "IX_Tasks_TeamId_StatusId");

            entity.HasIndex(e => e.TypeId, "IX_Tasks_TypeId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Deadline).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.GithubBranchName).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.AssignedTo).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Tasks_AssignedToId");

            entity.HasOne(d => d.Chat).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.ChatId)
                .HasConstraintName("FK_Tasks_Chats");

            entity.HasOne(d => d.Status).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tasks_StatusId");

            entity.HasOne(d => d.Team).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tasks_Teams");

            entity.HasOne(d => d.Type).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tasks_TypeId");
        });

        modelBuilder.Entity<TaskStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TaskStat__3214EC07DC23AB14");

            entity.HasIndex(e => e.Name, "UQ__TaskStat__737584F6DC5F6AFF").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<TaskType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TaskType__3214EC0716EA65CE");

            entity.HasIndex(e => e.Name, "UQ__TaskType__737584F66BFDEB08").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Teams__3214EC073AAA9ED0");

            entity.HasIndex(e => e.ChatId, "IX_Teams_ChatId");

            entity.HasIndex(e => e.CompetitionId, "IX_Teams_CompetitionId");

            entity.HasIndex(e => e.InviteCode, "UQ__Teams__B8659E398D2885DB").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.GitRepoName).HasMaxLength(100);
            entity.Property(e => e.InviteCode).HasMaxLength(36);
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.Chat).WithMany(p => p.Teams)
                .HasForeignKey(d => d.ChatId)
                .HasConstraintName("FK_Teams_Chats");

            entity.HasOne(d => d.Competition).WithMany(p => p.Teams)
                .HasForeignKey(d => d.CompetitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Teams_Competitions");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC073975C412");

            entity.HasIndex(e => e.GitHubUsername, "IX_Users_GitHubUsername").HasFilter("([GitHubUsername] IS NOT NULL)");

            entity.HasIndex(e => e.Login, "IX_Users_Login");

            entity.HasIndex(e => e.PositionId, "IX_Users_PositionId");

            entity.HasIndex(e => e.ProfileIconId, "IX_Users_ProfileIconId");

            entity.HasIndex(e => e.RoleId, "IX_Users_RoleId");

            entity.HasIndex(e => e.TeamId, "IX_Users_TeamId");

            entity.HasIndex(e => e.TeamId, "IX_Users_TeamId_Filtered").HasFilter("([TeamId] IS NOT NULL)");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D1053498E69DD7").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.GitHubAccessToken).HasMaxLength(255);
            entity.Property(e => e.GitHubAvatarUrl).HasMaxLength(255);
            entity.Property(e => e.GitHubUsername).HasMaxLength(100);
            entity.Property(e => e.Login).HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PositionId).HasDefaultValue(1);
            entity.Property(e => e.RoleId).HasDefaultValue(4);
            entity.Property(e => e.Username).HasMaxLength(100);

            entity.HasOne(d => d.Position).WithMany(p => p.Users)
                .HasForeignKey(d => d.PositionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Positions");

            entity.HasOne(d => d.ProfileIcon).WithMany(p => p.Users)
                .HasForeignKey(d => d.ProfileIconId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Users_ProfileIcons");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_RoleId");

            entity.HasOne(d => d.Team).WithMany(p => p.Users)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Users_TeamId");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
