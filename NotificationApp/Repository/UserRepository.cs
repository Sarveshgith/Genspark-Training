using Npgsql;
using NotificationApp.Data;
using NotificationApp.Interfaces;
using NotificationApp.Models;

namespace NotificationApp.Repository;

internal class UserRepository : IUserRepository
{
    public User Create(User item)
    {
        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();

        string query = @"
            INSERT INTO users (name, email, phone_no)
            VALUES (@name, @email, @phone_no)
            RETURNING id;
        ";

        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@email", item.Email);
        cmd.Parameters.AddWithValue("@name", item.Name);
        cmd.Parameters.AddWithValue("@phone_no", item.PhoneNo);
        item.Id = Convert.ToInt32(cmd.ExecuteScalar());

        return item;
    }

    public User? Get(int id)
    {
        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();

        const string query = "SELECT id, name, email, phone_no FROM users WHERE id = @id";
        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new User
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Email = reader.GetString(2),
            PhoneNo = reader.GetString(3)
        };
    }

    public User? GetByEmail(string email)
    {
        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();

        const string query = "SELECT id, name, email, phone_no FROM users WHERE email = @email";
        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@email", email);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new User
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Email = reader.GetString(2),
            PhoneNo = reader.GetString(3)
        };
    }

    public User? GetByPhone(string phoneNo)
    {
        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();

        const string query = "SELECT id, name, email, phone_no FROM users WHERE phone_no = @phone_no";
        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@phone_no", phoneNo);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new User
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Email = reader.GetString(2),
            PhoneNo = reader.GetString(3)
        };
    }

    public List<User> GetAll()
    {
        var users = new List<User>();

        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();

        const string query = "SELECT id, name, email, phone_no FROM users";
        using var cmd = new NpgsqlCommand(query, connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            users.Add(new User
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Email = reader.GetString(2),
                PhoneNo = reader.GetString(3)
            });
        }

        return users.OrderBy(u => u.Name).ThenBy(u => u.Email).ToList();
    }

    public User? Update(int id, User item)
    {
        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();

        const string query = @"
            UPDATE users
            SET name = @name,
                email = @email,
                phone_no = @phone_no,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @id;
        ";

        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", item.Name);
        cmd.Parameters.AddWithValue("@email", item.Email);
        cmd.Parameters.AddWithValue("@phone_no", item.PhoneNo);

        var affected = cmd.ExecuteNonQuery();
        if (affected == 0)
        {
            return null;
        }

        item.Id = id;
        return item;
    }

    public User? Delete(int id)
    {
        var existing = Get(id);
        if (existing == null)
        {
            return null;
        }

        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();

        const string query = "DELETE FROM users WHERE id = @id";
        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();

        return existing;
    }
}