using System;

namespace NotificationApp.Models;

internal class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string Message {get; set;} = string.Empty;
    public string NotifType { get; set; } = string.Empty;
    public DateTime SentDate { get; set; } = DateTime.Now;

    public override string ToString()
    {
        return $"User Id: {UserId}\nType: {NotifType}\nMessage: {Message}\nSent Date: {SentDate}";
    }
}
