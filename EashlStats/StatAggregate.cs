namespace Zamboni3.EashlStats;

public sealed class StatAggregate
{
    public string Sql { get; }

    private StatAggregate(string sql)
    {
        Sql = sql;
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

    public static StatAggregate Avg(string column)
    {
        return new StatAggregate($"CAST(COALESCE(ROUND(AVG({column})), 0) AS BIGINT)");
    }
}