using Blaze3SDK.Blaze.GameReportingLegacy;
using Blaze3SDK.Components;
using BlazeCommon;

namespace Zamboni3.Components.Blaze;

internal class GameReportingLegacyComponent : GameReportingLegacyComponentBase.Server
{
    private static readonly SemaphoreSlim ReportInsertLock = new(1, 1);

    public override async Task<NullStruct> SubmitGameReportAsync(GameReport request, BlazeRpcContext context)
    {
        var reporterUserId = ServerManager.GetServerPlayerByConnectionId(context.Connection.ID)!.UserIdentification.mAccountId;

        if (Program.Database.isEnabled)
        {
            await ReportInsertLock.WaitAsync();
            try
            {
                await Program.Database.InsertLegacyReport(request, reporterUserId);
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
            mGameReportingId = request.mGameReportingId
        }, true);
        return new NullStruct();
    }
}