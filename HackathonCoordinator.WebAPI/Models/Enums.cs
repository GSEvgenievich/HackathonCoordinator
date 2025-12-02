namespace HackathonCoordinator.WebAPI.Models
{
    /// <summary>
    /// Enum для ролей пользователей (соответствует таблице Roles)
    /// </summary>
    public enum Roles
    {
        Captain = 1,
        Member = 2,
        Organizer = 3
    }

    /// <summary>
    /// Enum для типов уведомлений (соответствует таблице NotificationTypes)
    /// </summary>
    public enum NotificationTypes
    {
        NewTask = 1,
        TaskAssigned = 2,
        TaskCompleted = 3,
        TaskConfirmed = 4,
        TaskCancelled = 5,
        TaskDeadlineApproaching = 6,
        ImportantTaskChatMessage = 7,
        NewTeamMember = 8,
        TeamMemberLeft = 9,
        MemberKickedFromTeam = 10,
        NewTeamCaptainAppointed = 11,
        BecameTeamCaptain = 12,
        GitHubRepoCreated = 13,
        ImportantTeamChatMessage = 14,
        NewTeamCreated = 15,
        NewCompetitionCreated = 16,
        CompetitionStarted = 17,
        CompetitionEnded = 18,
        SystemNotification = 19,
        TeamDeleted = 20,
        TaskCompletionCancelled = 21,
        TaskDeadlineExpired = 22
    }

    /// <summary>
    /// Enum для статусов задач (соответствует таблице TaskStatuses)
    /// </summary>
    public enum TaskStatuses
    {
        Pending = 1,
        InProgress = 2,
        InReview = 3,
        Completed = 4,
        Cancelled = 5
    }

    /// <summary>
    /// Enum для типов задач (соответствует таблице TaskTypes)
    /// </summary>
    public enum TaskTypes
    {
        Feature = 1,
        Bug = 2,
        Documentation = 3
    }

    /// <summary>
    /// Enum для типов чатов (соответствует таблице ChatTypes)
    /// </summary>
    public enum ChatTypes
    {
        TeamChat = 1,
        TaskChat = 2
    }
}