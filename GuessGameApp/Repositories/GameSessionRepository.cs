using Npgsql;
using GuessGameApp.Data;
using GuessGameApp.Interfaces;
using GuessGameApp.Models;

namespace GuessGameApp.Repositories;

public class GameSessionRepository : IGameSessionRepository
{
    public void SaveGameSession(Game gameSession)
    {
        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();

        string insertGameSessionQuery = @"
            INSERT INTO games (user_id, difficulty_lvl, is_won, attempts, score, created_at)
            VALUES (@user_id, @difficulty_lvl, @is_won, @attempts, @score, @created_at);
        ";

        using var insertGameSessionCommand = new NpgsqlCommand(insertGameSessionQuery, connection);
        insertGameSessionCommand.Parameters.AddWithValue("@user_id", gameSession.userId);
        insertGameSessionCommand.Parameters.AddWithValue("@difficulty_lvl", gameSession.difficultyLvl);
        insertGameSessionCommand.Parameters.AddWithValue("@is_won", gameSession.isWin);
        insertGameSessionCommand.Parameters.AddWithValue("@attempts", gameSession.attempts);
        insertGameSessionCommand.Parameters.AddWithValue("@score", gameSession.score);
        insertGameSessionCommand.Parameters.AddWithValue("@created_at", gameSession.createdAt);

        insertGameSessionCommand.ExecuteNonQuery();

        // Upsert player_stats: create initial row or update aggregates
        string existsQuery = "SELECT COUNT(*) FROM player_stats WHERE user_id = @user_id";
        using var existsCmd = new NpgsqlCommand(existsQuery, connection);
        existsCmd.Parameters.AddWithValue("@user_id", gameSession.userId);
        int exists = Convert.ToInt32(existsCmd.ExecuteScalar());

        int winInc = gameSession.isWin ? 1 : 0;
        var now = DateTime.Now;

        if (exists == 0)
        {
            string insertStats = @"INSERT INTO player_stats (user_id, total_games, total_wins, best_score, total_score, updated_at)
                                    VALUES (@user_id, 1, @win_inc, @score, @score, @now)";

            using var insStatsCmd = new NpgsqlCommand(insertStats, connection);
            insStatsCmd.Parameters.AddWithValue("@user_id", gameSession.userId);
            insStatsCmd.Parameters.AddWithValue("@win_inc", winInc);
            insStatsCmd.Parameters.AddWithValue("@score", gameSession.score);
            insStatsCmd.Parameters.AddWithValue("@now", now);
            insStatsCmd.ExecuteNonQuery();
        }
        else
        {
            string updateStats = @"UPDATE player_stats SET
                                        total_games = total_games + 1,
                                        total_wins = total_wins + @win_inc,
                                        best_score = GREATEST(best_score, @score),
                                        total_score = total_score + @score,
                                        updated_at = @now
                                    WHERE user_id = @user_id";

            using var updCmd = new NpgsqlCommand(updateStats, connection);
            updCmd.Parameters.AddWithValue("@win_inc", winInc);
            updCmd.Parameters.AddWithValue("@score", gameSession.score);
            updCmd.Parameters.AddWithValue("@now", now);
            updCmd.Parameters.AddWithValue("@user_id", gameSession.userId);
            updCmd.ExecuteNonQuery();
        }
    }
}
