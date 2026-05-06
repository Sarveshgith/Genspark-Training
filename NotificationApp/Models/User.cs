using System;

namespace NotificationApp.Models;

//This Class handles the properties and constructors of the User class.
internal class User
{
    //Assuming EMail is the PK
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNo { get; set; } = string.Empty;

    public User() {}
    public User(string name, string email, string phoneNo)
    {
        Name = name;
        Email = email;
        PhoneNo = phoneNo;
    }

    //ToString => Returns a string representation of the User object
    public override string ToString()
    {
        return $"Name: {Name}\nEmail: {Email}\nPhone Number: {PhoneNo}";
    }
}
