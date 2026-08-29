using System.Collections;
using System.Globalization;
using System.Reflection;
using Blaze3SDK.Blaze.GameReporting;
using NLog;
using Npgsql;
using Tdf;
using ZamboniCommonComponents.Structs.TdfTagged;
using GameReport = Blaze3SDK.Blaze.GameReportingLegacy.GameReport;

namespace Zamboni3;

public class Database
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public readonly static string ConnectionString = Program.ZamboniConfig.DatabaseConnectionString;
    public readonly bool isEnabled;

    private static readonly Dictionary<string, HashSet<string>> _knownColumns = new();
    private static readonly Dictionary<string, string> ColumnRenames = new() { ["ctid"] = "ct_id" };

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
        CreateOtpReportTable();
        CreateHutReportTable();

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
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY (game_id, user_id)
                );";

        using var cmd = new NpgsqlCommand(createTableQuery, conn);
        cmd.ExecuteNonQuery();
    }

    private void CreateOtpReportTable()
    {
        using var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();

        const string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS reports_otp (
                    game_id NUMERIC(20,0) NOT NULL,
                    user_id NUMERIC(20,0) NOT NULL,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY (game_id, user_id)
                );";

        using var cmd = new NpgsqlCommand(createTableQuery, conn);
        cmd.ExecuteNonQuery();
    }

    private void CreateHutReportTable()
    {
        using var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();

        const string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS reports_hut (
                    game_id NUMERIC(20,0) NOT NULL,
                    user_id NUMERIC(20,0) NOT NULL,
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
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (game_id, user_id)
            );";

        using var cmd = new NpgsqlCommand(createTableQuery, conn);
        cmd.ExecuteNonQuery();
    }

    public async Task InsertReport(SubmitGameReportRequest request, ulong reporterUserId)
    {
        await InsertGameData(request);
        await InsertReportData(request, reporterUserId);
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

    private static async Task ProcessObject(NpgsqlConnection conn, string table, object? obj, decimal gameId, ulong? userId = null, ulong? reporterUserId = null)
    {
        if (obj == null) return;
        if (userId != null && reporterUserId != null)
        {
            if (WhoReportedTuple.Count > 100) WhoReportedTuple.RemoveRange(0, 50);
            if (WhoReportedTuple.Contains(((ulong GameId, ulong PlayerToBeReported, ulong Reporter))(gameId, userId, userId)))
            {
                return;
            }
        }

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

        if (userId != null && reporterUserId != null)
        {
            WhoReportedTuple.Add(((ulong GameId, ulong PlayerToBeReported, ulong Reporter))(gameId, userId, reporterUserId));
        }
    }

    private static async Task ExecuteDynamicUpsert(NpgsqlConnection conn, string table, string tag, object? value, decimal game_id, ulong? user_id)
    {
        var query = "";
        var column = ToColumn(tag);
        var mapped = MapType(value);
        EnsureColumn(conn, table, column, mapped);

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
        else if (table.Equals("reports_otp"))
            query = $@"
                INSERT INTO reports_otp (game_id, user_id, {column}) VALUES (@game_id, @user_id, @value)
                ON CONFLICT (game_id, user_id) DO UPDATE SET {column} = EXCLUDED.{column};";
        else if (table.Equals("reports_hut"))
            query = $@"
                INSERT INTO reports_hut (game_id, user_id, {column}) VALUES (@game_id, @user_id, @value)
                ON CONFLICT (game_id, user_id) DO UPDATE SET {column} = EXCLUDED.{column};";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("game_id", game_id);
        if (user_id.HasValue) cmd.Parameters.AddWithValue("user_id", (decimal)user_id.Value);
        cmd.Parameters.AddWithValue("value", mapped);

        cmd.ExecuteNonQuery();
    }

    private static async Task InsertReportData(SubmitGameReportRequest request, ulong reporterUserId)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();

        var table = request.mGameReport.mGameTypeName switch
        {
            "gameType1" => "reports_vs",
            "gameType2" => "reports_so",
            "gameType3" => "reports_otp",
            "gameType6" => "reports_hut",
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

            await ProcessObject(conn, table, reportData[user_id], gameId, user_id, reporterUserId);
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
    
    private static object ParseLegacy(string raw)
    {
        if (long.TryParse(raw, out var l)) return l;
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return d;
        return raw;
    }

    private static readonly List<(ulong GameId, ulong PlayerToBeReported, ulong Reporter)> WhoReportedTuple = new();

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
            var column = ToColumn(key);
            var raw = gameAttributeMap[key];
            EnsureColumn(conn, "games_l", column, ParseLegacy(raw));
            var insertGameAttributeQuery = $@"
                INSERT INTO games_l (game_id, {column})
                    VALUES (@game_id, @value)
                ON CONFLICT (game_id) DO UPDATE
                    SET {column} = EXCLUDED.{column};";

            await using var cmd1 = new NpgsqlCommand(insertGameAttributeQuery, conn);
            cmd1.Parameters.AddWithValue("game_id", (decimal)report.mGameReportingId);

            cmd1.Parameters.AddWithValue("value", ParseLegacy(raw));

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

        foreach (var pl in mPlayerReportMap.Keys)
        {
            if (WhoReportedTuple.Count > 100) WhoReportedTuple.RemoveRange(0, 50);
            if (WhoReportedTuple.Contains(((ulong GameId, ulong PlayerToBeReported, ulong Reporter))(report.mGameReportingId, pl, pl))) continue;
            foreach (var key in mPlayerReportMap[pl].mAttributeMap.Keys)
            {
                var column = ToColumn(key);
                EnsureColumn(conn, tableName, column, ParseLegacy(mPlayerReportMap[pl].mAttributeMap[key]));
                var insertPlayerAttributeQuery = $@"
                    INSERT INTO {tableName} (game_id, user_id, {column})
                        VALUES (@game_id, @user_id, @value)
                    ON CONFLICT (game_id, user_id) DO UPDATE
                        SET {column} = EXCLUDED.{column};";

                await using var cmd1 = new NpgsqlCommand(insertPlayerAttributeQuery, conn);
                cmd1.Parameters.AddWithValue("game_id", (long)report.mGameReportingId);
                cmd1.Parameters.AddWithValue("user_id", pl);

                cmd1.Parameters.AddWithValue("value", ParseLegacy(mPlayerReportMap[pl].mAttributeMap[key]));
                await cmd1.ExecuteNonQueryAsync();
            }

            WhoReportedTuple.Add(((ulong GameId, ulong PlayerToBeReported, ulong Reporter))(report.mGameReportingId, pl, reporterUserId));
        }
    }

    private static string ToColumn(string key)
    {
        var column = key.ToLowerInvariant();
        if (ColumnRenames.TryGetValue(column, out var renamed)) column = renamed;
        return column;
    }

    private static string InferType(object? val) => val switch
    {
        ulong or decimal => "NUMERIC(20,0)",
        uint or long or int or ushort or short => "BIGINT",
        double or float => "DOUBLE PRECISION",
        bool => "BOOLEAN",
        _ => "TEXT",
    };

    private static void EnsureColumn(NpgsqlConnection conn, string table, string column, object? sampleValue)
    {
        if (!_knownColumns.TryGetValue(table, out var cols))
        {
            _knownColumns[table] = cols = LoadColumns(conn, table);
        }

        if (cols.Contains(column)) return;

        string type = InferType(sampleValue);
        using var cmd = new NpgsqlCommand($"ALTER TABLE {table} ADD COLUMN IF NOT EXISTS \"{column}\" {type}", conn);
        cmd.ExecuteNonQuery();
        cols.Add(column);
    }

    private static HashSet<string> LoadColumns(NpgsqlConnection conn, string table)
    {
        var cols = new HashSet<string>();
        using var cmd = new NpgsqlCommand("SELECT column_name FROM information_schema.columns " + "WHERE table_schema = 'public' AND table_name = @t", conn);
        cmd.Parameters.AddWithValue("t", table);
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) cols.Add(reader.GetString(0));
        return cols;
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