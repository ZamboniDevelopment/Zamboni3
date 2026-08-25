using System.Net;
using System.Security.Cryptography.X509Certificates;
using Blaze3SDK;
using BlazeCommon;
using NLog;
using NLog.Layouts;
using Tdf;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Zamboni14Legacy.Components.Blaze;
using ZamboniCommonComponents;
using ZamboniCommonComponents.Structs.TdfTagged;
using ZamboniGameServerProvider;
using ZamboniUltimateTeam;
using LogLevel = NLog.LogLevel;

namespace Zamboni14Legacy;

internal class Program
{
    public const string Name = "Zamboni3 rolling";
    public const int RedirectorPort = 42127;

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static ZamboniConfig ZamboniConfig;
    public static Database Database;

    public static readonly string PublicIp = new HttpClient().GetStringAsync("https://checkip.amazonaws.com/").GetAwaiter().GetResult().Trim();
    public static string GameServerIp;

    private static ZamboniCoreServer core;

    private static async Task Main(string[] args)
    {
        InitConfig();
        StartLogger();
        InitDatabase();
        GameServerIp = ZamboniConfig.CoreServerIp.Equals("auto") ? PublicIp : ZamboniConfig.CoreServerIp;

        var tasks = new List<Task>();

        tasks.Add(Task.Run(StartCommandListener));
        tasks.Add(StartCoreServer());
        tasks.Add(new Api().StartAsync());
        if (ZamboniConfig.HostRedirectorInstance) tasks.Add(StartRedirectorServer());
        if (ZamboniConfig.StartLocalGameServerProvider)
        {
            var gameServerProvider = new GameServerProvider();
            tasks.Add(gameServerProvider.Start());
        }
        await GameServerCommunicator.ResetAllInstances([ZamboniConfig.TargetProtocol]);
        Logger.Warn(Name + " started");
        await Task.WhenAll(tasks);
    }

    private static void StartLogger()
    {
        var logLevel = LogLevel.FromString(ZamboniConfig.LogLevel);
        var layout = new SimpleLayout("[${longdate}][${callsite-filename:includeSourcePath=false}(${callsite-linenumber})][${level:uppercase=true}]: ${message:withexception=true}");
        LogManager.Setup().LoadConfiguration(builder =>
        {
            builder.ForLogger().FilterMinLevel(logLevel)
                .WriteToConsole(layout)
                .WriteToFile("logs/server-${shortdate}.log", layout);
        });
    }

    private static async Task StartRedirectorServer()
    {
        X509Certificate cert = null;
        try
        {
            var certBytes = File.ReadAllBytes(ZamboniConfig.CertPath);
            cert = new X509Certificate2(certBytes, ZamboniConfig.CertPassword);
        }
        catch (Exception e)
        {
            Logger.Warn(e);
            Logger.Warn("Certificate failed to load, check config");
            Logger.Info("https://github.com/Aim4kill/Bug_OldProtoSSL");
        }

        var redirector = Blaze3.CreateBlazeServer("RedirectorServer", new IPEndPoint(IPAddress.Any, RedirectorPort), cert);
        redirector.AddComponent<RedirectorComponent>();
        await redirector.Start(-1).ConfigureAwait(false);
    }

    private static void InitConfig()
    {
        const string configFile = "zamboni-config.yml";
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();
        var serializer = new SerializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();

        if (!File.Exists(configFile))
        {
            ZamboniConfig = new ZamboniConfig();
            var yaml = serializer.Serialize(ZamboniConfig);

            var comments = "# CoreServerIp: 'auto' = automatically detect public IP or specify a manual IP address, where CoreServer is run on\n" +
                           "# CoreServerPort: Port for CoreServer to listen on. (Redirector server lives on " + RedirectorPort + ", clients request there)\n" +
                           "# LogLevel: Valid values: Trace, Debug, Info, Warn, Error, Fatal, Off.\n" +
                           "# DatabaseConnectionString: Connection string to PostgreSQL, for saving data. (Not required)\n" +
                           "# HostRedirectorInstance: Whether this program should host a Redirector instance\n" +
                           "# ApiServerIdentifier and ApiServerPort: identifier and port where status is\n" +
                           "# ZamboniTopology: PeerHosted(Requires QoS program running), Relayed(Preferred?), (Dedicated, borked on h2h currently), used to determine topology used for h2h games. OTP Will always use Dedicated \n" +
                           "# Config: Game specific string key value pairs inputted to client \n\n";

            File.WriteAllText(configFile, comments + yaml);
            Logger.Warn("Config file created: " + configFile);
            return;
        }

        var yamlText = File.ReadAllText(configFile);
        ZamboniConfig = deserializer.Deserialize<ZamboniConfig>(yamlText);
    }

