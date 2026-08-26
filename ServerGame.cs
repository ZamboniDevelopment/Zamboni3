using System.Collections.Concurrent;
using System.Text;
using Blaze3SDK.Blaze;
using Blaze3SDK.Blaze.GameManager;
using Blaze3SDK.Components;
using Zamboni3.Components.Blaze;
using ZProtocol;

namespace Zamboni3;

public class ServerGame
{
    public ConcurrentDictionary<long, ServerPlayer> ServerPlayers { get; } = new();
    public ReplicatedGameData ReplicatedGameData { get; set; }
    public ConcurrentDictionary<long, ReplicatedGamePlayer> ReplicatedGamePlayers { get; set; } = new();
    private ZamboniTopology ZamboniTopology { get; set; }

    public static async Task<ServerGame> CreateAsync(ServerPlayer creator, CreateGameRequest request, ZamboniTopology zamboniTopology)
    {
        var game = new ServerGame(creator, request, zamboniTopology);
        if (zamboniTopology == ZamboniTopology.PeerHosted) return game;

        var reserveResponse = await GameServerCommunicator.ReserveInstance(creator, new ReserveInstanceCommand(new ReserveRequest(game.ReplicatedGameData.mGameId, Guid.Parse(game.ReplicatedGameData.mUUID), zamboniTopology, request.mGameProtocolVersionString, request.mSlotCapacities[0] + request.mSlotCapacities[1])));

        var updated = game.ReplicatedGameData;
        if (reserveResponse is not null)
        {
            updated.mHostNetworkAddressList = new List<NetworkAddress>
            {
                new NetworkAddress
                {
                    IpAddress = new IpAddress
                    {
                        mIp = Util.GetIPAddressAsUInt(reserveResponse.GameInstanceInfo.Host),
                        mPort = reserveResponse.GameInstanceInfo.Port
                    },
                }
            };

            if (zamboniTopology == ZamboniTopology.Dedicated)
            {
                updated.mAdminPlayerList = new List<long>
                {
                    123, creator.UserIdentification.mBlazeId
                };
                updated.mTopologyHostInfo = new HostInfo
                {
                    mPlayerId = 123,
                    mSlotId = 0
                };
                updated.mTopologyHostSessionId = 123;
                updated.mGameState = GameState.PRE_GAME;
            }
        }
        else
        {
            game.ZamboniTopology = ZamboniTopology.PeerHosted;
        }

        game.ReplicatedGameData = updated;
        return game;
    }

    public static async Task<ServerGame> CreateAsync(ServerPlayer creator, StartMatchmakingRequest request, ZamboniTopology zamboniTopology)
    {
        return await CreateAsync(creator, new CreateGameRequest
        {
            mEntryCriteriaMap = request.mEntryCriteriaMap,
            mGameAttribs = ToStringDictionary(request.mCriteriaData),
            mGameEntryType = request.mGameEntryType,
            mGameProtocolVersionString = request.mGameProtocolVersionString,
            mGameSettings = request.mGameSettings,
            mHostNetworkAddressList = new List<NetworkAddress>(),
            mIgnoreEntryCriteriaWithInvite = request.mIgnoreEntryCriteriaWithInvite,
            mJoiningSlotType = SlotType.SLOT_PUBLIC,
            mMaxPlayerCapacity = request.mMaxPlayerCapacity,
            mMeshAttribs = new SortedDictionary<string, string>(),
            mNetworkTopology = ToBlazeNetworkTopology(zamboniTopology),
            mPresenceMode = PresenceMode.PRESENCE_MODE_STANDARD,
            mQueueCapacity = 0,
            mSlotCapacities = new List<ushort>()
            {
                0, 2
            },
            mVoipNetwork = VoipTopology.VOIP_DISABLED
        }, zamboniTopology);
    }


    private static GameNetworkTopology ToBlazeNetworkTopology(ZamboniTopology zamboniTopology)
    {
        switch (zamboniTopology)
        {
            case ZamboniTopology.PeerHosted:
            case ZamboniTopology.Relayed:
                return GameNetworkTopology.CLIENT_SERVER_PEER_HOSTED;
            case ZamboniTopology.Dedicated:
                return GameNetworkTopology.CLIENT_SERVER_DEDICATED;
            default:
                throw new ArgumentOutOfRangeException(nameof(zamboniTopology), zamboniTopology, null);
        }
    }

