using Blaze3SDK.Blaze.GameReporting;
using Blaze3SDK.Components;
using BlazeCommon;

namespace Zamboni14Legacy.Components.Blaze;

internal class GameReportingComponent : GameReportingComponentBase.Server
{
    public override async Task<NullStruct> SubmitGameReportAsync(SubmitGameReportRequest request, BlazeRpcContext context)
    {
        if (Program.Database.isEnabled) await Database.InsertReport(request);
        NotifyResultNotificationAsync(context.BlazeConnection, new ResultNotification
        {
            mBlazeError = 0,
            mFinalResult = true,
            mGameHistoryId = request.mGameReport.mGameReportingId,
            mGameReportingId = request.mGameReport.mGameReportingId,
        }, true);

        return new NullStruct();
    }
}