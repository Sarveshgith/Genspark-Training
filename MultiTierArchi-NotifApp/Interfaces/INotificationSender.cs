using System;
using MultiTierArchi_NotifApp.Models;

namespace MultiTierArchi_NotifApp.Interfaces;

internal interface INotificationSender
{
    public void SendNotif(User user, Notification notification);

}
