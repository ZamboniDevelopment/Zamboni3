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
            [0] = ("SkaterGamesPlayed", StatAggregate.Const(0)), //TODO StatAggregate.SumOf("swin", "slos") once OT recording is known
            [5] = ("SkaterWins", StatAggregate.Const(0)),
            [6] = ("SkaterLosses", StatAggregate.Const(0)),
            [7] = ("Goals", StatAggregate.Const(0)),
            [8] = ("Assists", StatAggregate.Const(0)),
            [9] = ("GameWinningGoals", StatAggregate.Const(0)),
            [10] = ("PowerplayGoals", StatAggregate.Const(0)),
            [11] = ("ShorthandedGoals", StatAggregate.Const(0)),
            [12] = ("PlusMinus", StatAggregate.Const(0)),
            [13] = ("PenaltyMinutes", StatAggregate.Const(0)),
            [14] = ("Offsides", StatAggregate.Const(0)),
            [15] = ("Fights", StatAggregate.Const(0)),
            [16] = ("FightsWon", StatAggregate.Const(0)),
            [17] = ("Faceoffs", StatAggregate.Const(0)),
            [18] = ("FaceoffWins", StatAggregate.Const(0)),
            [19] = ("Hits", StatAggregate.Const(0)),
            [20] = ("BlockedShots", StatAggregate.Const(0)),
            [21] = ("Shots", StatAggregate.Const(0)),
            [22] = ("DekeAttempts", StatAggregate.Const(0)),
            [23] = ("SuccessfulDekes", StatAggregate.Const(0)),
            [24] = ("Turnovers", StatAggregate.Const(0)),
            [25] = ("Takeaways", StatAggregate.Const(0)),
            [26] = ("GoodShots", StatAggregate.Const(0)),
            [27] = ("GoalsScreeningGoalie", StatAggregate.Const(0)),
            [28] = ("GoalieGamesPlayed", StatAggregate.Const(0)),
            [33] = ("GoalieWins", StatAggregate.Const(0)),
            [34] = ("GoalieLosses", StatAggregate.Const(0)),
            [35] = ("OvertimeLosses", StatAggregate.Const(0)),
            [36] = ("ShotsAgainst", StatAggregate.Const(0)),
            [37] = ("Saves", StatAggregate.Const(0)),
            [38] = ("Shutouts", StatAggregate.Const(0)),
            [39] = ("ShutoutPeriods", StatAggregate.Const(0)),
            [40] = ("GoalieMinutes", StatAggregate.Const(0)),
            [41] = ("DesperationSaves", StatAggregate.Const(0)),
            [42] = ("BreakawayShots", StatAggregate.Const(0)),
            [43] = ("BreakawaySaves", StatAggregate.Const(0)),
            [44] = ("PenaltyShots", StatAggregate.Const(0)),
            [45] = ("PenaltyShotSaves", StatAggregate.Const(0)),
        }
    );

    private static readonly (int[]? PosOrder, int Width, Dictionary<int, (string StatName, StatAggregate Aggregate)> Stats) PerformanceTrackerRanked =
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
        ["StatPerformanceTrackerRankedOTP"] = PerformanceTrackerRanked,
        ["StatClubRulesPlayer"] = (PosOrder: [9], Width: 70, new()
        {
            [2] = ("Rating_Skater_Overall", StatAggregate.AvgLast("sovr", 10, Position.AllSkaters)),
            [3] = ("Rating_Skater_Position", StatAggregate.AvgLast("spos", 10, Position.AllSkaters)),
            [4] = ("Rating_Skater_TeamPlay", StatAggregate.AvgLast("stem", 10, Position.AllSkaters)),
            [5] = ("Rating_Skater_Stats", StatAggregate.AvgLast("ssta", 10, Position.AllSkaters)),
            [8] = ("Rating_Goalie_Overall", StatAggregate.AvgLast("govr", 10, Position.Goalie)),
            [9] = ("Rating_Goalie_Position", StatAggregate.AvgLast("gpos", 10, Position.Goalie)),
            [10] = ("Rating_Goalie_TeamPlay", StatAggregate.AvgLast("gtem", 10, Position.Goalie)),
            [11] = ("Rating_Goalie_Stats", StatAggregate.AvgLast("gsta", 10, Position.Goalie)),
            [12] = ("Rating_LW_Overall", StatAggregate.AvgLast("sovr", 10, Position.LeftWing)),
            [13] = ("Rating_LW_Position", StatAggregate.AvgLast("spos", 10, Position.LeftWing)),
            [14] = ("Rating_LW_TeamPlay", StatAggregate.AvgLast("stem", 10, Position.LeftWing)),
            [15] = ("Rating_LW_Stats", StatAggregate.AvgLast("ssta", 10, Position.LeftWing)),
            [16] = ("Rating_RW_Overall", StatAggregate.AvgLast("sovr", 10, Position.RightWing)),
            [17] = ("Rating_RW_Position", StatAggregate.AvgLast("spos", 10, Position.RightWing)),
            [18] = ("Rating_RW_TeamPlay", StatAggregate.AvgLast("stem", 10, Position.RightWing)),
            [19] = ("Rating_RW_Stats", StatAggregate.AvgLast("ssta", 10, Position.RightWing)),
            [20] = ("Rating_C_Overall", StatAggregate.AvgLast("sovr", 10, Position.Center)),
            [21] = ("Rating_C_Position", StatAggregate.AvgLast("spos", 10, Position.Center)),
            [22] = ("Rating_C_TeamPlay", StatAggregate.AvgLast("stem", 10, Position.Center)),
            [23] = ("Rating_C_Stats", StatAggregate.AvgLast("ssta", 10, Position.Center)),
            [24] = ("Rating_D_Overall", StatAggregate.AvgLast("sovr", 10, Position.Defense)),
            [25] = ("Rating_D_Position", StatAggregate.AvgLast("spos", 10, Position.Defense)),
            [26] = ("Rating_D_TeamPlay", StatAggregate.AvgLast("stem", 10, Position.Defense)),
            [27] = ("Rating_D_Stats", StatAggregate.AvgLast("ssta", 10, Position.Defense)),
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