using System;

namespace NotificationApp.Models;

internal partial class User
{
    public override string ToString()
    {
        return $"Id: {Id}\nName: {Name}\nEmail: {Email}\nPhone Number: {PhoneNo}";
    }
}
