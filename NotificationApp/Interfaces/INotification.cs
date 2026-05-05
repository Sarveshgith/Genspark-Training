using System;
using NotificationApp.Models;

namespace NotificationApp.Interfaces;

//Contract for sending notifications. Implemented by EmailNotification and SMSNotification classes.
internal interface INotification
{
    public void SendNotif(User user, Notification notification);
}
