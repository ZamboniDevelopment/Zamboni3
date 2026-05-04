using Blaze3SDK.Blaze.Clubs;
using Blaze3SDK.Components;
using BlazeCommon;

namespace Zamboni14Legacy.Components.Blaze;

internal class ClubsComponent : ClubsComponentBase.Server
{
    public override Task<ClubsComponentSettings> GetClubsComponentSettingsAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new ClubsComponentSettings());
    }
    
    public override Task<GetClubsResponse> GetClubsAsync(GetClubsRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new GetClubsResponse());
    }
    
    public override Task<GetClubMembershipForUsersResponse> GetClubMembershipForUsersAsync(GetClubMembershipForUsersRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new GetClubMembershipForUsersResponse());
    }
    
}