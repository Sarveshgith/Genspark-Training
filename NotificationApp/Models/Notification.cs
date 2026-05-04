using System;

namespace NotificationApp.Models;

internal class Notification
{
    public required string Message {get; set;} = string.Empty;
    public DateTime SentTime {get; set;} = DateTime.Now;
}
