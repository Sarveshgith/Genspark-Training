using System;

namespace NotificationApp.Models;

//This Partial class handles the functionality of the User class, such as equality checks and comparisons.

//Even though we are assuming Name is the PK, as its not unique in real world, We will consider 
//both Name and Email as Candidate Keys in this case for equality check.
internal partial class User : IEquatable<User>, IComparable<User>
{
    public override string ToString()
    {
        return $"Name: {Name}\nEmail: {Email}\nPhone Number: {PhoneNo}";
    }

     public int CompareTo(User? other)
    {
        if (other == null) return 1;
        return this.Name.CompareTo(other.Name);
    }

    //Reference Purpose
    public static bool operator ==(User? a, User? b)
    {
        if (a is null || b is null) return a is null && b is null;
        return (a.Name == b.Name && a.Email == b.Email);
    }
        
    public static bool operator !=(User? a, User? b)
    {
        if (a is null || b is null) return a is not null || b is not null;
        return (a.Name != b.Name || a.Email != b.Email);
    }

    //Can use 'RECORD' type for avoiding this
    public bool Equals(User? other)
    {
        if (other == null) return false;
        return (this.Name == other.Name && this.Email == other.Email);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not User user) return false;
        return (this.Name == user.Name && this.Email == user.Email);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Email);
    }
}
