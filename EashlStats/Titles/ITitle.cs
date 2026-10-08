using Blaze3SDK.Blaze.Stats;

namespace Zamboni3.EashlStats.Titles;

public interface ITitle
{
    IReadOnlyDictionary<string, (int[]? PosOrder, int Width, Dictionary<int, (string StatName, StatAggregate Aggregate)> StatGroup)> StatGroups { get; }
 
    Dictionary<string, List<StatDescSummary>> Descriptions { get; }
}