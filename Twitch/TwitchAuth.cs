using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using StardewModdingAPI;
using StardewValley;

namespace SDVChatVsStreamer.Twitch;

public static class TwitchAuth
{
    public static string ClientId { get; private set; } = "";

    private static string _configDir    = "";
    private static string _secretsPath => Path.Combine(_configDir, "secrets.json");
    private static string _tokenPath   => Path.Combine(_configDir, "token.dat");
    private static IMonitor _monitor    = null!;

    private const string RedirectUri = "https://localhost";
    private const string Scopes      =
        "chat:read " +
        "chat:edit " +
        "moderator:read:chatters " +
        "channel:read:subscriptions " +
        "channel:read:redemptions " +
        "bits:read " +
        "moderator:read:followers";

    public static bool HasToken   => LoadToken() != null;
    public static bool HasSecrets => !string.IsNullOrEmpty(ClientId);

    public static bool _pendingAuth = false;
    private static Action<string>? _pendingSuccess;
    private static Action<string>? _pendingError;

    // ─── Init ─────────────────────────────────────────────────────────────────

    public static void Init(string configDir, IMonitor monitor)
    {
        _configDir = configDir;
        _monitor   = monitor;
        Directory.CreateDirectory(configDir);
        LoadSecrets();
        _monitor.Log($"[TwitchAuth] Init. Config dir: {_configDir}", LogLevel.Debug);
    }

    // ─── Secrets ──────────────────────────────────────────────────────────────

    public static void LoadSecrets()
    {
        if (!File.Exists(_secretsPath)) { CreateSecretsTemplate(); return; }
        try
        {
            var json = File.ReadAllText(_secretsPath);
            ClientId = ExtractJsonString(json, "client_id");
            _monitor.Log($"[TwitchAuth] Loaded Client ID: {ClientId.Substring(0, Math.Min(6, ClientId.Length))}...", LogLevel.Debug);
        }
        catch (Exception ex)
        {
            _monitor.Log($"[TwitchAuth] Failed to read secrets.json: {ex.Message}", LogLevel.Error);
        }
    }

    private static void CreateSecretsTemplate()
    {
        File.WriteAllText(_secretsPath, "{\n  \"client_id\": \"PASTE_YOUR_CLIENT_ID_HERE\"\n}\n");
        _monitor.Log($"[TwitchAuth] Created secrets.json template at: {_secretsPath}", LogLevel.Warn);
    }

    // ─── OAuth Flow ───────────────────────────────────────────────────────────

    public static void StartAuth(Action<string> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(ClientId))
        {
            onError?.Invoke($"Client ID not set. Edit: {_secretsPath}");
            return;
        }

        string state = Guid.NewGuid().ToString("N");

        string url = "https://id.twitch.tv/oauth2/authorize"
                   + $"?client_id={ClientId}"
                   + $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}"
                   + "&response_type=token"
                   + $"&scope={Uri.EscapeDataString(Scopes)}"
                   + $"&state={state}"
                   + "&force_verify=false";

        _monitor.Log("[TwitchAuth] Opening browser for Twitch authorization...", LogLevel.Info);
        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });

        Game1.addHUDMessage(new HUDMessage(
            "🟣 Authorize on Twitch — then press F9 to paste your token.",
            HUDMessage.newQuest_type));

        _pendingAuth    = true;
        _pendingSuccess = onSuccess;
        _pendingError   = onError;
    }

    // ─── Token Submission ─────────────────────────────────────────────────────

    public static void SubmitToken(string raw)
    {
        _pendingAuth = false;
        string token = raw.Trim();

        _monitor.Log($"[TwitchAuth] Raw input length={token.Length}", LogLevel.Debug);

        if (token.Contains("#access_token="))
        {
            int idx = token.IndexOf("#access_token=") + "#access_token=".Length;
            int end = token.IndexOf('&', idx);
            token   = end > 0 ? token.Substring(idx, end - idx) : token.Substring(idx);
        }
        else if (token.Contains("access_token="))
        {
            int idx = token.IndexOf("access_token=") + "access_token=".Length;
            int end = token.IndexOf('&', idx);
            token   = end > 0 ? token.Substring(idx, end - idx) : token.Substring(idx);
        }
        else if (token.StartsWith("oauth:"))
        {
            token = token.Substring(6);
        }

        token = Uri.UnescapeDataString(token).Trim();

        if (token.Length < 20 || token.Length > 60 ||
            !System.Text.RegularExpressions.Regex.IsMatch(token, @"^[a-zA-Z0-9]+$"))
        {
            Game1.addHUDMessage(new HUDMessage(
                "❌ Invalid token. Copy the full URL from the browser and try F9 again.",
                HUDMessage.error_type));
            _pendingError?.Invoke("Invalid token format.");
            return;
        }

        SaveToken(token);
        _pendingSuccess?.Invoke(token);
    }

    // ─── Token Storage ────────────────────────────────────────────────────────

    public static void SaveToken(string token)
    {
        try
        {
            byte[] key = GetMachineKey();
            byte[] iv  = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(iv);
            byte[] plain = Encoding.UTF8.GetBytes(token);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV  = iv;

            using var enc = aes.CreateEncryptor();
            using var ms  = new MemoryStream();
            ms.Write(iv, 0, iv.Length);
            using (var cs = new CryptoStream(ms, enc, CryptoStreamMode.Write))
                cs.Write(plain, 0, plain.Length);

            File.WriteAllBytes(_tokenPath, ms.ToArray());
            _monitor.Log("[TwitchAuth] Token saved (encrypted).", LogLevel.Info);
        }
        catch (Exception ex)
        {
            _monitor.Log($"[TwitchAuth] Failed to save token: {ex.Message}", LogLevel.Error);
        }
    }

    public static string? LoadToken()
    {
        if (!File.Exists(_tokenPath)) return null;
        try
        {
            byte[] cipher = File.ReadAllBytes(_tokenPath);
            byte[] key    = GetMachineKey();
            byte[] iv     = new byte[16];
            Buffer.BlockCopy(cipher, 0, iv, 0, 16);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV  = iv;

            using var dec = aes.CreateDecryptor();
            using var ms  = new MemoryStream(cipher, 16, cipher.Length - 16);
            using var cs  = new CryptoStream(ms, dec, CryptoStreamMode.Read);
            using var sr  = new StreamReader(cs);
            return sr.ReadToEnd();
        }
        catch
        {
            _monitor.Log("[TwitchAuth] Could not decrypt token — re-auth required.", LogLevel.Warn);
            return null;
        }
    }

    public static void ClearToken()
    {
        if (File.Exists(_tokenPath)) File.Delete(_tokenPath);
        _monitor.Log("[TwitchAuth] Token cleared.", LogLevel.Info);
    }

    // ─── Machine Key ──────────────────────────────────────────────────────────

    private static byte[] GetMachineKey()
    {
        string seed = Environment.MachineName + Environment.UserName + "NOM_SDV_TWITCH";
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(seed));
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string ExtractJsonString(string json, string key)
    {
        string search = $"\"{key}\"";
        int ki    = json.IndexOf(search);            if (ki < 0) return "";
        int colon = json.IndexOf(':', ki + search.Length); if (colon < 0) return "";
        int q1    = json.IndexOf('"', colon + 1);   if (q1 < 0) return "";
        int q2    = json.IndexOf('"', q1 + 1);      if (q2 < 0) return "";
        return json.Substring(q1 + 1, q2 - q1 - 1);
    }
}