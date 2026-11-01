using System.IO;
using System.Globalization;

namespace IRacingTracker;

public sealed class EncounterContext
{
    public long SubSessionId;      // negativo/sintetico em sessoes offline (SubSessionID = 0)
    public int SessionNum;
    public string SessionType = "";
    public bool Official;
    public int SeriesId;
    public string Track = "";
    public int MyUserId;
}

public sealed class EncounterRow
{
    public string When = "";
    public string Track = "";
    public string Car = "";
    public string SessionType = "";
    public bool Official;
    public bool IsTest;
    public int IRating;
}

public sealed class DriverHistory
{
    public int Count;
    public string LastSeen = "";
    public List<EncounterRow> Recent = new();
    public List<string> Names = new();
}

/// <summary>Base de dados local (SQLite) com os pilotos encontrados, identificados pelo Customer ID.</summary>
public sealed class HistoryDb : IDisposable
{
    private readonly SqliteDb _db;

    public HistoryDb(string path)
    {
        _db = new SqliteDb(path);
        Migrate();
    }

    private void Migrate()
    {
        _db.Execute(@"CREATE TABLE IF NOT EXISTS h2h (
            cust_id       INTEGER NOT NULL,
            subsession_id INTEGER NOT NULL,
            ahead         INTEGER NOT NULL,
            PRIMARY KEY (cust_id, subsession_id))");
        _db.Execute(@"CREATE TABLE IF NOT EXISTS drivers (
            cust_id    INTEGER PRIMARY KEY,
            name       TEXT NOT NULL,
            first_seen TEXT NOT NULL,
            last_seen  TEXT NOT NULL)");

        _db.Execute(@"CREATE TABLE IF NOT EXISTS driver_names (
            cust_id    INTEGER NOT NULL,
            name       TEXT NOT NULL,
            first_seen TEXT NOT NULL,
            PRIMARY KEY (cust_id, name))");

        _db.Execute(@"CREATE TABLE IF NOT EXISTS encounters (
            id            INTEGER PRIMARY KEY AUTOINCREMENT,
            cust_id       INTEGER NOT NULL,
            subsession_id INTEGER NOT NULL,
            session_num   INTEGER NOT NULL,
            session_type  TEXT NOT NULL,
            official      INTEGER NOT NULL,
            is_test       INTEGER NOT NULL,
            series_id     INTEGER,
            track         TEXT,
            car           TEXT,
            car_class     TEXT,
            irating       INTEGER,
            occurred_at   TEXT NOT NULL,
            UNIQUE (subsession_id, session_num, cust_id))");

        _db.Execute("CREATE INDEX IF NOT EXISTS ix_encounters_cust ON encounters(cust_id)");

        // ---- Fase 4: tags, historico de alteracoes e notas ----
        _db.Execute(@"CREATE TABLE IF NOT EXISTS tags (
            id          INTEGER PRIMARY KEY AUTOINCREMENT,
            key         TEXT NOT NULL UNIQUE,
            name        TEXT NOT NULL,
            description TEXT NOT NULL,
            color       INTEGER NOT NULL,
            sort_order  INTEGER NOT NULL,
            is_builtin  INTEGER NOT NULL)");

        _db.Execute(@"CREATE TABLE IF NOT EXISTS driver_tags (
            cust_id     INTEGER NOT NULL,
            tag_id      INTEGER NOT NULL,
            assigned_at TEXT NOT NULL,
            PRIMARY KEY (cust_id, tag_id))");

        _db.Execute(@"CREATE TABLE IF NOT EXISTS tag_log (
            id            INTEGER PRIMARY KEY AUTOINCREMENT,
            cust_id       INTEGER NOT NULL,
            tag_id        INTEGER NOT NULL,
            action        TEXT NOT NULL,
            at            TEXT NOT NULL,
            subsession_id INTEGER,
            track         TEXT)");

        _db.Execute(@"CREATE TABLE IF NOT EXISTS driver_notes (
            id            INTEGER PRIMARY KEY AUTOINCREMENT,
            cust_id       INTEGER NOT NULL,
            note          TEXT NOT NULL,
            created_at    TEXT NOT NULL,
            subsession_id INTEGER,
            track         TEXT)");

        _db.Execute("CREATE INDEX IF NOT EXISTS ix_tag_log_cust ON tag_log(cust_id)");
        _db.Execute("CREATE INDEX IF NOT EXISTS ix_notes_cust ON driver_notes(cust_id)");

        SeedTags();
    }

