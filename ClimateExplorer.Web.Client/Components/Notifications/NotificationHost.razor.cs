namespace ClimateExplorer.Web.Client.Components.Notifications;

using System.Globalization;
using ClimateExplorer.Web.Client.Components.Common;
using ClimateExplorer.Web.Client.Services.Notifications;
using ClimateExplorer.Web.Client.UiModel;
using Microsoft.AspNetCore.Components;

public partial class NotificationHost : IDisposable
{
    private static readonly TimeSpan ToastDuration = TimeSpan.FromSeconds(5);
    private bool toastVisible;
    private SidePanel? notificationsSidePanel;
    private CancellationTokenSource? toastCancellationTokenSource;

    [Inject]
    private IUserNotificationService NotificationService { get; set; } = default!;

    private IReadOnlyList<UserNotification> Notifications => NotificationService.Notifications;

    public void Dispose()
    {
        NotificationService.Changed -= OnNotificationsChanged;
        NotificationService.FirstUnreadNotificationReceived -= OnFirstUnreadNotificationReceived;
        NotificationService.OpenPanelRequested -= OnOpenPanelRequested;
        toastCancellationTokenSource?.Cancel();
        toastCancellationTokenSource?.Dispose();
    }

    protected override void OnInitialized()
    {
        NotificationService.Changed += OnNotificationsChanged;
        NotificationService.FirstUnreadNotificationReceived += OnFirstUnreadNotificationReceived;
        NotificationService.OpenPanelRequested += OnOpenPanelRequested;
    }

    private void OnFirstUnreadNotificationReceived()
    {
        _ = ShowToastAsync();
    }

    private void OnOpenPanelRequested()
    {
        _ = OpenNotificationsPanelAsync();
    }

    private void OnNotificationsChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task OpenNotificationsPanelAsync()
    {
        HideToast();

        if (notificationsSidePanel is not null)
        {
            NotificationService.MarkAllRead();
            await notificationsSidePanel.ShowAsync();
        }
    }

    private async Task ShowToastAsync()
    {
        toastCancellationTokenSource?.Cancel();
        toastCancellationTokenSource?.Dispose();
        toastCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = toastCancellationTokenSource.Token;

        toastVisible = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            await Task.Delay(ToastDuration, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        toastVisible = false;
        await InvokeAsync(StateHasChanged);
    }

    private void HideToast()
    {
        toastCancellationTokenSource?.Cancel();
        toastVisible = false;
    }

    private void OnReadChanged(Guid notificationId, ChangeEventArgs args)
    {
        if (args.Value is bool isRead)
        {
            NotificationService.SetRead(notificationId, isRead);
        }
    }

    private string FormatLocation(UserNotification notification)
    {
        return string.IsNullOrWhiteSpace(notification.LocationName)
            ? "N/A"
            : notification.LocationName;
    }

    private string FormatTime(DateTimeOffset timestamp)
    {
        return timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    }

    private string GetTypeClass(NotificationType type)
    {
        return $"notification-type notification-type-{type.ToString().ToLowerInvariant()}";
    }

    private string CreateReadLabel(UserNotification notification)
    {
        return $"Mark notification '{notification.Message}' as read";
    }
}
