using Blaze3SDK;
using Blaze3SDK.Blaze;
using Blaze3SDK.Blaze.Authentication;
using Blaze3SDK.Blaze.Util;
using Blaze3SDK.Components;
using BlazeCommon;
using SceNetNp;
using Tdf;

namespace Zamboni14Legacy.Components.Blaze;

internal class AuthenticationComponent : AuthenticationComponentBase.Server
{
    public override Task<ConsoleLoginResponse> Ps3LoginAsync(PS3LoginRequest request, BlazeRpcContext context)
    {
        if (!NpTicket.TryParse(request.mPS3Ticket, out NpTicket? ticket))
        {
            throw new BlazeRpcException(Blaze3RpcError.AUTH_ERR_INVALID_PS3_TICKET);
        }

        //Still unsure what EXBB is. Research concluded its
        //`externalblob` binary(36) DEFAULT NULL COMMENT 'sizeof(SceNpId)==36',
        //"SceNpId", Its 36 bytes long, it starts with PSN Username and suffixed with other data in the end
        //This taken straight from https://github.com/hallofmeat/Skateboard3Server/blob/master/src/Skateboard3Server.Blaze/Handlers/Authentication/LoginHandler.cs
        // var externalBlob = new List<byte>();
        // externalBlob.AddRange(Encoding.ASCII.GetBytes(ticket.OnlineId.PadRight(20, '\0')));
        // externalBlob.AddRange(Encoding.ASCII.GetBytes(ticket.Domain));
        // externalBlob.AddRange(Encoding.ASCII.GetBytes(ticket.Region));
        // externalBlob.AddRange(Encoding.ASCII.GetBytes("ps3"));
        // externalBlob.Add(0x0);
        // externalBlob.Add(0x1);
        // externalBlob.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

        var userIdentification = new UserIdentification
        {
            mAccountId = (long)ticket.SubjectId,
            mAccountLocale = 1701729619,
            mBlazeId = (long)ticket.SubjectId,
            // mExternalBlob = externalBlob.ToArray(),
            mExternalId = ticket.SubjectId,
            mName = ticket.SubjectHandle,
            mORIG = ticket.SubjectId
        };

        var sessionInfo = new SessionInfo
        {
            mBlazeUserId = (long)ticket.SubjectId,
            mIsFirstLogin = false,
            mSessionKey = "does-client-even-need-this",
            mLastLoginDateTime = 10,
            mEmail = "",
            mPersonaDetails = new PersonaDetails
            {
                mDisplayName = ticket.SubjectHandle,
                mLastAuthenticated = 0,
                mPersonaId = (long)ticket.SubjectId,
                mPlatform = ExternalSystemId.PS3,
                mStatus = PersonaStatus.ACTIVE,
                mExtId = ticket.SubjectId,
                mExternalSystemId = ExternalSystemId.PS3
            },
            mUserId = (long)ticket.SubjectId
        };

        // new ServerPlayer(context.BlazeConnection, userIdentification, extendedData, sessionInfo);
        new ServerPlayer(context.BlazeConnection, userIdentification, sessionInfo);
        
        return Task.FromResult(new ConsoleLoginResponse
        {
            mCanAgeUp = false,
            mANON = false,
            mNeedsLegalDoc = false,
            mSessionInfo = sessionInfo,
            mIsOfLegalContactAge = true,
            mIsUnderAge = false
        });
    }


    public override Task<AccountInfo> GetAccountAsync(NullStruct request, BlazeRpcContext context)
    {
        var serverPlayer = ServerManager.GetServerPlayerByConnectionId(context.Connection.ID);

        return Task.FromResult(new AccountInfo
        {
            mAnonymousUser = false,
            mAuthenticationSource = "303107",
            mCountry = "idc",
            mDOB = "dob",
            mDateCreated = "67.67.67",
            mEmail = "maili",
            mEmailStatus = EmailStatus.VERIFIED,
            mGlobalOptin = 1,
            mLanguage = "language",
            mLastAuth = "10",
            mParentalEmail = "parentrs",
            mReasonCode = StatusReason.NONE,
            mStatus = AccountStatus.ACTIVE,
            mThirdPartyOptin = 1,
            mTosVersion = "tosverison",
            mUnderageUser = false,
            mUserId = serverPlayer.UserIdentification.mAccountId
        });
    }

    public override Task<NullStruct> HasEntitlementAsync(HasEntitlementRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }
    
    public override Task<NullStruct> LogoutAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }
    
    public override Task<GetTosInfoResponse> GetTosInfoAsync(GetTosInfoRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new GetTosInfoResponse
        {
            mEaMayContact = 0,
            mPartnersMayContact = 0,
            mPrivacyPolicyUri = "",
            mTosHost = "",
            mTosUri = ""
        });
    }

    public override Task<Entitlements> ListUserEntitlements2Async(ListUserEntitlements2Request request, BlazeRpcContext context)
    {
        return Task.FromResult(new Entitlements
        {
            mEntitlements = new List<Entitlement>()
        });
    }
}