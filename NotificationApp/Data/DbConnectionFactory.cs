using Npgsql;

namespace NotificationApp.Data;

internal static class DbConnectionFactory
{
    private static readonly string connectionString =
        "Host=localhost;Username=sarvesh;Password=Sarvesh_dev@1;Database=notif_app";

    public static NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(connectionString);
    }
}
