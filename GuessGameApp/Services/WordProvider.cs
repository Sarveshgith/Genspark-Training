using System;
using GuessGameApp.Data;
using Npgsql;

namespace GuessGameApp.Services;

internal class WordProvider
{
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }

    public string GetRandomWord(Difficulty difficulty)
    {
        string difficultyStr = difficulty.ToString();

        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();

        string query = @"SELECT word FROM words WHERE difficulty = @difficulty ORDER BY RANDOM() LIMIT 1";

        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@difficulty", difficultyStr);

        var result = cmd.ExecuteScalar();
        connection.Close();

        if (result != null && result != DBNull.Value)
        {
            return result.ToString() ?? "apple";
        }

        return "apple";
    }
}