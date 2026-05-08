using System;

namespace MultiTierArchi_NotifApp.Models;

internal class User
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNo { get; set; } = string.Empty;

    //Default constructor for deserialization and other purposes
    public User() {}
    public User(string name, string email, string phoneNo)
    {
        Name = name;
        Email = email;
        PhoneNo = phoneNo;
    }

    public override string ToString()
    {
        return $"Name: {Name}\nEmail: {Email}\nPhone Number: {PhoneNo}";
    }
}
