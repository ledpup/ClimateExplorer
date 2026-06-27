namespace ClimateExplorer.UnitTests;

using System;
using System.Linq;
using ClimateExplorer.Web.Client.Services.Notifications;
using ClimateExplorer.Web.Client.UiModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class NotificationServiceTests
{
    [TestMethod]
    public void Add_NoUnreadNotifications_RaisesFirstUnreadEvent()
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
    public void Add_UnreadNotificationsExist_DoesNotRaiseFirstUnreadEventAgain()
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
    public void Add_IdenticalNotifications_GroupsNotifications()
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
    public void Add_GroupedNotificationWasRead_MarksGroupedNotificationUnread()
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
    public void MarkAllRead_UnreadNotificationsExist_ClearsUnreadCountButLeavesNotifications()
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
    public void SetRead_UnreadNotification_MarksNotificationReadAndClearsUnreadCount()
    {
        var service = new NotificationService();
        var notification = service.Add(new UserNotification { Message = "First warning", Type = NotificationType.Warning });

        service.SetRead(notification.Id, true);

        Assert.IsTrue(notification.IsRead);
        Assert.AreEqual(0, service.UnreadCount);
        Assert.AreEqual(1, service.TotalCount);
    }

    [TestMethod]
    public void SetRead_ReadGroupedNotification_MarksGroupedNotificationUnread()
    {
        var service = new NotificationService();
        var notification = service.Add(new UserNotification { Message = "First warning", Type = NotificationType.Warning });
        service.Add(new UserNotification { Message = "First warning", Type = NotificationType.Warning });
        service.MarkAllRead();

        service.SetRead(notification.Id, false);

        Assert.IsFalse(notification.IsRead);
        Assert.AreEqual(2, service.UnreadCount);
        Assert.AreEqual(2, service.TotalCount);
    }

    [TestMethod]
    public void SetRead_NotificationDoesNotExist_DoesNothing()
    {
        var service = new NotificationService();
        service.Add(new UserNotification { Message = "First warning", Type = NotificationType.Warning });

        service.SetRead(Guid.NewGuid(), true);

        Assert.AreEqual(1, service.UnreadCount);
        Assert.AreEqual(1, service.TotalCount);
    }

    [TestMethod]
    public void Add_SameMessageWithDifferentActionUrls_DoesNotGroupNotifications()
    {
        var service = new NotificationService();

        service.Add(new UserNotification { Message = "View location", ActionText = "View location", ActionUrl = "/location/one" });
        service.Add(new UserNotification { Message = "View location", ActionText = "View location", ActionUrl = "/location/two" });

        Assert.HasCount(2, service.Notifications);
    }

    [TestMethod]
    public void RequestOpenPanel_WhenCalled_RaisesOpenPanelEvent()
    {
        var service = new NotificationService();
        var openPanelEvents = 0;
        service.OpenPanelRequested += () => openPanelEvents++;

        service.RequestOpenPanel();

        Assert.AreEqual(1, openPanelEvents);
    }
}
