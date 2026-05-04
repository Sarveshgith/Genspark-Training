using System;
using NotificationApp.Models;

namespace NotificationApp.Interfaces;

internal interface INotification
{
    public void SendNotif(User user, Notification notification);
}
