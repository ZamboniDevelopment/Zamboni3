using Zamboni3.EashlStats.Titles;

namespace Zamboni3.EashlStats;

public sealed class StatAggregate
{
    public string Sql { get; }
    public bool SelfContained { get; }

    private StatAggregate(string sql, bool selfContained = false)
    {
        Sql = sql;
        SelfContained = selfContained;
    }

    public static StatAggregate Count()
    {
        return new StatAggregate("COUNT(*)");
    }

    private static string SumOne(string c)
    {
        return $"COALESCE(SUM({c}), 0)";
    }

    public static StatAggregate Const(long value)
    {
        return new StatAggregate($"CAST(COALESCE(MAX({value}), {value}) AS BIGINT)");
    }

    public static StatAggregate Sum(string column)
    {
        return new StatAggregate($"CAST({SumOne(column)} AS BIGINT)");
    }

    public static StatAggregate SumOf(params string[] columns)
    {
        return new StatAggregate($"CAST({string.Join(" + ", columns.Select(SumOne))} AS BIGINT)");
    }

    public static StatAggregate AvgLast(string column, int window, Position position)
    {
        string filter = $"user_id = @uid AND {StatFetcher.PostFilter(position)} AND {StatFetcher.CompletedOtpGame} AND {StatFetcher.IsOtpClubGame}";
        return new StatAggregate($"CAST(COALESCE(ROUND((SELECT AVG({column}) FROM (SELECT {column} FROM reports_otp WHERE {filter} ORDER BY created_at DESC LIMIT {window}) w)), 0) AS BIGINT)", true);
    }
}