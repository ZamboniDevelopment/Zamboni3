using System.Runtime.CompilerServices;
using Blaze3SDK.Blaze.Stats;
using Blaze3SDK.Components;
using BlazeCommon;
using NLog;
using Zamboni3.EashlStats;
using Zamboni3.EashlStats.Titles;

namespace Zamboni3.Components.Blaze;

internal class StatsComponent : StatsComponentBase.Server
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly ITitle _title = Nhl16.Instance;

    public override Task<KeyScopes> GetKeyScopesMapAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new KeyScopes
        {
            mKeyScopesMap = new SortedDictionary<string, KeyScopeItem>
            {
                ["pos"] = new KeyScopeItem
                {
                    mAggregateKeyValue = 0,
                    mEnableAggregation = false,
                    mKeyScopeValues = new SortedDictionary<long, long> { { 0, 10 } }
                }
            }
        });
    }

    public override Task<StatGroupList> GetStatGroupListAsync(NullStruct request, BlazeRpcContext context)
    {
        var groups = new List<StatGroupSummary>();
        foreach (var name in _title.StatGroups.Keys)
        {
            groups.Add(new StatGroupSummary
            {
                mName = name,
                mDesc = name,
                mMetadata = name,
                mKeyScopeNameValueMap = new SortedDictionary<string, long>()
                {
                    {
                        "pos", -1
                    }
                }
            });
        }

        return Task.FromResult(new StatGroupList
        {
            mGroups = groups
        });
    }


    public override Task<StatGroupResponse> GetStatGroupAsync(GetStatGroupRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new StatGroupResponse
        {
            mName = request.mName,
            mDesc = request.mName,
            mCategoryName = request.mName,
            mKeyScopeNameValueMap = new SortedDictionary<string, long>()
            {
                {
                    "pos", -1
                }
            },
            mMetadata = request.mName,
            mStatDescs = _title.Descriptions[request.mName]
        });
    }

    private static readonly ConditionalWeakTable<BlazeServerConnection, Dictionary<string, int>> NextPos = new();

    private int GetNextPos(BlazeServerConnection connection, string statGroup)
    {
        var posOrder = _title.StatGroups[statGroup].PosOrder;
        if (posOrder == null) return -1;
        var counters = NextPos.GetOrCreateValue(connection)!;
        counters.TryGetValue(statGroup, out int ordinal);
        counters[statGroup] = (ordinal + 1) % posOrder.Length;
        return posOrder[ordinal];
    }

    public override async Task<NullStruct> GetStatsByGroupAsyncAsync(GetStatsByGroupRequest request, BlazeRpcContext context)
    {
        if (request.mKeyScopeNameValueMap is { Count: > 0 })
        {
            throw new NotImplementedException();
        }

        if (_title.StatGroups.TryGetValue(request.mGroupName, out var group))
        {
            int p = GetNextPos(context.BlazeConnection, request.mGroupName);

            var entityStatsList = new List<EntityStats>();

            var stats = await StatFetcher.GetStats(request.mEntityIds[0], (Position)p, group.StatGroup, group.Width);

            entityStatsList.Add(new EntityStats
            {
                mEntityId = request.mEntityIds[0],
                mPeriodOffset = request.mPeriodOffset,
                mStatValues = stats.ToList().ConvertAll(x => x.ToString()),
            });

            NotifyGetStatsAsyncNotificationAsync(context.BlazeConnection, new KeyScopedStatValues
            {
                mGroupName = request.mGroupName,
                mKeyString = p == -1 ? "No_Scope_Defined" : "pos=" + p,
                mLast = true,
                mStatValues = new StatValues
                {
                    mEntityStatsList = entityStatsList
                },
                mViewId = request.mViewId
            });
        }

        return new NullStruct();
    }
}