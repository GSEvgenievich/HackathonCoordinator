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

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<NotificationType> NotificationTypes { get; set; }

    public virtual DbSet<ProfileIcon> ProfileIcons { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Task> Tasks { get; set; }

    public virtual DbSet<TaskStatus> TaskStatuses { get; set; }

    public virtual DbSet<TaskType> TaskTypes { get; set; }

    public virtual DbSet<TaskVote> TaskVotes { get; set; }

    public virtual DbSet<TaskVoteResponse> TaskVoteResponses { get; set; }

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
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.CreatedBy).WithMany(p => p.Competitions)
                .HasForeignKey(d => d.CreatedById)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Competitions_CreatedById");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Messages__3214EC07CA949DD0");

            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Text).HasMaxLength(1000);

            entity.HasOne(d => d.Chat).WithMany(p => p.Messages)
                .HasForeignKey(d => d.ChatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Messages_ChatId");

            entity.HasOne(d => d.User).WithMany(p => p.Messages)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Messages_UserId");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__3214EC07CE823C32");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Message).HasMaxLength(1000);

            entity.HasOne(d => d.NotificationType).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.NotificationTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notifications_NotificationTypeId");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notifications_UserId");
        });

        modelBuilder.Entity<NotificationType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__3214EC07C4B8AEF3");

            entity.HasIndex(e => e.Name, "UQ__Notifica__737584F66F727CA5").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<ProfileIcon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ProfileI__3214EC07ADEC564C");

            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC074348EF0D");

            entity.HasIndex(e => e.Name, "UQ__Roles__737584F652306A2F").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(25);
        });

        modelBuilder.Entity<Task>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tasks__3214EC071FB6BCF1");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Deadline).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.GithubBranchName).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.AssignedTo).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.AssignedToId)
                .HasConstraintName("FK_Tasks_AssignedToId");

            entity.HasOne(d => d.Chat).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.ChatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
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

        modelBuilder.Entity<TaskVote>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TaskVote__3214EC070C7FC7B9");

            entity.HasIndex(e => e.TaskId, "UQ__TaskVote__7C6949B02F246B18").IsUnique();

            entity.Property(e => e.ClosedAt).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Task).WithOne(p => p.TaskVote)
                .HasForeignKey<TaskVote>(d => d.TaskId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaskVotes_TaskId");
        });

        modelBuilder.Entity<TaskVoteResponse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TaskVote__3214EC07B7FBA58E");

            entity.HasIndex(e => new { e.TaskVoteId, e.UserId }, "UQ_TaskVote_User").IsUnique();

            entity.HasOne(d => d.TaskVote).WithMany(p => p.TaskVoteResponses)
                .HasForeignKey(d => d.TaskVoteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaskVoteResponses_TaskVoteId");

            entity.HasOne(d => d.User).WithMany(p => p.TaskVoteResponses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaskVotes_UserId");
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Teams__3214EC073AAA9ED0");

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

            entity.HasIndex(e => e.Email, "UQ__Users__A9D1053498E69DD7").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.GitHubAccessToken).HasMaxLength(255);
            entity.Property(e => e.GitHubAvatarUrl).HasMaxLength(255);
            entity.Property(e => e.GitHubUsername).HasMaxLength(100);
            entity.Property(e => e.Login).HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Username).HasMaxLength(100);

            entity.HasOne(d => d.ProfileIcon).WithMany(p => p.Users)
                .HasForeignKey(d => d.ProfileIconId)
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
