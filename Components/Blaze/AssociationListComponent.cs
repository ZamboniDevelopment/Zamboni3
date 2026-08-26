using Blaze3SDK.Blaze.Association;
using Blaze3SDK.Components;
using BlazeCommon;

namespace Zamboni3.Components.Blaze;

internal class AssociationListsComponent : AssociationListsComponentBase.Server
{
    public override Task<Lists> GetListsAsync(GetListsRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new Lists());
    }
}