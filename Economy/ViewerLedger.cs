using Microsoft.Data.Sqlite;

namespace SDVChatVsStreamer.Economy;

public class ViewerLedger
{
    private readonly string _dbPath;

    public ViewerLedger(string dbPath)
    {
        SQLitePCL.Batteries_V2.Init();
        _dbPath = dbPath;
        Initialize();
    }

    private void Initialize()
    {
        using var conn = GetConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS viewers (
                username     TEXT PRIMARY KEY,
                points       INTEGER DEFAULT 0,
                sub_tier     INTEGER DEFAULT 0,
                last_chat    TEXT,
                total_earned INTEGER DEFAULT 0,
                total_spent  INTEGER DEFAULT 0
            );";
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection GetConnection()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        return conn;
    }

    // ─── Read ─────────────────────────────────────────────────────────────────

    public ViewerRecord? GetViewer(string username)
    {
        using var conn = GetConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM viewers WHERE username = $u;";
        cmd.Parameters.AddWithValue("$u", username.ToLower());

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new ViewerRecord
        {
            Username    = reader.GetString(0),
            Points      = reader.GetInt32(1),
            SubTier     = (SubTier)reader.GetInt32(2),
            LastChat    = DateTime.Parse(reader.IsDBNull(3) ? DateTime.MinValue.ToString() : reader.GetString(3)),
            TotalEarned = reader.GetInt32(4),
            TotalSpent  = reader.GetInt32(5)
        };
    }

    public int GetPoints(string username) => GetViewer(username.ToLower())?.Points ?? 0;

    public List<ViewerRecord> GetTopViewers(int count)
    {
        using var conn = GetConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM viewers ORDER BY points DESC LIMIT $c;";
        cmd.Parameters.AddWithValue("$c", count);

        var viewers = new List<ViewerRecord>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            viewers.Add(new ViewerRecord
            {
                Username    = reader.GetString(0),
                Points      = reader.GetInt32(1),
                SubTier     = (SubTier)reader.GetInt32(2),
                LastChat    = DateTime.Parse(reader.IsDBNull(3) ? DateTime.MinValue.ToString() : reader.GetString(3)),
                TotalEarned = reader.GetInt32(4),
                TotalSpent  = reader.GetInt32(5)
            });
        }
        return viewers;
    }

    // ─── Write ────────────────────────────────────────────────────────────────

    public void EnsureViewer(string username)
    {
        using var conn = GetConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR IGNORE INTO viewers (username, points, sub_tier, last_chat, total_earned, total_spent)
            VALUES ($u, 0, 0, $t, 0, 0);";
        cmd.Parameters.AddWithValue("$u", username.ToLower());
        cmd.Parameters.AddWithValue("$t", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public void AddPoints(string username, int amount)
    {
        EnsureViewer(username);
        using var conn = GetConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE viewers
            SET points = points + $a, total_earned = total_earned + $a
            WHERE username = $u;";
        cmd.Parameters.AddWithValue("$a", amount);
        cmd.Parameters.AddWithValue("$u", username.ToLower());
        cmd.ExecuteNonQuery();
    }

    public bool DeductPoints(string username, int amount)
    {
        EnsureViewer(username);
        if (GetPoints(username) < amount) return false;

        using var conn = GetConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE viewers
            SET points = points - $a, total_spent = total_spent + $a
            WHERE username = $u;";
        cmd.Parameters.AddWithValue("$a", amount);
        cmd.Parameters.AddWithValue("$u", username.ToLower());
        cmd.ExecuteNonQuery();
        return true;
    }

    public void SetSubTier(string username, SubTier tier)
    {
        EnsureViewer(username);
        using var conn = GetConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = "UPDATE viewers SET sub_tier = $t WHERE username = $u;";
        cmd.Parameters.AddWithValue("$t", (int)tier);
        cmd.Parameters.AddWithValue("$u", username.ToLower());
        cmd.ExecuteNonQuery();
    }

    public void UpdateLastChat(string username)
    {
        EnsureViewer(username);
        using var conn = GetConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = "UPDATE viewers SET last_chat = $t WHERE username = $u;";
        cmd.Parameters.AddWithValue("$t", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("$u", username.ToLower());
        cmd.ExecuteNonQuery();
    }

    public List<ViewerRecord> GetAllViewers()
    {
        using var conn = GetConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM viewers;";

        var viewers = new List<ViewerRecord>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            viewers.Add(new ViewerRecord
            {
                Username    = reader.GetString(0),
                Points      = reader.GetInt32(1),
                SubTier     = (SubTier)reader.GetInt32(2),
                LastChat    = DateTime.Parse(reader.IsDBNull(3) ? DateTime.MinValue.ToString() : reader.GetString(3)),
                TotalEarned = reader.GetInt32(4),
                TotalSpent  = reader.GetInt32(5)
            });
        }
        return viewers;
    }
}