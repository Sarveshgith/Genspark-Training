using System;

namespace MultiTierArchi_NotifApp.Models;

internal class Notification
{
    public required string NotifType {get; set;} = string.Empty;
    public required string Message {get; set;} = string.Empty;
    public DateTime SentTime {get; set;} = DateTime.Now;

    public override string ToString()
    {
        return $"Message: {Message}\nSent Time: {SentTime}\nNotification Type: {NotifType}";
    }
}