    private void SeedTags()
    {
        // key, nome, descricao, cor (RGB), ordem
        object[][] seed =
        {
            new object[] { "skull",     "Sujo",         "Piloto perigoso, antidesportivo ou com comportamentos muito negativos", 0xF85149, 1 },
            new object[] { "shield",    "Limpo",        "Piloto correto, previsível e respeitador nas disputas",                0x3FB950, 2 },
            new object[] { "bolt",      "Agressivo",    "Disputa posições de forma muito agressiva",                            0xF0883E, 3 },
            new object[] { "warning",   "Imprevisível", "Difícil de antecipar nas manobras",                                    0xE3B341, 4 },
            new object[] { "star",      "Rápido",       "Piloto com bom ritmo e velocidade",                                    0x58A6FF, 5 },
            new object[] { "repeat",    "Reincidente",  "Vários incidentes ou comportamentos negativos repetidos",              0xBC8CFF, 6 },
            new object[] { "handshake", "Respeitador",  "Demonstra respeito pelos adversários",                                 0x56D4DD, 7 },
        };
        foreach (object[] t in seed)
        {
            _db.Execute(
                "INSERT OR IGNORE INTO tags (key, name, description, color, sort_order, is_builtin) VALUES (?, ?, ?, ?, ?, 1)",
                (string)t[0], (string)t[1], (string)t[2], (int)t[3], (int)t[4]);
        }
    }

    // ------------------------------------------------------------------
    //  Tags e notas
    // ------------------------------------------------------------------

    public List<TagDef> LoadTags()
    {
        var list = new List<TagDef>();
        var rows = _db.Query("SELECT id, key, name, description, color, sort_order, is_builtin FROM tags ORDER BY sort_order, id");
        foreach (var r in rows)
        {
            list.Add(new TagDef
            {
                Id = (long)r[0]!,
                Key = r[1] as string ?? "",
                Name = r[2] as string ?? "",
                Description = r[3] as string ?? "",
                Color = (int)(long)r[4]!,
                SortOrder = (int)(long)r[5]!,
                IsBuiltin = (long)(r[6] ?? 1L) != 0
            });
        }
        return list;
    }

    /// <summary>Cria uma tag personalizada (nao mexe nas de origem).</summary>
    public void AddCustomTag(string name, string iconKey, int color)
    {
        string key = "c" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ":" + iconKey;
        long next = 100;
        var rows = _db.Query("SELECT COALESCE(MAX(sort_order), 0) FROM tags");
        if (rows.Count > 0 && rows[0].Length > 0 && rows[0][0] is long m) next = Math.Max(next, m + 1);
        _db.Execute(
            "INSERT INTO tags (key, name, description, color, sort_order, is_builtin) VALUES (?, ?, ?, ?, ?, 0)",
            key, name, "Tag personalizada", color, (int)next);
    }

    /// <summary>Apaga uma tag personalizada e tira-a de todos os pilotos.</summary>
    public void DeleteCustomTag(long tagId)
    {
        _db.Execute("BEGIN");
        try
        {
            _db.Execute("DELETE FROM driver_tags WHERE tag_id = ? AND tag_id IN (SELECT id FROM tags WHERE is_builtin = 0)", tagId);
            _db.Execute("DELETE FROM tag_log WHERE tag_id = ? AND tag_id IN (SELECT id FROM tags WHERE is_builtin = 0)", tagId);
            _db.Execute("DELETE FROM tags WHERE id = ? AND is_builtin = 0", tagId);
            _db.Execute("COMMIT");
        }
        catch
        {
            try { _db.Execute("ROLLBACK"); } catch { }
            throw;
        }
    }

    /// <summary>Customer ID -> ids das tags atribuidas.</summary>
    public Dictionary<int, HashSet<long>> LoadAllDriverTags()
    {
        var map = new Dictionary<int, HashSet<long>>();
        var rows = _db.Query("SELECT cust_id, tag_id FROM driver_tags");
        foreach (var r in rows)
        {
            int cust = (int)(long)r[0]!;
            long tag = (long)r[1]!;
            if (!map.TryGetValue(cust, out HashSet<long>? ids))
            {
                ids = new HashSet<long>();
                map[cust] = ids;
            }
            ids.Add(tag);
        }
        return map;
    }

