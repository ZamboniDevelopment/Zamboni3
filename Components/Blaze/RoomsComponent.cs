using Blaze3SDK.Blaze.Rooms;
using Blaze3SDK.Components;
using BlazeCommon;

namespace Zamboni3.Components.Blaze;

internal class RoomsComponent : RoomsComponentBase.Server
{
    public override Task<NullStruct> SelectViewUpdatesAsync(SelectViewUpdatesRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> SelectCategoryUpdatesAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> ToggleJoinedRoomNotificationsAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }
}