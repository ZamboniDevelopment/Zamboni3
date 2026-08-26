using Blaze3SDK.Components;
using BlazeCommon;

namespace Zamboni3.Components.Blaze;

internal class CensusDataComponent : CensusDataComponentBase.Server
{
    public override Task<NullStruct> SubscribeToCensusDataAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> UnsubscribeFromCensusDataAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }
}