    private void EnsureDriver(int custId, string name, string now)
    {
        _db.Execute("INSERT OR IGNORE INTO drivers (cust_id, name, first_seen, last_seen) VALUES (?, ?, ?, ?)",
            custId, name, now, now);
        _db.Execute("INSERT OR IGNORE INTO driver_names (cust_id, name, first_seen) VALUES (?, ?, ?)",
            custId, name, now);
    }

    /// <summary>Liga/desliga uma tag num piloto e regista a alteracao. Devolve true se a tag ficou ligada.</summary>
    public bool ToggleTag(int custId, string name, long tagId, long subSessionId, string track)
    {
        string now = Now();
        bool turnOn = _db.Query("SELECT 1 FROM driver_tags WHERE cust_id = ? AND tag_id = ?", custId, tagId).Count == 0;

        _db.Execute("BEGIN");
        try
        {
            EnsureDriver(custId, name, now);
            if (turnOn)
                _db.Execute("INSERT INTO driver_tags (cust_id, tag_id, assigned_at) VALUES (?, ?, ?)", custId, tagId, now);
            else
                _db.Execute("DELETE FROM driver_tags WHERE cust_id = ? AND tag_id = ?", custId, tagId);

            _db.Execute(
                "INSERT INTO tag_log (cust_id, tag_id, action, at, subsession_id, track) VALUES (?, ?, ?, ?, ?, ?)",
                custId, tagId, turnOn ? "add" : "remove", now, subSessionId, track);
            _db.Execute("COMMIT");
        }
        catch
        {
            try { _db.Execute("ROLLBACK"); } catch { }
            throw;
        }
        return turnOn;
    }

    public List<string> GetTagLog(int custId, int limit)
    {
        var lines = new List<string>();
        var rows = _db.Query(
            @"SELECT l.at, t.name, l.action, l.track FROM tag_log l
              JOIN tags t ON t.id = l.tag_id
              WHERE l.cust_id = ? ORDER BY l.id DESC LIMIT ?",
            custId, limit);
        foreach (var r in rows)
        {
            string when = r[0] as string ?? "";
            string tag = r[1] as string ?? "";
            string action = (r[2] as string) == "add" ? "+" : "-";
            string track = r[3] as string ?? "";
            lines.Add(when + "  " + action + " " + tag + (track.Length > 0 ? "  (" + track + ")" : ""));
        }
        return lines;
    }

    public void AddNote(int custId, string name, string note, long subSessionId, string track)
    {
        string now = Now();
        _db.Execute("BEGIN");
        try
        {
            EnsureDriver(custId, name, now);
            _db.Execute(
                "INSERT INTO driver_notes (cust_id, note, created_at, subsession_id, track) VALUES (?, ?, ?, ?, ?)",
                custId, note, now, subSessionId, track);
            _db.Execute("COMMIT");
        }
        catch
        {
            try { _db.Execute("ROLLBACK"); } catch { }
            throw;
        }
    }

    public List<string> GetNotes(int custId, int limit)
    {
        var lines = new List<string>();
        var rows = _db.Query(
            "SELECT created_at, note, track FROM driver_notes WHERE cust_id = ? ORDER BY id DESC LIMIT ?",
            custId, limit);
        foreach (var r in rows)
        {
            string track = r[2] as string ?? "";
            lines.Add((r[0] as string ?? "") + "  " + (r[1] as string ?? "") + (track.Length > 0 ? "  (" + track + ")" : ""));
        }
        return lines;
    }

