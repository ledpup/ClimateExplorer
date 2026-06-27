namespace ClimateExplorer.Web.Client.Components.Notifications;

using ClimateExplorer.Web.Client.Services.Notifications;
using Microsoft.AspNetCore.Components;

public partial class NotificationBell : IDisposable
{
    private const int AnimationDurationMs = 700;
    private bool shouldAnimate;
    private CancellationTokenSource? animationCancellationTokenSource;

    [Inject]
    private IUserNotificationService NotificationService { get; set; } = default!;

    private int UnreadCount => NotificationService.UnreadCount;
    private bool HasNotifications => NotificationService.TotalCount > 0;
    private string BadgeText => UnreadCount > 99 ? "99+" : UnreadCount.ToString();
    private string UnreadLabel => UnreadCount == 1 ? "1 unread notification" : $"{UnreadCount} unread notifications";
    private string AriaLabel => HasNotifications
        ? UnreadCount > 0
            ? $"Open notifications, {UnreadLabel}"
            : "Open notifications"
        : "No notifications";

    private string BellClass
    {
        get
        {
            var className = "notification-bell";

            if (UnreadCount > 0)
            {
                className += " has-unread";
            }

            if (shouldAnimate)
            {
                className += " ring-once";
            }

            return className;
        }
    }

    public void Dispose()
    {
        NotificationService.Changed -= OnNotificationsChanged;
        NotificationService.FirstUnreadNotificationReceived -= OnFirstUnreadNotificationReceived;
        animationCancellationTokenSource?.Cancel();
        animationCancellationTokenSource?.Dispose();
    }

    protected override void OnInitialized()
    {
        NotificationService.Changed += OnNotificationsChanged;
        NotificationService.FirstUnreadNotificationReceived += OnFirstUnreadNotificationReceived;
    }

    private void OpenNotifications()
    {
        if (HasNotifications)
        {
            NotificationService.RequestOpenPanel();
        }
    }

    private void OnNotificationsChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private void OnFirstUnreadNotificationReceived()
    {
        _ = PlayAnimationAsync();
    }

    private async Task PlayAnimationAsync()
    {
        animationCancellationTokenSource?.Cancel();
        animationCancellationTokenSource?.Dispose();
        animationCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = animationCancellationTokenSource.Token;

        shouldAnimate = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            await Task.Delay(AnimationDurationMs, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        shouldAnimate = false;
        await InvokeAsync(StateHasChanged);
    }
}
