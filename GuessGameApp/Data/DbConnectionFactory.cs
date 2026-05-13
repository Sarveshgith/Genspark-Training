using Npgsql;

namespace GuessGameApp.Data;

public class DbConnectionFactory
{
    private static readonly string connectionString = "Host=localhost;Username=sarvesh;Password=Sarvesh_dev@1;Database=guessgame";
    public static NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(connectionString);
    }
}
