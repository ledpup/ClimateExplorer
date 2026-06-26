namespace ClimateExplorer.UnitTests;

using System.Linq;
using ClimateExplorer.Web.Client.Services.Notifications;
using ClimateExplorer.Web.Client.UiModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class NotificationServiceTests
{
    [TestMethod]
    public void AddFirstUnreadNotificationRaisesFirstUnreadEvent()
    {
        var service = new NotificationService();
        var firstUnreadEvents = 0;
        service.FirstUnreadNotificationReceived += () => firstUnreadEvents++;

        service.Add(new UserNotification { Message = "Chart warning", Type = NotificationType.Warning });

        Assert.AreEqual(1, service.UnreadCount);
        Assert.AreEqual(1, service.TotalCount);
        Assert.AreEqual(1, firstUnreadEvents);
    }

    [TestMethod]
    public void AddUnreadNotificationWhileUnreadDoesNotRaiseFirstUnreadEventAgain()
    {
        var service = new NotificationService();
        var firstUnreadEvents = 0;
        service.FirstUnreadNotificationReceived += () => firstUnreadEvents++;

        service.Add(new UserNotification { Message = "First warning", Type = NotificationType.Warning });
        service.Add(new UserNotification { Message = "Second warning", Type = NotificationType.Warning });

        Assert.AreEqual(2, service.UnreadCount);
        Assert.AreEqual(1, firstUnreadEvents);
    }

    [TestMethod]
    public void IdenticalNotificationsAreGrouped()
    {
        var service = new NotificationService();

        service.Add(new UserNotification { Message = "Data unavailable", Type = NotificationType.Warning, LocationName = "Auckland" });
        service.Add(new UserNotification { Message = " Data unavailable ", Type = NotificationType.Warning, LocationName = "auckland" });

        Assert.HasCount(1, service.Notifications);
        Assert.AreEqual(2, service.Notifications[0].Count);
        Assert.AreEqual(2, service.UnreadCount);
        Assert.AreEqual(2, service.TotalCount);
    }

    [TestMethod]
    public void GroupedNotificationBecomesUnreadWhenRepeated()
    {
        var service = new NotificationService();
        var notification = service.Add(new UserNotification { Message = "Data unavailable", Type = NotificationType.Warning });
        service.MarkAllRead();

        service.Add(new UserNotification { Message = "Data unavailable", Type = NotificationType.Warning });

        Assert.AreSame(notification, service.Notifications[0]);
        Assert.IsFalse(service.Notifications[0].IsRead);
        Assert.AreEqual(2, service.Notifications[0].Count);
        Assert.AreEqual(2, service.UnreadCount);
    }

    [TestMethod]
    public void MarkAllReadClearsUnreadCountButLeavesNotifications()
    {
        var service = new NotificationService();
        service.Add(new UserNotification { Message = "First warning", Type = NotificationType.Warning });
        service.Add(new UserNotification { Message = "Second warning", Type = NotificationType.Warning });

        service.MarkAllRead();

        Assert.AreEqual(0, service.UnreadCount);
        Assert.AreEqual(2, service.TotalCount);
        Assert.IsTrue(service.Notifications.All(notification => notification.IsRead));
    }

    [TestMethod]
    public void GroupingKeyPreservesActionUrl()
    {
        var service = new NotificationService();

        service.Add(new UserNotification { Message = "View location", ActionText = "View location", ActionUrl = "/location/one" });
        service.Add(new UserNotification { Message = "View location", ActionText = "View location", ActionUrl = "/location/two" });

        Assert.AreEqual(2, service.Notifications.Count);
    }

    [TestMethod]
    public void RequestOpenPanelRaisesOpenPanelEvent()
    {
        var service = new NotificationService();
        var openPanelEvents = 0;
        service.OpenPanelRequested += () => openPanelEvents++;

        service.RequestOpenPanel();

        Assert.AreEqual(1, openPanelEvents);
    }
}
