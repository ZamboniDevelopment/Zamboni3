using Blaze3SDK.Blaze.Redirector;
using Blaze3SDK.Components;
using BlazeCommon;

namespace Zamboni14Legacy.Components.Blaze;

internal class RedirectorComponent : RedirectorComponentBase.Server
{
    public override Task<ServerInstanceInfo> GetServerInstanceAsync(ServerInstanceRequest request, BlazeRpcContext context)
    {
        var responseData = new ServerInstanceInfo
        {
            mAddress = new ServerAddress
            {
                IpAddress = new IpAddress
                {
                    mHostname = Program.GameServerIp,
                    mIp = Util.GetIPAddressAsUInt(Program.GameServerIp),
                    mPort = Program.ZamboniConfig.CoreServerPort
                }
            },
            mSecure = false,
            mDefaultDnsAddress = 0
        };

        return Task.FromResult(responseData);
    }
}