using Blaze3SDK.Blaze;
using Blaze3SDK.Blaze.Util;
using Blaze3SDK.Components;
using BlazeCommon;

namespace Zamboni14Legacy.Components.Blaze;

internal class UtilComponent : UtilComponentBase.Server
{
    public override Task<PreAuthResponse> PreAuthAsync(PreAuthRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new PreAuthResponse
        {
            mAuthenticationSource = "303107",
            mComponentIds = new List<ushort>
            {
                1, 4, 5, 7, 9, 10, 11, 12, 13, 15, 21, 25, 28, 2249, 2250, 2251, 2262, 2268, 30722
            },
            mConfig = new FetchConfigResponse
            {
                mConfig = new SortedDictionary<string, string>
                {
                    { "connIdleTimeout", "120s" },
                    { "defaultRequestTimeout", "80s" },
                    { "pingPeriod", "20s" },
                    { "voipHeadsetUpdateRate", "1000" },
                    { "xlspConnectionIdleTimeout", "300" }
                }
            },
            mEEFA = true,
            mESRC = "nhl-2016-ps3",
            mINST = "nhl-2016-ps3",
            mUnderageSupported = false,
            mPersonaNamespace = "cem_ea_id",
            mLegalDocGameIdentifier = "nhl-2016-ps3",
            mPlatform = "ps3",
            mQosSettings = new QosConfigInfo
            {
                mBandwidthPingSiteInfo = new QosPingSiteInfo
                {
                    mAddress = Program.GameServerIp,
                    mPort = 17502,
                    mSiteName = "qos"
                },
                mNumLatencyProbes = 10,
                mPingSiteInfoByAliasMap = new SortedDictionary<string, QosPingSiteInfo>
                {
                    {
                        "qos", new QosPingSiteInfo
                        {
                            mAddress = Program.GameServerIp,
                            mPort = 17502,
                            mSiteName = "qos"
                        }
                    }
                },
                mServiceId = 1161889797
            },
            mRegistrationSource = "303107",
            mServerVersion = Program.Name
        });
    }

    public override Task<PostAuthResponse> PostAuthAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new PostAuthResponse
        {
            // mPssConfig = new PssConfig
            // {
            //     mAddress = Program.GameServerIp,
            //     mInitialReportTypes = PssReportTypes.None,
            //     mNpCommSignature = new byte[]
            //     {
            //     },
            //     mOfferIds = new List<string>(),
            //     mPort = 7667,
            //     mProjectId = "",
            //     mTitleId = 0
            // },
            mTelemetryServer = GetTele(),
            mTickerServer = GetTicker(),
            mUserOptions = new UserOptions
            {
                mTelemetryOpt = TelemetryOpt.TELEMETRY_OPT_OUT,
                mUserId = 301116
            }
        });
    }

    public override Task<PingResponse> PingAsync(NullStruct request, BlazeRpcContext context)
    {
        var time = (uint)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        var serverPlayer = ServerManager.GetServerPlayerByConnectionId(context.Connection.ID);
        if (serverPlayer != null) serverPlayer.LastPingedTime = time;
        return Task.FromResult(new PingResponse
        {
            mServerTime = time
        });
    }

    public override Task<GetTelemetryServerResponse> GetTelemetryServerAsync(GetTelemetryServerRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(GetTele());
    }

    public override Task<UserSettingsLoadAllResponse> UserSettingsLoadAllAsync(UserSettingsLoadAllRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new UserSettingsLoadAllResponse
        {
            mDataMap = new SortedDictionary<string, string>()
        });
    }

    public override Task<UserSettingsResponse> UserSettingsLoadAsync(UserSettingsLoadRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new UserSettingsResponse());
    }

    private GetTelemetryServerResponse GetTele()
    {
        return new GetTelemetryServerResponse
        {
            mAddress = Program.GameServerIp,
            mDisable = "disa",
            mFilter = "filt",
            mIsAnonymous = false,
            mKey = "key",
            mLocale = 1701729619,
            mNoToggleOk = "nook",
            mPort = 6767,
            mSendDelay = 10,
            mSendPercentage = 10,
            mSessionID = "id",
            mUseServerTime = "true"
        };
    }

    private GetTickerServerResponse GetTicker()
    {
        return new GetTickerServerResponse
        {
            mAddress = Program.GameServerIp,
            mKey = "key",
            mPort = 6776
        };
    }


    public override Task<FetchConfigResponse> FetchClientConfigAsync(FetchClientConfigRequest request, BlazeRpcContext context)
    {
        if (request.mConfigSection.Equals("OSDK_ROSTER"))
            return Task.FromResult(new FetchConfigResponse
            {
                mConfig = new SortedDictionary<string, string>
                {
                    {
                        "CRC", ""
                    },
                    {
                        "URL", ""
                    }
                }
            });
        return Task.FromResult(new FetchConfigResponse
        {
            mConfig = new SortedDictionary<string, string>()
        });
    }

    public override Task<LocalizeStringsResponse> LocalizeStringsAsync(LocalizeStringsRequest request, BlazeRpcContext context)
    {
        var retList = new SortedDictionary<string, string>();
        foreach (var variable in request.mStringIds)
        {
            retList.Add(variable, variable);
        }

        return Task.FromResult(new LocalizeStringsResponse
        {
            mLocalizedStrings = retList
        });
    }
    
    public override Task<FilterUserTextResponse> FilterForProfanityAsync(FilterUserTextResponse request, BlazeRpcContext context)
    {
        var response = new List<FilteredUserText>();

        foreach (var filteredUserText in request.mFilteredTextList)
            response.Add(new FilteredUserText
            {
                mFilteredText = filteredUserText.mFilteredText,
                mResult = FilterResult.FILTER_RESULT_PASSED
            });

        return Task.FromResult(new FilterUserTextResponse
        {
            mFilteredTextList = response
        });
    }
}