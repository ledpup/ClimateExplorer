namespace ClimateExplorer.Web.Client.Services.Notifications;

using ClimateExplorer.Web.Client.UiModel;

public interface IUserNotificationService
{
    event Action? Changed;

    event Action? FirstUnreadNotificationReceived;

    event Action? OpenPanelRequested;

    IReadOnlyList<UserNotification> Notifications { get; }

    int TotalCount { get; }

    int UnreadCount { get; }

    UserNotification Add(UserNotification notification);

    void MarkAllRead();

    void SetRead(Guid notificationId, bool isRead);

    void RequestOpenPanel();
}
