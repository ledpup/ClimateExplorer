namespace ClimateExplorer.Web.Client.Services.Notifications;

using ClimateExplorer.Web.Client.UiModel;

public sealed class NotificationService : IUserNotificationService
{
    private readonly List<UserNotification> notifications = [];

    public event Action? Changed;

    public event Action? FirstUnreadNotificationReceived;

    public event Action? OpenPanelRequested;

    public IReadOnlyList<UserNotification> Notifications => notifications;

    public int TotalCount => notifications.Sum(notification => notification.Count);

    public int UnreadCount => notifications
        .Where(notification => !notification.IsRead)
        .Sum(notification => notification.Count);

    public UserNotification Add(UserNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (string.IsNullOrWhiteSpace(notification.Message))
        {
            throw new ArgumentException("Notification message is required.", nameof(notification));
        }

        var previousUnreadCount = UnreadCount;
        var existing = notifications.FirstOrDefault(candidate => HasSameGroupingKey(candidate, notification));

        if (existing is null)
        {
            notification.UpdatedAt = notification.CreatedAt;
            notifications.Insert(0, notification);
            Notify(previousUnreadCount);
            return notification;
        }

        existing.Count += Math.Max(1, notification.Count);
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        existing.IsRead = false;

        notifications.Remove(existing);
        notifications.Insert(0, existing);

        Notify(previousUnreadCount);
        return existing;
    }

    public void MarkAllRead()
    {
        if (UnreadCount == 0)
        {
            return;
        }

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        Changed?.Invoke();
    }

    public void RequestOpenPanel()
    {
        OpenPanelRequested?.Invoke();
    }

    private static bool HasSameGroupingKey(UserNotification left, UserNotification right)
    {
        return Normalize(left.Message) == Normalize(right.Message) &&
               left.Type == right.Type &&
               left.LocationId == right.LocationId &&
               Normalize(left.LocationName) == Normalize(right.LocationName) &&
               Normalize(left.ActionUrl) == Normalize(right.ActionUrl);
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
    }

    private void Notify(int previousUnreadCount)
    {
        Changed?.Invoke();

        if (previousUnreadCount == 0 && UnreadCount > 0)
        {
            FirstUnreadNotificationReceived?.Invoke();
        }
    }
}
