using Blaze3SDK.Blaze.Messaging;
using Blaze3SDK.Components;
using BlazeCommon;

namespace Zamboni3.Components.Blaze;

internal class MessagingComponent : MessagingComponentBase.Server
{
    public override Task<FetchMessageResponse> FetchMessagesAsync(FetchMessageRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new FetchMessageResponse());
    }
}