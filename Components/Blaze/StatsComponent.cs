using Blaze3SDK.Blaze.Stats;
using Blaze3SDK.Components;
using BlazeCommon;

namespace Zamboni3.Components.Blaze;

internal class StatsComponent : StatsComponentBase.Server
{
    public override Task<KeyScopes> GetKeyScopesMapAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new KeyScopes());
    }

    public override Task<StatGroupList> GetStatGroupListAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new StatGroupList());
    }
}