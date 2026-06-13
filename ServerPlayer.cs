using System.Collections.Concurrent;
using Blaze3SDK.Blaze;
using Blaze3SDK.Blaze.Authentication;
using Blaze3SDK.Blaze.GameManager;
using BlazeCommon;

namespace Zamboni14Legacy;

public class ServerPlayer
{
    public ServerPlayer(BlazeServerConnection blazeServerConnection, UserIdentification userIdentification, SessionInfo sessionInfo)
    {
        BlazeServerConnection = blazeServerConnection;
        UserIdentification = userIdentification;
        ExtendedData = new UserSessionExtendedData();
        SessionInfo = sessionInfo;
        // UserSettings.TryAdd("FirstTimeFlag", "0");
        // UserSettings.TryAdd("MUPSET", File.ReadAllText("filename.txt"));
        ServerManager.AddServerPlayer(userIdentification.mAccountId, this);
    }

    public BlazeServerConnection BlazeServerConnection { get; }
    public UserIdentification UserIdentification { get; set; }
    public UserSessionExtendedData ExtendedData { get; set; }
    public SessionInfo SessionInfo { get; set; }
    public uint LastPingedTime { get; set; }
    public ConcurrentDictionary<string, string> UserSettings { get; set; } = new();

    public ReplicatedGamePlayer ToReplicatedGamePlayer(byte slot, ulong gameId, NetworkAddress address = null)
    {
        return new ReplicatedGamePlayer
        {
            mAccountLocale = 1701729619,
            mCustomData = UserIdentification.mExternalBlob,
            mExternalId = UserIdentification.mExternalId,
            mGameId = gameId,
            mJoinedGameTimestamp = Util.TimeNow(),
            mNetworkAddress = address == null ? ExtendedData.mAddress : address,
            // mNetworkAddress = new NetworkAddress
            // {
            //     IpAddress = new IpAddress
            //     {
            //         mIp = ExtendedData.mAddress.IpPairAddress.Value.mInternalAddress.mIp,
            //         mPort = ExtendedData.mAddress.IpPairAddress.Value.mInternalAddress.mPort
            //     },
            // },
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
    }
}