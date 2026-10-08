using System.Text;
using NLog;
using Npgsql;
using Zamboni3.EashlStats.Titles;

namespace Zamboni3.EashlStats;

public class StatFetcher
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static async Task<long[]> GetStats(long? userId, Position position, Dictionary<int, (string StatName, StatAggregate Aggregate)> statGroup, int width)
    {
        var entries = statGroup.ToArray();
        string selectList = string.Join(", ", entries.Select(e => e.Value.Aggregate.Sql));
        Logger.Debug($"GetStats called with: {userId}, {(int)position}");

        var sql = new StringBuilder($"SELECT {selectList} FROM reports_otp WHERE 1=1");

        if (userId != null) sql.Append(" AND user_id = @uid");

        const string completedGame = "(COALESCE(swin,0) + COALESCE(slos,0) + COALESCE(gwin,0) + COALESCE(glos,0) + COALESCE(got,0)) >= 1";
        const string isOtpClubGame = "post IS NOT NULL";

        sql.Append($" AND {isOtpClubGame}");
        sql.Append($" AND {completedGame}");

        switch (position)
        {
            case Position.Goalie:
            case Position.Defense:
            case Position.LeftWing:
            case Position.Center:
            case Position.RightWing:
                sql.Append($" AND post = {(int)position}");
                break;
            case Position.AllSkaters:
                sql.Append(" AND post <> 0");
                break;
            case Position.Something:
            case Position.Unused:
                sql.Append(" AND 1=2");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(position), position, null);
        }

        await using var conn = new NpgsqlConnection(Database.ConnectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql.ToString(), conn);
        if (userId != null) cmd.Parameters.AddWithValue("uid", userId.Value);

        var result = new long[width];
        Array.Fill(result, 67);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return result;

        for (int i = 0; i < entries.Length; i++)
            result[entries[i].Key] = reader.GetInt64(i);
        return result;
    }
}