    private static string Now()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Regista os pilotos presentes. Numa corrida oficial grava sempre; fora disso so grava em modo teste
    /// (marcado como teste, para poder ser apagado). Nunca regista duas vezes o mesmo piloto na mesma sessao.
    /// Devolve quantos encontros novos foram gravados.
    /// </summary>
    public int RecordEncounters(EncounterContext ctx, IEnumerable<DriverEntry> drivers, bool testMode)
    {
        bool real = ctx.SessionType == "Race" && ctx.Official;
        if (!real && !testMode) return 0;
        int isTest = real ? 0 : 1;
        string now = Now();
        int inserted = 0;

        _db.Execute("BEGIN");
        try
        {
            foreach (var d in drivers)
            {
                if (d.UserId == 0 || d.UserId == ctx.MyUserId) continue;
                if (d.IsSpectator || d.IsPaceCar) continue;
                if (real && (d.IsAI || d.UserId < 0)) continue;

                var exists = _db.Query(
                    "SELECT 1 FROM encounters WHERE subsession_id = ? AND session_num = ? AND cust_id = ?",
                    ctx.SubSessionId, ctx.SessionNum, d.UserId);
                if (exists.Count == 0)
                {
                    _db.Execute(
                        @"INSERT INTO encounters (cust_id, subsession_id, session_num, session_type, official, is_test,
                                                  series_id, track, car, car_class, irating, occurred_at)
                          VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                        d.UserId, ctx.SubSessionId, ctx.SessionNum, ctx.SessionType, ctx.Official ? 1 : 0, isTest,
                        ctx.SeriesId, ctx.Track, d.CarScreenName, d.CarClassShortName, d.IRating, now);
                    inserted++;
                }

                _db.Execute(
                    "INSERT OR IGNORE INTO drivers (cust_id, name, first_seen, last_seen) VALUES (?, ?, ?, ?)",
                    d.UserId, d.DisplayName, now, now);
                _db.Execute("UPDATE drivers SET name = ?, last_seen = ? WHERE cust_id = ?",
                    d.DisplayName, now, d.UserId);
                _db.Execute(
                    "INSERT OR IGNORE INTO driver_names (cust_id, name, first_seen) VALUES (?, ?, ?)",
                    d.UserId, d.DisplayName, now);
            }
            _db.Execute("COMMIT");
        }
        catch
        {
            try { _db.Execute("ROLLBACK"); } catch { }
            throw;
        }

