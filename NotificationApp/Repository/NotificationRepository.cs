using Npgsql;
using NotificationApp.Data;
using NotificationApp.Interfaces;
using NotificationApp.Models;

namespace NotificationApp.Repository;

internal class NotificationRepository : INotificationRepository
{
    public Notification Create(Notification item)
    {
        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();

        const string query = @"
            INSERT INTO notifications (user_id, message, notif_type, sent_date)
            VALUES (@user_id, @message, @notif_type, @sent_date);
        ";

        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@user_id", item.UserId);
        cmd.Parameters.AddWithValue("@message", item.Message);
        cmd.Parameters.AddWithValue("@notif_type", item.NotifType);
        cmd.Parameters.AddWithValue("@sent_date", item.SentDate);

        cmd.ExecuteNonQuery();
        return item;
    }

    public Notification? Get(DateTime sentDate)
    {
        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();

        const string query = @"
            SELECT user_id, message, notif_type, sent_date
            FROM notifications
            WHERE sent_date = @sent_date
            LIMIT 1;
        ";

        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@sent_date", sentDate);
        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        return Map(reader);
    }

    public List<Notification> GetAll()
    {
        var result = new List<Notification>();

        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();

        const string query = @"
            SELECT user_id, message, notif_type, sent_date
            FROM notifications
            ORDER BY sent_date DESC;
        ";

        using var cmd = new NpgsqlCommand(query, connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            result.Add(Map(reader));
        }

        return result;
    }

    public List<Notification> GetByUserId(int userId)
    {
        var result = new List<Notification>();

        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();

        const string query = @"
            SELECT user_id, message, notif_type, sent_date
            FROM notifications
            WHERE user_id = @user_id
            ORDER BY sent_date DESC;
        ";

        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@user_id", userId);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            result.Add(Map(reader));
        }

        return result;
    }

    public Notification? Update(DateTime sentDate, Notification item)
    {
        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();

        const string query = @"
            UPDATE notifications
            SET user_id = @user_id,
                message = @message,
                notif_type = @notif_type,
                sent_date = @new_sent_date
            WHERE sent_date = @sent_date;
        ";

        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@sent_date", sentDate);
        cmd.Parameters.AddWithValue("@user_id", item.UserId);
        cmd.Parameters.AddWithValue("@message", item.Message);
        cmd.Parameters.AddWithValue("@notif_type", item.NotifType);
        cmd.Parameters.AddWithValue("@new_sent_date", item.SentDate);

        var affected = cmd.ExecuteNonQuery();
        if (affected == 0)
        {
            return null;
        }

        return item;
    }

    public Notification? Delete(DateTime sentDate)
    {
        var existing = Get(sentDate);
        if (existing == null)
        {
            return null;
        }

        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();

        const string query = "DELETE FROM notifications WHERE sent_date = @sent_date";
        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@sent_date", sentDate);
        cmd.ExecuteNonQuery();

        return existing;
    }

    private static Notification Map(NpgsqlDataReader reader)
    {
        return new Notification
        {
            UserId = reader.GetInt32(0),
            Message = reader.GetString(1),
            NotifType = reader.GetString(2),
            SentDate = reader.GetDateTime(3)
        };
    }
}