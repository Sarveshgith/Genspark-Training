using System;
using GuessGameApp.Data;
using GuessGameApp.Interfaces;
using GuessGameApp.Models;
using Npgsql;

namespace GuessGameApp.Repositories;

public class UserRepository : IUserRepository
{
    public void RegisterUser(User user)
    {
        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();

        string insertUserQuery = @"
            INSERT INTO users (username, password)
            VALUES (@username, @password);
        ";

        using var insertUserCommand = new NpgsqlCommand(insertUserQuery, connection);
        insertUserCommand.Parameters.AddWithValue("@username", user.username);
        insertUserCommand.Parameters.AddWithValue("@password", user.password);
        insertUserCommand.ExecuteNonQuery();
    }

    public User? LoginUser(string username, string password)
    {
        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();

        string selectUserQuery = @"
            SELECT id, username, password from users 
            WHERE username = @username AND password = @password";

            using var selectUserCommand = new NpgsqlCommand(selectUserQuery, connection);
            selectUserCommand.Parameters.AddWithValue("@username", username);
            selectUserCommand.Parameters.AddWithValue("@password", password);

            using var reader = selectUserCommand.ExecuteReader();

        if (reader.Read())
        {
            return new User
            {
                id = reader.GetInt32(0),
                username = reader.GetString(1),
                password = reader.GetString(2)
            };
        }

        return null;
    }
}
