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

        string sql;
        if (entries.All(e => e.Value.Aggregate.SelfContained))
        {
            sql = $"SELECT {selectList}";
        }
        else
        {
            var where = new StringBuilder("1=1");
            if (userId != null) where.Append(" AND user_id = @uid");
            where.Append($" AND {IsOtpClubGame}");
            where.Append($" AND {CompletedOtpGame}");
            where.Append($" AND {PostFilter(position)}");
            sql = $"SELECT {selectList} FROM reports_otp WHERE {where}";
        }

        await using var conn = new NpgsqlConnection(Database.ConnectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("uid", userId ?? 0);

        var result = new long[width];
        Array.Fill(result, 67);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return result;

        for (int i = 0; i < entries.Length; i++)
            result[entries[i].Key] = reader.GetInt64(i);
        return result;
    }

    public const string CompletedOtpGame = "(COALESCE(swin,0) + COALESCE(slos,0) + COALESCE(gwin,0) + COALESCE(glos,0) + COALESCE(got,0)) >= 1";
    public const string IsOtpClubGame = "post IS NOT NULL";

    public static string PostFilter(Position position)
    {
        switch (position)
        {
            case Position.Goalie:
            case Position.Defense:
            case Position.LeftWing:
            case Position.Center:
            case Position.RightWing:
                return $"post = {(int)position}";
            case Position.AllSkaters:
                return "post <> 0";
            case Position.Something:
            case Position.Unused:
                return "1=2";
            default:
                throw new ArgumentOutOfRangeException(nameof(position), position, null);
        }
    }
}