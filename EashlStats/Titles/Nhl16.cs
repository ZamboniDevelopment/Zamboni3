using Blaze3SDK.Blaze.Stats;

namespace Zamboni3.EashlStats.Titles;

public sealed class Nhl16 : ITitle
{
    public static readonly Nhl16 Instance = new();

    private static readonly (int[]? PosOrder, int Width, Dictionary<int, (string StatName, StatAggregate Aggregate)> Stats) PerformanceTracker =
    (
        PosOrder: [4, 3, 5, 1, 2, 0, 9],
        Width: 70,
        Stats: new()
        {
            [0] = ("SkaterGamesPlayed", StatAggregate.Count()), //TODO StatAggregate.SumOf("swin", "slos") once OT recording is known
            [5] = ("SkaterWins", StatAggregate.Sum("swin")),
            [6] = ("SkaterLosses", StatAggregate.Sum("slos")),
            [7] = ("Goals", StatAggregate.Sum("sgls")),
            [8] = ("Assists", StatAggregate.Sum("sass")),
            [9] = ("GameWinningGoals", StatAggregate.Sum("sgwg")),
            [10] = ("PowerplayGoals", StatAggregate.Sum("sppg")),
            [11] = ("ShorthandedGoals", StatAggregate.Sum("sshg")),
            [12] = ("PlusMinus", StatAggregate.Sum("splu")),
            [13] = ("PenaltyMinutes", StatAggregate.Sum("spim")),
            [14] = ("Offsides", StatAggregate.Sum("soff")),
            [15] = ("Fights", StatAggregate.Sum("sfgt")),
            [16] = ("FightsWon", StatAggregate.Sum("sftw")),
            [17] = ("Faceoffs", StatAggregate.Sum("sfot")),
            [18] = ("FaceoffWins", StatAggregate.Sum("sfow")),
            [19] = ("Hits", StatAggregate.Sum("shit")),
            [20] = ("BlockedShots", StatAggregate.Sum("sbs")),
            [21] = ("Shots", StatAggregate.Sum("ssht")),
            [22] = ("DekeAttempts", StatAggregate.Sum("sdkm")), // which of sdek/sdkm
            [23] = ("SuccessfulDekes", StatAggregate.Sum("sdek")), // is attempts: VERIFY in-game
            [24] = ("Turnovers", StatAggregate.Sum("sgva")),
            [25] = ("Takeaways", StatAggregate.Sum("stak")),
            [26] = ("GoodShots", StatAggregate.Sum("sscc")), // semantics unverified
            [27] = ("GoalsScreeningGoalie", StatAggregate.Sum("sscg")),
            [28] = ("GoalieGamesPlayed", StatAggregate.Count()),
            [33] = ("GoalieWins", StatAggregate.Sum("gwin")), // losing goalie had 1 once: VERIFY
            [34] = ("GoalieLosses", StatAggregate.Sum("glos")),
            [35] = ("OvertimeLosses", StatAggregate.Sum("got")),
            [36] = ("ShotsAgainst", StatAggregate.Sum("gsht")),
            [37] = ("Saves", StatAggregate.Sum("gsav")),
            [38] = ("Shutouts", StatAggregate.Sum("gso")),
            [39] = ("ShutoutPeriods", StatAggregate.Sum("gsop")),
            [40] = ("GoalieMinutes", StatAggregate.Sum("gmin")),
            [41] = ("DesperationSaves", StatAggregate.Sum("gdsv")),
            [42] = ("BreakawayShots", StatAggregate.Sum("gbsh")),
            [43] = ("BreakawaySaves", StatAggregate.Sum("gbsv")),
            [44] = ("PenaltyShots", StatAggregate.Sum("gpns")),
            [45] = ("PenaltyShotSaves", StatAggregate.Sum("gpsv")),
        }
    );

    private static readonly Dictionary<string, (int[]? PosOrder, int Width, Dictionary<int, (string StatName, StatAggregate Aggregate)> StatGroup)> Groups = new()
    {
        ["StatPerformanceTracker"] = PerformanceTracker,
        ["StatPerformanceTrackerRankedOTP"] = PerformanceTracker,
        ["StatClubRulesPlayer"] = (PosOrder: [9], Width: 70, new()
        {
            [2] = ("Rating_Skater_Overall", StatAggregate.Const(50)),
            [3] = ("Rating_Skater_Position", StatAggregate.Const(50)),
            [4] = ("Rating_Skater_TeamPlay", StatAggregate.Const(50)),
            [5] = ("Rating_Skater_Stats", StatAggregate.Const(50)),
            [8] = ("Rating_Goalie_Overall", StatAggregate.Const(50)),
            [9] = ("Rating_Goalie_Position", StatAggregate.Const(50)),
            [10] = ("Rating_Goalie_TeamPlay", StatAggregate.Const(50)),
            [11] = ("Rating_Goalie_Stats", StatAggregate.Const(50)),
            [12] = ("Rating_LW_Overall", StatAggregate.Const(50)),
            [13] = ("Rating_LW_Position", StatAggregate.Const(50)),
            [14] = ("Rating_LW_TeamPlay", StatAggregate.Const(50)),
            [15] = ("Rating_LW_Stats", StatAggregate.Const(50)),
            [16] = ("Rating_RW_Overall", StatAggregate.Const(50)),
            [17] = ("Rating_RW_Position", StatAggregate.Const(50)),
            [18] = ("Rating_RW_TeamPlay", StatAggregate.Const(50)),
            [19] = ("Rating_RW_Stats", StatAggregate.Const(50)),
            [20] = ("Rating_C_Overall", StatAggregate.Const(50)),
            [21] = ("Rating_C_Position", StatAggregate.Const(50)),
            [22] = ("Rating_C_TeamPlay", StatAggregate.Const(50)),
            [23] = ("Rating_C_Stats", StatAggregate.Const(50)),
            [24] = ("Rating_D_Overall", StatAggregate.Const(50)),
            [25] = ("Rating_D_Position", StatAggregate.Const(50)),
            [26] = ("Rating_D_TeamPlay", StatAggregate.Const(50)),
            [27] = ("Rating_D_Stats", StatAggregate.Const(50)),
        }),
        ["GlobalStats"] = (PosOrder: null, Width: 3, new()
        {
            [0] = ("Unknown", StatAggregate.Count()),
        }),
    };

    private static readonly Lazy<Dictionary<string, List<StatDescSummary>>> Descs =
        new(() => Groups.Keys.ToDictionary(g => g, BuildDescriptions));

    private static List<StatDescSummary> BuildDescriptions(string statGroup)
    {
        var stats = Groups[statGroup];
        int width = stats.Width;
        var descriptions = new List<StatDescSummary>(width);

        for (int i = 0; i < width; i++)
        {
            string name = stats.StatGroup.TryGetValue(i, out var s) ? s.StatName : "UNKNOWN_" + i;
            descriptions.Add(new StatDescSummary
            {
                mCategory = statGroup,
                mDefaultValue = "0",
                mDerived = false,
                mFormat = "%d",
                mLongDesc = name,
                mMetadata = name,
                mName = name,
                mShortDesc = name,
                mType = 1
            });
        }

        return descriptions;
    }

    public IReadOnlyDictionary<string, (int[]? PosOrder, int Width, Dictionary<int, (string StatName, StatAggregate Aggregate)> StatGroup)> StatGroups => Groups;
    public Dictionary<string, List<StatDescSummary>> Descriptions => Descs.Value;
}