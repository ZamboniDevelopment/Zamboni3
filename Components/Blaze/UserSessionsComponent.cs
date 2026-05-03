using Blaze3SDK;
using Blaze3SDK.Blaze;
using Blaze3SDK.Components;
using BlazeCommon;

namespace Zamboni14Legacy.Components.Blaze;

internal class UserSessionsComponent : UserSessionsBase.Server
{
    public override Task<NullStruct> UpdateNetworkInfoAsync(NetworkInfo request, BlazeRpcContext context)
    {
        var serverPlayer = ServerManager.GetServerPlayerByConnectionId(context.Connection.ID);
        if (serverPlayer == null) return Task.FromResult(new NullStruct());

        var serverPlayerExtendedData = serverPlayer.ExtendedData;
        serverPlayerExtendedData.mAddress = request.mAddress;
        serverPlayerExtendedData.mQosData = request.mQosData;
        serverPlayerExtendedData.mBestPingSiteAlias = request.mPingSiteLatencyByAliasMap.MinBy(pair => pair.Value).Key;
        serverPlayer.ExtendedData = serverPlayerExtendedData;

        NotifyUserSessionExtendedDataUpdateAsync(context.BlazeConnection, new UserSessionExtendedDataUpdate
        {
            mExtendedData = serverPlayerExtendedData,
            mUserId = serverPlayer.UserIdentification.mBlazeId
        }, true);

        return Task.FromResult(new NullStruct());
    }

    // public override Task<NullStruct> UpdateExtendedDataAttributeAsync(NullStruct request, BlazeRpcContext context)
    // {
    //     //This was fired only once?
    //     return Task.FromResult(new NullStruct());
    // }

    public override Task<NullStruct> UpdateHardwareFlagsAsync(UpdateHardwareFlagsRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }

    public override Task<UserData> LookupUserAsync(UserIdentification request, BlazeRpcContext context)
    {
        var target = ServerManager.GetServerPlayerByName(request.mName);
        if (target == null) throw new BlazeRpcException(Blaze3RpcError.USER_ERR_USER_NOT_FOUND);
        return Task.FromResult(new UserData
        {
            mExtendedData = target.ExtendedData,
            mStatusFlags = UserDataFlags.Online,
            mUserInfo = target.UserIdentification
        });
    }

    public override Task<UserDataResponse> LookupUsersAsync(LookupUsersRequest request, BlazeRpcContext context)
    {
        var response = new UserDataResponse();
        response.mUserDataList = new List<UserData>();

        if (request.mLookupType.Equals(LookupUsersRequest.LookupType.PERSONA_NAME))
            foreach (var variable in request.mUserIdentificationList)
            {
                var serverPlayer = ServerManager.GetServerPlayerByName(variable.mName);
                if (serverPlayer != null)
                    response.mUserDataList.Add(new UserData
                    {
                        mExtendedData = serverPlayer.ExtendedData,
                        mStatusFlags = UserDataFlags.Online,
                        mUserInfo = serverPlayer.UserIdentification
                    });
            }

        return Task.FromResult(response);
    }
}