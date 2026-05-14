using Npgsql;

namespace NotificationApp.Data;

internal static class DatabaseInitializer
{
    public static void Initialize()
    {
        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();

        string createUsersTable = @"
            CREATE TABLE IF NOT EXISTS users (
                id SERIAL PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                email VARCHAR(255) NOT NULL UNIQUE,
                phone_no VARCHAR(20) NOT NULL UNIQUE,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
        ";

        string createNotificationsTable = @"
            CREATE TABLE IF NOT EXISTS notifications (
                user_id INTEGER NOT NULL,
                message TEXT NOT NULL,
                notif_type VARCHAR(20) NOT NULL,
                sent_date TIMESTAMP NOT NULL,
                PRIMARY KEY (user_id, sent_date),
                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
            );
        ";

        using var usersCmd = new NpgsqlCommand(createUsersTable, connection);
        usersCmd.ExecuteNonQuery();

        using var notifCmd = new NpgsqlCommand(createNotificationsTable, connection);
        notifCmd.ExecuteNonQuery();
    }
}