        return inserted;
    }

    /// <summary>
    /// Pilotos que ja encontrei ANTES desta sessao (Customer ID -> numero de encontros).
    /// Exclui a sessao atual, para que ninguem fique "verde" so por estar na corrida em curso.
    /// </summary>
    public Dictionary<int, int> LoadKnown(long currentSubSessionId, bool includeTest)
    {
        var map = new Dictionary<int, int>();
        var rows = _db.Query(
            @"SELECT cust_id, COUNT(*) FROM encounters
              WHERE subsession_id <> ? AND (is_test = 0 OR ? = 1)
              GROUP BY cust_id",
            currentSubSessionId, includeTest ? 1 : 0);
        foreach (var r in rows)
            map[(int)(long)r[0]!] = (int)(long)r[1]!;
        return map;
    }

    public DriverHistory GetHistory(int custId, long currentSubSessionId, bool includeTest)
    {
        var h = new DriverHistory();
        int inc = includeTest ? 1 : 0;

        var sum = _db.Query(
            @"SELECT COUNT(*), MAX(occurred_at) FROM encounters
              WHERE cust_id = ? AND subsession_id <> ? AND (is_test = 0 OR ? = 1)",
            custId, currentSubSessionId, inc);
        if (sum.Count > 0)
        {
            h.Count = (int)(long)sum[0][0]!;
            h.LastSeen = sum[0][1] as string ?? "";
        }

        var recent = _db.Query(
            @"SELECT occurred_at, track, car, session_type, official, is_test, irating FROM encounters
              WHERE cust_id = ? AND subsession_id <> ? AND (is_test = 0 OR ? = 1)
              ORDER BY id DESC LIMIT 8",
            custId, currentSubSessionId, inc);
        foreach (var r in recent)
        {
            h.Recent.Add(new EncounterRow
            {
                When = r[0] as string ?? "",
                Track = r[1] as string ?? "",
                Car = r[2] as string ?? "",
                SessionType = r[3] as string ?? "",
                Official = r[4] is long o && o == 1,
                IsTest = r[5] is long t && t == 1,
                IRating = r[6] is long ir ? (int)ir : 0
            });
        }

        var names = _db.Query("SELECT name FROM driver_names WHERE cust_id = ? ORDER BY first_seen", custId);
        foreach (var r in names)
            if (r[0] is string n) h.Names.Add(n);

        return h;
    }

    /// <summary>Apaga os registos gravados em modo teste. Devolve quantos encontros foram apagados.</summary>
    /// <summary>Guarda quem acabou a frente (1 = eu, 0 = ele) numa corrida oficial. Pode ser chamado varias vezes (substitui).</summary>
    public void SaveHeadToHead(long subSessionId, IEnumerable<KeyValuePair<int, bool>> results)
    {
        _db.Execute("BEGIN");
        try
        {
            foreach (var kv in results)
                _db.Execute("INSERT OR REPLACE INTO h2h (cust_id, subsession_id, ahead) VALUES (?, ?, ?)",
                    kv.Key, subSessionId, kv.Value ? 1 : 0);
            _db.Execute("COMMIT");
        }
        catch
        {
            try { _db.Execute("ROLLBACK"); } catch { }
            throw;
        }
    }

    /// <summary>Em quantas corridas oficiais acabei a frente / atras deste piloto (sem contar a sessao atual).</summary>
    public (int Ahead, int Behind) GetHeadToHead(int custId, long currentSubSessionId)
    {
        var rows = _db.Query(
            "SELECT COALESCE(SUM(ahead), 0), COUNT(*) FROM h2h WHERE cust_id = ? AND subsession_id <> ?",
            custId, currentSubSessionId);
        if (rows.Count == 0) return (0, 0);
        int a = (int)(long)rows[0][0]!;
        int n = (int)(long)rows[0][1]!;
        return (a, n - a);
    }

    public int DeleteTestData()
    {
        int n = _db.Execute("DELETE FROM encounters WHERE is_test = 1");
        // Mantem os pilotos que ainda tenham encontros, tags ou notas
        const string keep = @"cust_id NOT IN (SELECT cust_id FROM encounters)
                              AND cust_id NOT IN (SELECT cust_id FROM driver_tags)
                              AND cust_id NOT IN (SELECT cust_id FROM driver_notes)
                              AND cust_id NOT IN (SELECT cust_id FROM tag_log)";
        _db.Execute("DELETE FROM drivers WHERE " + keep);
        _db.Execute("DELETE FROM driver_names WHERE " + keep);
        return n;
    }

    /// <summary>Copia consistente da base de dados para um ficheiro novo.</summary>
    public void BackupTo(string path)
    {
        if (File.Exists(path)) File.Delete(path);
        _db.Execute("VACUUM INTO ?", path);
    }

    /// <summary>Resumo do conteudo, para mostrar ao exportar/importar.</summary>
    public string Summary()
    {
        long Count(string table)
        {
            var r = _db.Query("SELECT COUNT(*) FROM " + table);
            return r.Count > 0 && r[0].Length > 0 && r[0][0] is long n ? n : 0;
        }
        return Loc.T("bk_summary", Count("drivers"), Count("encounters"), Count("driver_tags"), Count("driver_notes"));
    }

    /// <summary>Procura pilotos no historico por nome ou Customer ID (sem texto: os mais recentes).</summary>
    public List<HistHit> SearchDrivers(string text, int limit)
    {
        string q = (text ?? "").Trim().TrimStart('#');
        string like = "%" + q.Replace("%", "").Replace("_", "") + "%";
        var rows = _db.Query(
            @"SELECT d.cust_id, d.name, d.last_seen,
                     (SELECT COUNT(*) FROM encounters e WHERE e.cust_id = d.cust_id AND e.is_test = 0)
              FROM drivers d
              WHERE ? = '' OR d.name LIKE ? OR CAST(d.cust_id AS TEXT) LIKE ?
                    OR d.cust_id IN (SELECT cust_id FROM driver_names WHERE name LIKE ?)
              ORDER BY d.last_seen DESC LIMIT " + limit.ToString(System.Globalization.CultureInfo.InvariantCulture),
            q, like, like, like);
        var list = new List<HistHit>();
        foreach (var r in rows)
        {
            list.Add(new HistHit
            {
                CustId = (int)(long)r[0]!,
                Name = r[1] as string ?? "",
                LastSeen = r[2] as string ?? "",
                Count = r[3] is long n ? (int)n : 0
            });
        }
        return list;
    }

    public void Dispose() => _db.Dispose();
}
