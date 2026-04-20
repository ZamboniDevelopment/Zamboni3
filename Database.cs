using System.Collections;
using System.Reflection;
using Blaze3SDK.Blaze.GameReporting;
using NLog;
using Npgsql;
using Tdf;
using ZamboniCommonComponents.Structs.TdfTagged;
using GameReport = Blaze3SDK.Blaze.GameReportingLegacy.GameReport;

namespace Zamboni14Legacy;

public class Database
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public readonly static string ConnectionString = Program.ZamboniConfig.DatabaseConnectionString;
    public readonly bool isEnabled;

    private ulong fallbackGameIdCounter = 1;

    public Database()
    {
        try
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            isEnabled = true;
            Logger.Warn("Database is accessible.");
        }
        catch (Exception)
        {
            isEnabled = false;
            Logger.Warn("Database is not accessible. Gamedata wont be saved");
            return;
        }

        CreateGameIdSequence();
        CreateGamesTable();
        CreateReportTable();
        CreateSoReportTable();

        CreateLegacyGamesTable();
        CreateLegacyReportTable();
        CreateLegacyOtpReportTable();
        CreateLegacySoReportTable();
        CreateLegacyHutReportTable();
    }

    private void CreateGameIdSequence()
    {
        using var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();

        const string createSequenceQuery = @"
            CREATE SEQUENCE IF NOT EXISTS zamboni_game_id_seq
            START 1
            INCREMENT 1;
        ";

        using var cmd = new NpgsqlCommand(createSequenceQuery, conn);
        cmd.ExecuteNonQuery();
    }

    private void CreateGamesTable()
    {
        using var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();

        const string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS games (
                    game_id NUMERIC(20,0) PRIMARY KEY,
                    gtyp VARCHAR,
                    arid NUMERIC(20,0),
                    cbid BIGINT,
                    ct_id BIGINT,
                    grid NUMERIC(20,0),
                    gtim BIGINT,
                    isim BOOLEAN,
                    lgid BIGINT,
                    nper INTEGER,
                    ovrt BIGINT,
                    plen INTEGER,
                    rank BOOLEAN,
                    roid BIGINT,
                    seid BIGINT,
                    shoo BIGINT,
                    skil INTEGER,
                    sku INTEGER,
                -- stus shouldn't be used as a reliable way to tell the status of the game, rely on reports table cdnf value or other means
                    stus BIGINT,
                    type VARCHAR,
                    venu INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );";

        using var cmd = new NpgsqlCommand(createTableQuery, conn);
        cmd.ExecuteNonQuery();
    }

    private void CreateReportTable()
    {
        using var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();

        const string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS reports_vs (
                    game_id NUMERIC(20,0) NOT NULL,
                    user_id NUMERIC(20,0) NOT NULL,
                    
                    cdnf BIGINT,
                    cht INTEGER,
                    
                    bag BIGINT,
                    bao BIGINT,
                    bs BIGINT,
                    fo BIGINT,
                    fol BIGINT,
                    hits BIGINT,
                    loga BIGINT,
                    logf BIGINT,
                    otg BIGINT,
                    oto BIGINT,
                    pims BIGINT,
                    ppg BIGINT,
                    ppga BIGINT,
                    ppo BIGINT,
                    psa BIGINT,
                    psg BIGINT,
                    pssa BIGINT,
                    pssc BIGINT,
                    shg BIGINT,
                    shga BIGINT,
                    shta BIGINT,
                    shts BIGINT,
                    sklv BIGINT,
                    so BIGINT,
                    toa BIGINT,
                    tsh BIGINT,
                    wiga BIGINT,
                    wigf BIGINT,
                    
                    csco BIGINT,
                    ctry INTEGER,
                    disc INTEGER,
                    fhrn BIGINT,
                    grlt BIGINT,
                    gtag VARCHAR,
                    home BOOLEAN,
                    loss BIGINT,
                    name VARCHAR,
                    opct BIGINT,
                    
                    bandavggm BIGINT,
                    bandavgnet BIGINT,
                    bandhigm BIGINT,
                    bandhinet BIGINT,
                    bandlowgm BIGINT,
                    bandlownet BIGINT,
                    bytesrcvdgm BIGINT,
                    bytesrcvdnet BIGINT,
                    bytessentgm BIGINT,
                    bytessentnet BIGINT,
                    droppkts BIGINT,
                    fpsavg BIGINT,
                    fpsdev BIGINT,
                    fpshi BIGINT,
                    fpslow BIGINT,
                    gdesyncend BIGINT,
                    gdesyncrsn BIGINT,
                    gendphase BIGINT,
                    gresult BIGINT,
                    grpttype BIGINT,
                    grptver BIGINT,
                    guests0 BIGINT,
                    guests1 BIGINT,
                    lateavggm BIGINT,
                    lateavgnet BIGINT,
                    latehigm BIGINT,
                    latehinet BIGINT,
                    latelowgm BIGINT,
                    latelownet BIGINT,
                    latesdevgm BIGINT,
                    latesdevnet BIGINT,
                    pktloss BIGINT,
                    usersend0 BIGINT,
                    usersend1 BIGINT,
                    usersstrt0 BIGINT,
                    usersstrt1 BIGINT,
                    voipend0 BIGINT,
                    voipend1 BIGINT,
                    voipstrt0 BIGINT,
                    voipstrt1 BIGINT,
                    
                    otl BIGINT,
                    peid NUMERIC(20,0),
                    pnid NUMERIC(20,0),
                    ppna VARCHAR,
                    ptag BIGINT,
                    quit INTEGER,
                    relt BIGINT,
                    scor BIGINT,
                    serg INTEGER,
                    skil BIGINT,
                    skpt BIGINT,
                    team BIGINT,
                    ties BIGINT,
                    tnam VARCHAR,
                    wdnf BIGINT,
                    wght BIGINT,
                    wins BIGINT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY (game_id, user_id)
                );";

        using var cmd = new NpgsqlCommand(createTableQuery, conn);
        cmd.ExecuteNonQuery();
    }

    private void CreateSoReportTable()
    {
        using var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();

        const string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS reports_so (
                    game_id NUMERIC(20,0) NOT NULL,
                    user_id NUMERIC(20,0) NOT NULL,
                    
                    cdnf BIGINT,
                    cht INTEGER,

                    ga BIGINT,
                    gf BIGINT,
                    shta BIGINT,
                    shts BIGINT,
                    sklv BIGINT,
                    
                    csco BIGINT,
                    ctry INTEGER,
                    disc INTEGER,
                    fhrn BIGINT,
                    grlt BIGINT,
                    gtag VARCHAR,
                    home BOOLEAN,
                    loss BIGINT,
                    name VARCHAR,
                    opct BIGINT,
                    
                    bandavggm BIGINT,
                    bandavgnet BIGINT,
                    bandhigm BIGINT,
                    bandhinet BIGINT,
                    bandlowgm BIGINT,
                    bandlownet BIGINT,
                    bytesrcvdgm BIGINT,
                    bytesrcvdnet BIGINT,
                    bytessentgm BIGINT,
                    bytessentnet BIGINT,
                    droppkts BIGINT,
                    fpsavg BIGINT,
                    fpsdev BIGINT,
                    fpshi BIGINT,
                    fpslow BIGINT,
                    gdesyncend BIGINT,
                    gdesyncrsn BIGINT,
                    gendphase BIGINT,
                    gresult BIGINT,
                    grpttype BIGINT,
                    grptver BIGINT,
                    guests0 BIGINT,
                    guests1 BIGINT,
                    lateavggm BIGINT,
                    lateavgnet BIGINT,
                    latehigm BIGINT,
                    latehinet BIGINT,
                    latelowgm BIGINT,
                    latelownet BIGINT,
                    latesdevgm BIGINT,
                    latesdevnet BIGINT,
                    pktloss BIGINT,
                    usersend0 BIGINT,
                    usersend1 BIGINT,
                    usersstrt0 BIGINT,
                    usersstrt1 BIGINT,
                    voipend0 BIGINT,
                    voipend1 BIGINT,
                    voipstrt0 BIGINT,
                    voipstrt1 BIGINT,
                    
                    otl BIGINT,
                    peid NUMERIC(20,0),
                    pnid NUMERIC(20,0),
                    ppna VARCHAR,
                    ptag BIGINT,
                    quit INTEGER,
                    relt BIGINT,
                    scor BIGINT,
                    serg INTEGER,
                    skil BIGINT,
                    skpt BIGINT,
                    team BIGINT,
                    ties BIGINT,
                    tnam VARCHAR,
                    wdnf BIGINT,
                    wght BIGINT,
                    wins BIGINT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY (game_id, user_id)
                );";

        using var cmd = new NpgsqlCommand(createTableQuery, conn);
        cmd.ExecuteNonQuery();
    }

    private void CreateLegacyGamesTable()
    {
        using var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();

        const string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS games_l (
                    game_id BIGINT PRIMARY KEY,
                    fnsh BOOLEAN,
                    gtyp INTEGER,
                    venue INTEGER,
                    ""time"" INTEGER,
                    sku INTEGER,
                    skil INTEGER,
                    shootout INTEGER,
                    pnum INTEGER,
                    prcs BOOLEAN,
                    plen INTEGER,
                    ot INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );";

        using var cmd = new NpgsqlCommand(createTableQuery, conn);
        cmd.ExecuteNonQuery();
    }

    private void CreateLegacyReportTable()
    {
        using var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();

        const string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS reports_l (
                -- Primary Keys / Identifiers
                game_id BIGINT NOT NULL,
                user_id BIGINT NOT NULL,
                -- Network and Bandwidth Stats
                bandavggm INTEGER,
                bandavgnet INTEGER,
                bandhigm INTEGER,
                bandhinet INTEGER,
                bandlowgm INTEGER,
                bandlownet INTEGER,
                bytesrcvdgm INTEGER,
                bytesrcvdnet INTEGER,
                bytessentgm INTEGER,
                bytessentnet INTEGER,
                droppkts INTEGER,
                lateavggm INTEGER,
                lateavgnet INTEGER,
                latehigm INTEGER,
                latehinet INTEGER,
                latelowgm INTEGER,
                latelownet INTEGER,
                latesdevgm INTEGER,
                latesdevnet INTEGER,
                pktloss INTEGER,
                -- Performance, Synchronization, and Session Stats
                fpsavg INTEGER,
                fpsdev INTEGER,
                fpshi INTEGER,
                fpslow INTEGER,
                gdesyncend INTEGER,
                gdesyncrsn INTEGER,
                gendphase INTEGER,
                gresult INTEGER,
                grpttype INTEGER,
                grptver VARCHAR,
                guests0 INTEGER,
                guests1 INTEGER,
                usersend0 INTEGER,
                usersend1 INTEGER,
                usersstrt0 INTEGER,
                usersstrt1 INTEGER,
                voipend0 INTEGER,
                voipend1 INTEGER,
                voipstrt0 INTEGER,
                voipstrt1 INTEGER,
                -- Player Metadata and Game Outcome
                gamertag VARCHAR,
                name VARCHAR,
                team INTEGER,
                team_name VARCHAR,
                home INTEGER,
                quit INTEGER,
                quitscore INTEGER,
                disc INTEGER,
                cheat INTEGER,
                score INTEGER,
                userresult INTEGER,
                weight INTEGER,
                -- In-Game Statistics
                bkchance INTEGER,
                bkgoal INTEGER,
                blkshot INTEGER,
                faceoff INTEGER,
                hits INTEGER,
                passchance INTEGER,
                passcomp INTEGER,
                penmin INTEGER,
                ppo INTEGER,
                ppg INTEGER,
                pshchance INTEGER,
                pshgoal INTEGER,
                onetgoal INTEGER,
                onetchance INTEGER,
                shg INTEGER,
                shots INTEGER,
                toa INTEGER,
                -- Audit Field
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (game_id, user_id)
            );";

        using var cmd = new NpgsqlCommand(createTableQuery, conn);
        cmd.ExecuteNonQuery();
    }

    private void CreateLegacySoReportTable()
    {
        using var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();

        const string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS so_reports_l (
                -- Primary Keys / Identifiers (Assumed)
                game_id BIGINT NOT NULL,
                user_id BIGINT NOT NULL,
                -- Network and Bandwidth Stats
                bandavggm INTEGER,
                bandavgnet INTEGER,
                bandhigm INTEGER,
                bandhinet INTEGER,
                bandlowgm INTEGER,
                bandlownet INTEGER,
                bytesrcvdgm INTEGER,
                bytesrcvdnet INTEGER,
                bytessentgm INTEGER,
                bytessentnet INTEGER,
                droppkts INTEGER,
                lateavggm INTEGER,
                lateavgnet INTEGER,
                latehigm INTEGER,
                latehinet INTEGER,
                latelowgm INTEGER,
                latelownet INTEGER,
                latesdevgm INTEGER,
                latesdevnet INTEGER,
                pktloss INTEGER,
                -- Performance, Synchronization, and Session Stats
                fpsavg INTEGER,
                fpsdev INTEGER,
                fpshi INTEGER,
                fpslow INTEGER,
                gdesyncend INTEGER,
                gdesyncrsn INTEGER,
                gendphase INTEGER,
                gresult INTEGER,
                grpttype INTEGER,
                grptver VARCHAR,
                guests0 INTEGER,
                guests1 INTEGER,
                usersend0 INTEGER,
                usersend1 INTEGER,
                usersstrt0 INTEGER,
                usersstrt1 INTEGER,
                voipend0 INTEGER,
                voipend1 INTEGER,
                voipstrt0 INTEGER,
                voipstrt1 INTEGER,
                -- Player Metadata and Game Outcome
                gamertag VARCHAR,
                name VARCHAR,
                team INTEGER,
                team_name VARCHAR,
                home INTEGER,
                quit INTEGER,
                disc INTEGER,
                cheat INTEGER,
                score INTEGER,
                userresult INTEGER,
                weight INTEGER,
                shots INTEGER,
                -- Audit Field
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (game_id, user_id)
            );";

        using var cmd = new NpgsqlCommand(createTableQuery, conn);
        cmd.ExecuteNonQuery();
    }

    private void CreateLegacyOtpReportTable()
    {
        using var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();

        const string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS otp_reports_l (
                    -- Primary Keys / Identifiers
                    game_id BIGINT NOT NULL,
                    user_id BIGINT NOT NULL,
                    -- Network/Bandwidth Stats
                    bandavggm INTEGER,
                    bandavgnet INTEGER,
                    bandhigm INTEGER,
                    bandhinet INTEGER,
                    bandlowgm INTEGER,
                    bandlownet INTEGER,
                    bytesrcvdgm INTEGER,
                    bytesrcvdnet INTEGER,
                    bytessentgm INTEGER,
                    bytessentnet INTEGER,
                    droppkts INTEGER,
                    lateavggm INTEGER,
                    lateavgnet INTEGER,
                    latehigm INTEGER,
                    latehinet INTEGER,
                    latelowgm INTEGER,
                    latelownet INTEGER,
                    latesdevgm INTEGER,
                    latesdevnet INTEGER,
                    pktloss INTEGER,
                    -- Performance/Session Stats
                    fpsavg INTEGER,
                    fpsdev INTEGER,
                    fpshi INTEGER,
                    fpslow INTEGER,
                    gdesyncend INTEGER,
                    gdesyncrsn INTEGER,
                    gendphase INTEGER,
                    gresult INTEGER,
                    grpttype INTEGER,
                    grptver VARCHAR,
                    guests0 INTEGER,
                    guests1 INTEGER,
                    usersend0 INTEGER,
                    usersend1 INTEGER,
                    usersstrt0 INTEGER,
                    usersstrt1 INTEGER,
                    voipend0 INTEGER,
                    voipend1 INTEGER,
                    voipstrt0 INTEGER,
                    voipstrt1 INTEGER,
                    -- Game & Player Metadata
                    gamertag VARCHAR,
                    name VARCHAR,
                    plycrfirst VARCHAR,
                    plycrlast VARCHAR,
                    plycrname VARCHAR,
                    team_name VARCHAR,
                    team INTEGER,
                    home INTEGER,
                    pos INTEGER,
                    quit INTEGER,
                    disc INTEGER,
                    cheat INTEGER,
                    score INTEGER,
                    userresult INTEGER,
                    -- Player In-Game Stats
                    lass INTEGER,
                    lblkshots INTEGER,
                    ldekemade INTEGER,
                    ldeketry INTEGER,
                    lfit INTEGER,
                    lfitwon INTEGER,
                    lfo INTEGER,
                    lfowon INTEGER,
                    lgdespsave INTEGER,
                    lgive INTEGER,
                    lgminplay INTEGER,
                    lgoal INTEGER,
                    lgpsave INTEGER,
                    lgpshot INTEGER,
                    lgrateo INTEGER,
                    lgratep INTEGER,
                    lgrates INTEGER,
                    lgratet INTEGER,
                    lgsa INTEGER,
                    lgsave INTEGER,
                    lgso INTEGER,
                    lgsosave INTEGER,
                    lgsoshot INTEGER,
                    lgwg INTEGER,
                    lgwin INTEGER,
                    lhits INTEGER,
                    loff INTEGER,
                    lpim INTEGER,
                    lplusmin INTEGER,
                    lpos INTEGER,
                    lppg INTEGER,
                    lscrchnce INTEGER,
                    lscrngoal INTEGER,
                    lshg INTEGER,
                    lshots INTEGER,
                    lsrateo INTEGER,
                    lsratep INTEGER,
                    lsrates INTEGER,
                    lsratet INTEGER,
                    lswin INTEGER,
                    ltake INTEGER,
                    -- Audit Field
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY (game_id, user_id)
                );";

        using var cmd = new NpgsqlCommand(createTableQuery, conn);
        cmd.ExecuteNonQuery();
    }

    private void CreateLegacyHutReportTable()
    {
        using var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();

        const string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS hut_reports_l (
                -- Primary Keys / Identifiers
                game_id BIGINT NOT NULL,
                user_id BIGINT NOT NULL,
                -- Network and Bandwidth Stats
                bandavggm INTEGER,
                bandavgnet INTEGER,
                bandhigm INTEGER,
                bandhinet INTEGER,
                bandlowgm INTEGER,
                bandlownet INTEGER,
                bytesrcvdgm INTEGER,
                bytesrcvdnet INTEGER,
                bytessentgm INTEGER,
                bytessentnet INTEGER,
                droppkts INTEGER,
                lateavggm INTEGER,
                lateavgnet INTEGER,
                latehigm INTEGER,
                latehinet INTEGER,
                latelowgm INTEGER,
                latelownet INTEGER,
                latesdevgm INTEGER,
                latesdevnet INTEGER,
                pktloss INTEGER,
                -- Performance, Synchronization, and Session Stats
                fpsavg INTEGER,
                fpsdev INTEGER,
                fpshi INTEGER,
                fpslow INTEGER,
                gdesyncend INTEGER,
                gdesyncrsn INTEGER,
                gendphase INTEGER,
                gresult INTEGER,
                grpttype INTEGER,
                grptver VARCHAR,
                guests0 INTEGER,
                guests1 INTEGER,
                usersend0 INTEGER,
                usersend1 INTEGER,
                usersstrt0 INTEGER,
                usersstrt1 INTEGER,
                voipend0 INTEGER,
                voipend1 INTEGER,
                voipstrt0 INTEGER,
                voipstrt1 INTEGER,
                -- Player Metadata and Game Outcome
                gamertag VARCHAR,
                name VARCHAR,
                team INTEGER,
                team_name VARCHAR,
                home INTEGER,
                quit INTEGER,
                quitscore INTEGER,
                disc INTEGER,
                cheat INTEGER,
                score INTEGER,
                userresult INTEGER,
                weight INTEGER,
                -- In-Game Statistics
                bkchance INTEGER,
                bkgoal INTEGER,
                blkshot INTEGER,
                faceoff INTEGER,
                hits INTEGER,
                passchance INTEGER,
                passcomp INTEGER,
                penmin INTEGER,
                ppo INTEGER,
                ppg INTEGER,
                pshchance INTEGER,
                pshgoal INTEGER,
                onetgoal INTEGER,
                onetchance INTEGER,
                shg INTEGER,
                shots INTEGER,
                toa INTEGER,
                -- Hut specific
                tropply1 INTEGER,
                tropply2 INTEGER,
                tropply3 INTEGER,
                -- Audit Field
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (game_id, user_id)
            );";

        using var cmd = new NpgsqlCommand(createTableQuery, conn);
        cmd.ExecuteNonQuery();
    }

    public static async Task InsertReport(SubmitGameReportRequest request)
    {
        await InsertGameData(request);
        await InsertReportData(request);
    }

    private static async Task InsertGameData(SubmitGameReportRequest request)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();

        var gameId = (decimal)request.mGameReport.mGameReportingId;
        var gameType = request.mGameReport.mGameTypeName;
        var reportData = ((Report)request.mGameReport.mReport).mGameInfoReport;

        const string insertMainQuery = @"
            INSERT INTO games (game_id, gtyp) VALUES (@game_id, @gtyp)
            ON CONFLICT (game_id) DO NOTHING;";

        await using (var cmd = new NpgsqlCommand(insertMainQuery, conn))
        {
            cmd.Parameters.AddWithValue("game_id", gameId);
            cmd.Parameters.AddWithValue("gtyp", gameType);
            cmd.ExecuteNonQuery();
        }

        await ProcessObject(conn, "games", reportData, gameId);
    }

    private static async Task ProcessObject(NpgsqlConnection conn, string table, object? obj, decimal gameId, ulong? userId = null)
    {
        if (obj == null) return;
        foreach (var field in obj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            var value = field.GetValue(obj);
            if (value == null) continue;

            if (value is IDictionary dict)
            {
                foreach (DictionaryEntry entry in dict)
                    await ExecuteDynamicUpsert(conn, table, entry.Key.ToString()!, entry.Value, gameId, userId);
                continue;
            }

            if (!field.FieldType.IsPrimitive && field.FieldType != typeof(string) && field.FieldType != typeof(decimal))
            {
                await ProcessObject(conn, table, value, gameId, userId);
                continue;
            }

            var tag = field.GetCustomAttribute<TdfMember>()?.Tag;
            if (tag != null) await ExecuteDynamicUpsert(conn, table, tag, value, gameId, userId);
        }
    }

    private static async Task ExecuteDynamicUpsert(NpgsqlConnection conn, string table, string tag, object? value, decimal game_id, ulong? user_id)
    {
        var query = "";
        var column = tag.ToLower() == "ctid" ? "ct_id" : tag.ToLower(); //PSQL reserved column name "ctid" >:(

        if (table.Equals("games"))
            query = $@"
                INSERT INTO games (game_id, {column}) VALUES (@game_id, @value)
                ON CONFLICT (game_id) DO UPDATE SET {column} = EXCLUDED.{column};";
        else if (table.Equals("reports_vs"))
            query = $@"
                INSERT INTO reports_vs (game_id, user_id, {column}) VALUES (@game_id, @user_id, @value)
                ON CONFLICT (game_id, user_id) DO UPDATE SET {column} = EXCLUDED.{column};";
        else if (table.Equals("reports_so"))
            query = $@"
                INSERT INTO reports_so (game_id, user_id, {column}) VALUES (@game_id, @user_id, @value)
                ON CONFLICT (game_id, user_id) DO UPDATE SET {column} = EXCLUDED.{column};";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("game_id", game_id);
        if (user_id.HasValue) cmd.Parameters.AddWithValue("user_id", (decimal)user_id.Value);
        cmd.Parameters.AddWithValue("value", MapType(value));

        cmd.ExecuteNonQuery();
    }

    private static async Task InsertReportData(SubmitGameReportRequest request)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();

        var table = request.mGameReport.mGameTypeName switch
        {
            "gameType1" => "reports_vs",
            "gameType2" => "reports_so",
            _ => throw new NotImplementedException($"Game type {request.mGameReport.mGameTypeName} is not mapped.")
        };

        var gameId = (decimal)request.mGameReport.mGameReportingId;
        var reportData = ((Report)request.mGameReport.mReport).mPlayerReports;

        foreach (var user_id in reportData.Keys)
        {
            var insertMainQuery = $@"
                INSERT INTO {table} (game_id, user_id) VALUES (@game_id, @user_id)
                ON CONFLICT (game_id, user_id) DO NOTHING;";

            await using (var cmd = new NpgsqlCommand(insertMainQuery, conn))
            {
                cmd.Parameters.AddWithValue("game_id", gameId);
                cmd.Parameters.AddWithValue("user_id", (decimal)user_id);
                cmd.ExecuteNonQuery();
            }

            await ProcessObject(conn, table, reportData[user_id], gameId, user_id);
        }
    }

    private static object MapType(object? val)
    {
        return val switch
        {
            ulong uLongValue => (decimal)uLongValue,
            uint uIntValue => (long)uIntValue,
            ushort uShortValue => (int)uShortValue,
            _ => val ?? DBNull.Value
        };
    }

    public async Task InsertLegacyReport(GameReport report, long reporterUserId)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        const string insertGameQuery = @"
            INSERT INTO games_l (
                game_id, fnsh, gtyp
            ) VALUES (
                @game_id, @fnsh, @gtyp
            )
            ON CONFLICT (game_id) DO NOTHING;";

        await using var cmd = new NpgsqlCommand(insertGameQuery, conn);
        cmd.Parameters.AddWithValue("game_id", (decimal)report.mGameReportingId);
        cmd.Parameters.AddWithValue("fnsh", report.mFinished);
        cmd.Parameters.AddWithValue("gtyp", (long)report.mGameTypeId);
        cmd.Parameters.AddWithValue("prcs", report.mProcess);
        await cmd.ExecuteNonQueryAsync();

        var gameAttributeMap = report.mAttributeMap;
        foreach (var key in gameAttributeMap.Keys)
        {
            var column = key.ToLower();
            var insertGameAttributeQuery = $@"
                INSERT INTO games_l (game_id, {column})
                    VALUES (@game_id, @value)
                ON CONFLICT (game_id) DO UPDATE
                    SET {column} = EXCLUDED.{column};";

            await using var cmd1 = new NpgsqlCommand(insertGameAttributeQuery, conn);
            cmd1.Parameters.AddWithValue("game_id", (decimal)report.mGameReportingId);

            if (int.TryParse(gameAttributeMap[key], out var intValue))
                cmd1.Parameters.AddWithValue("value", intValue);
            else
                cmd1.Parameters.AddWithValue("value", gameAttributeMap[key]);
            await cmd1.ExecuteNonQueryAsync();
        }

        var tableName = "reports_l";
        switch (report.mGameTypeId)
        {
            case 1:
                tableName = "reports_l";
                break;
            case 2:
                tableName = "so_reports_l";
                break;
            case 3:
                tableName = "otp_reports_l";
                break;
            case 6:
                tableName = "hut_reports_l";
                break;
        }

        var mPlayerReportMap = report.mPlayerReportMap;
        foreach (var userId in mPlayerReportMap.Keys)
        {
            var insertPlayerQuery = $@"
                INSERT INTO {tableName} ( 
                    game_id, user_id
                ) VALUES (
                    @game_id, @user_id
                )
                ON CONFLICT (game_id, user_id) DO NOTHING;";

            await using var cmd1 = new NpgsqlCommand(insertPlayerQuery, conn);
            cmd1.Parameters.AddWithValue("game_id", (long)report.mGameReportingId);
            cmd1.Parameters.AddWithValue("user_id", userId);
            await cmd1.ExecuteNonQueryAsync();
        }

        var playerAttributeMap = mPlayerReportMap[reporterUserId].mAttributeMap;
        foreach (var key in playerAttributeMap.Keys)
        {
            var column = key.ToLower();
            var insertPlayerAttributeQuery = $@"
                    INSERT INTO {tableName} (game_id, user_id, {column})
                        VALUES (@game_id, @user_id, @value)
                    ON CONFLICT (game_id, user_id) DO UPDATE
                        SET {column} = EXCLUDED.{column};";

            await using var cmd1 = new NpgsqlCommand(insertPlayerAttributeQuery, conn);
            cmd1.Parameters.AddWithValue("game_id", (long)report.mGameReportingId);
            cmd1.Parameters.AddWithValue("user_id", reporterUserId);

            if (int.TryParse(playerAttributeMap[key], out var intValue))
                cmd1.Parameters.AddWithValue("value", intValue);
            else
                cmd1.Parameters.AddWithValue("value", playerAttributeMap[key]);
            await cmd1.ExecuteNonQueryAsync();
        }
    }

    public ulong GetNextGameId()
    {
        if (!isEnabled) return fallbackGameIdCounter++;

        using var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();
        //TODO: PSQL overflows at 9 quintillion. Though game client cant receive a game id max of 18 quintillion.
        using var cmd = new NpgsqlCommand("SELECT nextval('zamboni_game_id_seq');", conn);
        var result = cmd.ExecuteScalar();

        if (result == null || result == DBNull.Value)
            throw new InvalidOperationException("Sequence returned no value.");

        return (ulong)(long)result;
    }
}