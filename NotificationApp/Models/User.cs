using System;

namespace NotificationApp.Models;

//This Partial class handles the properties and constructors of the User class.
internal partial class User
{
    public int Id { get; set; }
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
}