    private static void InitDatabase()
    {
        Database = new Database();
    }

    private static async Task StartCoreServer()
    {
        var tdfFactory = new TdfFactory();
        var tdfDecoder = tdfFactory.CreateDecoder(true);
        var config = new BlazeServerConfiguration("CoreServer", new IPEndPoint(IPAddress.Any, ZamboniConfig.CoreServerPort), tdfFactory.CreateEncoder(true), tdfDecoder);
        core = new ZamboniCoreServer(config);

        core.AddComponent<UtilComponent>();
        core.AddComponent<AuthenticationComponent>();
        core.AddComponent<UserSessionsComponent>();
        core.AddComponent<RoomsComponent>();
        core.AddComponent<MessagingComponent>();
        core.AddComponent<CensusDataComponent>();
        core.AddComponent<AssociationListsComponent>();
        core.AddComponent<ClubsComponent>();
        core.AddComponent<StatsComponent>();
        core.AddComponent<GameManager>();
        core.AddComponent<GameReportingComponent>();
        core.AddComponent<GameReportingLegacyComponent>();
        core.AddComponent<LeagueComponent>();

        core.AddComponent<OSDKSeasonalPlayComponent>();
        core.AddComponent<OsdkDynamicMessagingComponent>();
        core.AddComponent<OsdkWebOfferSurveyComponent>();
        core.AddComponent<OSDKSettingsComponent>();
        core.AddComponent<OsdkOnlinePassComponent>();
        core.AddComponent<OsdkTicker2Component>();

        if (Database.isEnabled)
        {
            UltimateTeam.Initialize(Database.ConnectionString, new ServerProviderBridge());
            core.AddComponent<CardHouseComponent>();
        }

        tdfFactory.RegisterTdfType(typeof(Report));
        tdfFactory.RegisterTdfType(typeof(ClubReportVersusGame));
        tdfFactory.RegisterTdfType(typeof(CustomPlayerReportVersusGame));
        tdfFactory.RegisterTdfType(typeof(ClubReportSoGame));
        tdfFactory.RegisterTdfType(typeof(CustomPlayerReportSoGame));
        tdfFactory.RegisterTdfType(typeof(ClubReportOtpGame));
        tdfFactory.RegisterTdfType(typeof(CustomPlayerReportOtpLeagueGame));

        await core.Start(-1).ConfigureAwait(false);
    }

    private static void StartCommandListener()
    {
        Logger.Info("Type 'help' or 'status'.");

        while (true)
        {
            var input = ReadLine.Read();
            if (string.IsNullOrWhiteSpace(input))
                continue;

            switch (input.Trim().ToLowerInvariant())
            {
                case "help":
                    Logger.Warn("Available commands: help, status, reload");
                    break;

                case "status":
                    Logger.Info(Name);
                    Logger.Info("Server running on ip: " + GameServerIp + " (" + PublicIp + ")");
                    Logger.Info("GameServerPort port: " + ZamboniConfig.CoreServerPort);
                    Logger.Info("Redirector port: " + RedirectorPort);
                    Logger.Info("Online Players: " + ServerManager.GetServerPlayers().Count);
                    foreach (var serverPlayer in ServerManager.GetServerPlayers().Values)
                        Logger.Info(
                            serverPlayer.UserIdentification.mName + " "
                                                                  + serverPlayer.UserIdentification.mAccountId + " "
                                                                  + serverPlayer.BlazeServerConnection.ProtoFireConnection.ID);
                    Logger.Info("Queued Total Players: " + ServerManager.GetQueuedPlayers().Count);
                    foreach (var queuedPlayer in ServerManager.GetQueuedPlayers().Values) Logger.Info(queuedPlayer.ServerPlayer.UserIdentification.mName);
                    Logger.Info("Server Games: " + ServerManager.GetServerGames().Count);
                    foreach (var serverGame in ServerManager.GetServerGames()) Logger.Info(serverGame);
                    break;

                case "reload":
                    UltimateTeam.ReloadConfigs();
                    Logger.Warn("Reloaded configs");
                    break;

                default:
                    Logger.Info($"Unknown command: {input}");
                    break;
            }
        }
    }
}