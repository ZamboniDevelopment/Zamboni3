using ZamboniUltimateTeam;

namespace Zamboni3;

public class ServerProviderBridge : IServerProvider
{
    public long GetUserIdByConnectionId(long connectionId)
    {
        return ServerManager.GetServerPlayerByConnectionId(connectionId)!.UserIdentification.mAccountId;
    }

    public string GetUserNameByUserId(long userId)
    {
        return ServerManager.GetServerPlayerByUserId(userId)!.UserIdentification.mName;
    }
}