    private ServerGame(ServerPlayer host, CreateGameRequest request, ZamboniTopology zamboniTopology)
    {
        var gameId = Program.Database.GetNextGameId();

        ReplicatedGameData = new ReplicatedGameData
        {
            mAdminPlayerList = new List<long>
            {
                host.UserIdentification.mAccountId
            },
            mEntryCriteriaMap = request.mEntryCriteriaMap,
            mGameAttribs = request.mGameAttribs,
            mGameId = gameId,
            mGameName = "game" + gameId,
            mGameProtocolVersionHash = GetGameProtocolVersionHash(request.mGameProtocolVersionString),
            mGameProtocolVersionString = request.mGameProtocolVersionString,
            mGameReportingId = gameId,
            mGameSettings = request.mGameSettings,
            mGameState = GameState.INITIALIZING,
            mGameTypeName = request.mGameTypeName,
            mHostNetworkAddressList = new List<NetworkAddress>(),
            mIgnoreEntryCriteriaWithInvite = request.mIgnoreEntryCriteriaWithInvite,
            mMaxPlayerCapacity = (ushort)(request.mSlotCapacities[0] + request.mSlotCapacities[1]),
            mMeshAttribs = request.mMeshAttribs,
            mNetworkQosData = host.ExtendedData.mQosData,
            mNetworkTopology = ToBlazeNetworkTopology(zamboniTopology),
            mPingSiteAlias = host.ExtendedData.mBestPingSiteAlias,
            mPlatformHostInfo = new HostInfo
            {
                mPlayerId = host.UserIdentification.mBlazeId,
                mSlotId = 1
            },
            mPresenceMode = request.mPresenceMode,
            mQueueCapacity = 3,
            mServerNotResetable = request.mServerNotResetable,
            mSharedSeed = (uint)gameId,
            mSlotCapacities = request.mSlotCapacities,
            mTeamCapacity = default,
            mTeamIds = request.mTeamIds,
            mTopologyHostInfo = new HostInfo
            {
                mPlayerId = host.UserIdentification.mAccountId,
                mSlotId = 1
            },
            mTopologyHostSessionId = (ulong)host.UserIdentification.mAccountId,
            mUUID = Guid.NewGuid().ToString(),
            mVoipNetwork = VoipTopology.VOIP_DISABLED,
            mXnetNonce = new byte[]
            {
            },
            mXnetSession = new byte[]
            {
            },
        };
        ZamboniTopology = zamboniTopology;
        ServerManager.AddServerGame(gameId, this);
    }

