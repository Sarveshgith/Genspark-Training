using System;

namespace NotificationApp.Models;

internal class Notification : IComparable<Notification>
{
    public required string Message {get; set;} = string.Empty;
    public DateTime SentTime {get; set;} = DateTime.Now;

    public override string ToString()
    {
        return $"Message: {Message}\nSent Time: {SentTime}";
    }

    //Reference Purpose
    public int CompareTo(Notification? other)
    {
        if (other == null) return 1;
        return this.SentTime.CompareTo(other.SentTime);
    }
}
