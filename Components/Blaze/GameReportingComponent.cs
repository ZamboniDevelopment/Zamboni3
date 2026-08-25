using Blaze3SDK.Blaze.GameReporting;
using Blaze3SDK.Components;
using BlazeCommon;

namespace Zamboni14Legacy.Components.Blaze;

internal class GameReportingComponent : GameReportingComponentBase.Server
{
    private static readonly SemaphoreSlim ReportInsertLock = new(1, 1);

    public override async Task<NullStruct> SubmitGameReportAsync(SubmitGameReportRequest request, BlazeRpcContext context)
    {
        var reporterUserId = ServerManager.GetServerPlayerByConnectionId(context.Connection.ID)!.UserIdentification.mAccountId;

        if (Program.Database.isEnabled)
        {
            await ReportInsertLock.WaitAsync();
            try
            {
                await Program.Database.InsertReport(request, (ulong)reporterUserId);
            }
            finally
            {
                ReportInsertLock.Release();
            }
        }

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