    public async Task AddGameParticipant(ServerPlayer serverPlayer, uint matchmakingSessionId = 0)
    {
        ServerPlayers.TryAdd(serverPlayer.UserIdentification.mAccountId, serverPlayer);
        ReplicatedGamePlayer replicatedGamePlayer;
        switch (ZamboniTopology)
        {
            case ZamboniTopology.PeerHosted:
                replicatedGamePlayer = serverPlayer.ToReplicatedGamePlayer((byte)ServerPlayers.Count, ReplicatedGameData.mGameId, false);
                break;
            case ZamboniTopology.Relayed:
                replicatedGamePlayer = serverPlayer.ToReplicatedGamePlayer((byte)ServerPlayers.Count, ReplicatedGameData.mGameId, false, new NetworkAddress
                {
                    IpAddress = new IpAddress
                    {
                        mIp = ReplicatedGameData.mHostNetworkAddressList[0].IpAddress.Value.mIp,
                        mPort = ReplicatedGameData.mHostNetworkAddressList[0].IpAddress.Value.mPort,
                    },
                });
                break;
            case ZamboniTopology.Dedicated:
                replicatedGamePlayer = serverPlayer.ToReplicatedGamePlayer((byte)ServerPlayers.Count, ReplicatedGameData.mGameId, true);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        ReplicatedGamePlayers.TryAdd(serverPlayer.UserIdentification.mAccountId, replicatedGamePlayer);

        if (ZamboniTopology == ZamboniTopology.Dedicated && ServerPlayers.Count == 1)
        {
            GameManagerBase.Server.NotifyGameSetupAsync(serverPlayer.BlazeServerConnection, new NotifyGameSetup
            {
                mGameData = ReplicatedGameData,
                mGameRoster = ReplicatedGamePlayers.Values.ToList(),
                mGameSetupReason = new GameSetupReason
                {
                    ResetDedicatedServerSetupContext = new ResetDedicatedServerSetupContext(),
                }
            }, true);
        }
        else
        {
            GameManagerBase.Server.NotifyGameSetupAsync(serverPlayer.BlazeServerConnection, new NotifyGameSetup
            {
                mGameData = ReplicatedGameData,
                mGameRoster = ReplicatedGamePlayers.Values.ToList(),
                mGameSetupReason = new GameSetupReason
                {
                    MatchmakingSetupContext = new MatchmakingSetupContext
                    {
                        mFitScore = 10,
                        mMatchmakingResult = MatchmakingResult.SUCCESS_CREATED_GAME,
                        mMaxPossibleFitScore = 010,
                        mSessionId = matchmakingSessionId,
                        mUserSessionId = 0
                    }
                }
            }, true);
        }

        ServerPlayers.Values.ToList().Where(par => par.UserIdentification.mAccountId != serverPlayer.UserIdentification.mAccountId).ToList().ForEach(participant => GameManagerBase.Server.NotifyPlayerJoiningAsync(participant.BlazeServerConnection, new NotifyPlayerJoining
        {
            mGameId = ReplicatedGameData.mGameId,
            mJoiningPlayer = replicatedGamePlayer
        }, true));
    }

    public bool HasSpaceForPlayer()
    {
        return ReplicatedGameData.mSlotCapacities.Sum(x => x) > ReplicatedGamePlayers.Count;
    }

    public void RemoveGameParticipant(ServerPlayer leaver, PlayerRemovedReason reason)
    {
        ServerPlayers.Values.ToList().ForEach(participant => GameManagerBase.Server.NotifyPlayerRemovedAsync(participant.BlazeServerConnection, new NotifyPlayerRemoved
        {
            mPlayerRemovedTitleContext = 0,
            mGameId = ReplicatedGameData.mGameId,
            mPlayerId = leaver.UserIdentification.mBlazeId,
            mPlayerRemovedReason = reason
        }));

        ServerPlayers.TryRemove(leaver.UserIdentification.mAccountId, out _);
        ReplicatedGamePlayers.Remove(leaver.UserIdentification.mAccountId, out _);

        if (ZamboniTopology == ZamboniTopology.Dedicated && ReplicatedGameData.mGameState == GameState.IN_GAME && !ServerPlayers.IsEmpty)
        {
            return;
        }

        if (ServerPlayers.IsEmpty || leaver.UserIdentification.mBlazeId == ReplicatedGameData.mPlatformHostInfo.mPlayerId)
        {
            RemoveGame();
        }
    }

    private void RemoveGame()
    {
        GameManager.StaleGames.Enqueue(ReplicatedGameData.mGameId);

        while (GameManager.StaleGames.Count > 20)
        {
            GameManager.StaleGames.TryDequeue(out _);
        }

        _ = GameServerCommunicator.DestroyInstance(this);

        ServerPlayers.Values.ToList().ForEach(participant => GameManagerBase.Server.NotifyGameRemovedAsync(participant.BlazeServerConnection, new NotifyGameRemoved()
        {
            mGameId = ReplicatedGameData.mGameId,
            mDestructionReason = GameDestructionReason.SYS_GAME_ENDING
        }));

        ServerPlayers.Values.ToList().ForEach(participant => GameManagerBase.Server.NotifyPlayerRemovedAsync(participant.BlazeServerConnection, new NotifyPlayerRemoved
        {
            mGameId = ReplicatedGameData.mGameId,
            mPlayerId = participant.UserIdentification.mBlazeId,
            mPlayerRemovedReason = PlayerRemovedReason.GAME_DESTROYED,
            mPlayerRemovedTitleContext = 0
        }));

        ServerManager.RemoveServerGame(ReplicatedGameData.mGameId);
    }

    private static SortedDictionary<string, string> ToStringDictionary(MatchmakingCriteriaData matchmakingCriteriaData)
    {
        SortedDictionary<string, string> returningList = new();
        foreach (var variable in matchmakingCriteriaData.mGenericRulePrefsList)
        {
            returningList.Add(variable.mRuleName, variable.mDesiredValues[0]);
        }

        return returningList;
    }

    public static ulong GetGameProtocolVersionHash(string protocolVersion)
    {
        protocolVersion ??= string.Empty;
        //FNV1 HASH - the same hashing logic is used in ea blaze for game protocol versions
        var buf = Encoding.UTF8.GetBytes(protocolVersion);
        var hash = 2166136261UL;
        foreach (var c in buf)
            hash = (hash * 16777619) ^ c;
        return hash;
    }

    public override string ToString()
    {
        return "Players: " +
               string.Join(", ", ServerPlayers.Values.Select(serverPlayer => serverPlayer.UserIdentification.mName)) +
               " gameId:" + ReplicatedGameData.mGameId +
               " state: " + ReplicatedGameData.mGameState +
               " OSDK_gameMode: " + ReplicatedGameData.mGameAttribs["OSDK_gameMode"] +
               " ZamboniTopology: " + ZamboniTopology;
    }
}