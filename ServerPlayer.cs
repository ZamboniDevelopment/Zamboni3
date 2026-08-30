using Blaze3SDK.Blaze;
using Blaze3SDK.Blaze.Authentication;
using Blaze3SDK.Blaze.GameManager;
using BlazeCommon;

namespace Zamboni3;

public class ServerPlayer
{
    public ServerPlayer(BlazeServerConnection blazeServerConnection, UserIdentification userIdentification, SessionInfo sessionInfo)
    {
        BlazeServerConnection = blazeServerConnection;
        UserIdentification = userIdentification;
        ExtendedData = new UserSessionExtendedData();
        SessionInfo = sessionInfo;
        ServerManager.AddServerPlayer(userIdentification.mAccountId, this);
    }

    public BlazeServerConnection BlazeServerConnection { get; }
    public UserIdentification UserIdentification { get; set; }
    public UserSessionExtendedData ExtendedData { get; set; }
    public SessionInfo SessionInfo { get; set; }
    public uint LastPingedTime { get; set; }

    public ReplicatedGamePlayer ToReplicatedGamePlayer(byte slot, ulong gameId, bool hideAddress, NetworkAddress address = null)
    {
        var replicatedGamePlayer = new ReplicatedGamePlayer
        {
            mAccountLocale = 1701729619,
            mCustomData = UserIdentification.mExternalBlob,
            mExternalId = UserIdentification.mExternalId,
            mGameId = gameId,
            mJoinedGameTimestamp = Util.TimeNow(),
            mPlayerAttribs = new SortedDictionary<string, string>(),
            mPlayerId = UserIdentification.mBlazeId,
            mPlayerName = UserIdentification.mName,
            mPlayerSessionId = (uint)UserIdentification.mBlazeId,
            mPlayerState = PlayerState.ACTIVE_CONNECTING,
            mSlotId = slot,
            mSlotType = SlotType.SLOT_PRIVATE,
            mTeamIndex = slot,
            mUserGroupId = default
        };
        if (hideAddress) return replicatedGamePlayer;
        replicatedGamePlayer.mNetworkAddress = address ?? ExtendedData.mAddress;
        return replicatedGamePlayer;
    }
}