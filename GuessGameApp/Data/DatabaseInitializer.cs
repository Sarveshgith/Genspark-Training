using Npgsql;
using GuessGameApp.Data;

namespace GuessGameApp.Data;

public class DatabaseInitializer
{
    public static void Initialize()
    {
        using var connection = DbConnectionFactory.CreateConnection();

        connection.Open();

        string createUserTableQuery = @"
            CREATE TABLE IF NOT EXISTS users (
                id SERIAL PRIMARY KEY,
                username VARCHAR(255) NOT NULL UNIQUE,
                password VARCHAR(255) NOT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
        ";

        using var createUserTableCommand = new NpgsqlCommand(createUserTableQuery, connection);
        createUserTableCommand.ExecuteNonQuery();

        string createGameTableQuery = @"
            CREATE TABLE IF NOT EXISTS games (
                id SERIAL PRIMARY KEY,
                user_id INTEGER NOT NULL,
                difficulty_lvl VARCHAR(50) NOT NULL,
                attempts INTEGER NOT NULL,
                is_won BOOLEAN NOT NULL,
                score INTEGER NOT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (user_id) REFERENCES users(id)
            );
        ";

        using var createGameTableCommand = new NpgsqlCommand(createGameTableQuery, connection);
        createGameTableCommand.ExecuteNonQuery();

        string createPlayerStatsQuery = @"
            CREATE TABLE IF NOT EXISTS player_stats (
                user_id INTEGER PRIMARY KEY,
                total_games INTEGER NOT NULL DEFAULT 0,
                total_wins INTEGER NOT NULL DEFAULT 0,
                best_score INTEGER NOT NULL DEFAULT 0,
                total_score INTEGER NOT NULL DEFAULT 0,
                updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (user_id) REFERENCES users(id)
            );
        ";

        using var createPlayerStatsCommand = new NpgsqlCommand(createPlayerStatsQuery, connection);
        createPlayerStatsCommand.ExecuteNonQuery();

        connection.Close();
    }
}
