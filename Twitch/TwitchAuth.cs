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

    private const string RedirectUri = "http://localhost:7379/";
    private const string Scopes      =
        "chat:read " +
        "chat:edit " +
        "moderator:read:chatters " +
        "channel:read:subscriptions " +
        "channel:read:redemptions " +
        "bits:read " +
        "clips:edit " +
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
            var json      = File.ReadAllText(_secretsPath);
            ClientId      = ExtractJsonString(json, "client_id");
            _clientSecret = ExtractJsonString(json, "client_secret");
            var refresh   = ExtractJsonString(json, "RefreshToken");
            if (!string.IsNullOrWhiteSpace(refresh))
                SaveRefreshToken(refresh);
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

    private static string _clientSecret = "";
    private static string _state        = "";

    // ─── OAuth Flow ───────────────────────────────────────────────────────────

    public static void StartAuth(Action<string> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(ClientId))
        {
            onError?.Invoke($"Client ID not set. Edit: {_secretsPath}");
            return;
        }

        _state = Guid.NewGuid().ToString("N");

        string url = "https://id.twitch.tv/oauth2/authorize"
                   + $"?client_id={ClientId}"
                   + $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}"
                   + "&response_type=code"
                   + $"&scope={Uri.EscapeDataString(Scopes)}"
                   + $"&state={_state}"
                   + "&force_verify=false";

        _pendingAuth    = true;
        _pendingSuccess = onSuccess;
        _pendingError   = onError;

        // Start local HTTP listener to catch the auth code
        Task.Run(() => ListenForAuthCode(onSuccess, onError));

        _monitor.Log("[TwitchAuth] Opening browser for Twitch authorization (code flow)...", LogLevel.Info);

        // Try to open in Chrome incognito so bot account auth doesn't conflict with main account
        bool opened = false;
        foreach (var chrome in new[] {
            @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
            Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe")
        })
        {
            if (!File.Exists(chrome)) continue;
            Process.Start(new ProcessStartInfo
            {
                FileName         = chrome,
                Arguments        = $"--incognito \"{url}\"",
                UseShellExecute  = true
            });
            opened = true;
            break;
        }

        if (!opened)
        {
            // Fall back to default browser if Chrome not found
            _monitor.Log("[TwitchAuth] Chrome not found — opening in default browser. Sign in as bardbouncerbot!", LogLevel.Warn);
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }

        Game1.addHUDMessage(new HUDMessage(
            "🟣 Authorize on Twitch in your browser — the mod will handle the rest.",
            HUDMessage.newQuest_type));
    }

    private static async Task ListenForAuthCode(Action<string> onSuccess, Action<string> onError)
    {
        try
        {
            using var listener = new System.Net.HttpListener();
            listener.Prefixes.Add("http://localhost:7379/");
            listener.Start();
            _monitor.Log("[TwitchAuth] Listening for OAuth callback on http://localhost:7379/", LogLevel.Info);

            var context  = await listener.GetContextAsync();
            var query    = context.Request.QueryString;
            var code     = query["code"]  ?? "";
            var state    = query["state"] ?? "";
            var error    = query["error"] ?? "";

            // Send a response to the browser
            var response = context.Response;
            string html  = error.Length > 0
                ? "<html><body><h2>❌ Auth failed. You can close this tab.</h2></body></html>"
                : "<html><body><h2>✅ Authorized! You can close this tab and return to Stardew Valley.</h2></body></html>";
            var buffer = Encoding.UTF8.GetBytes(html);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            listener.Stop();

            if (!string.IsNullOrEmpty(error))
            {
                onError?.Invoke($"Auth error: {error}");
                return;
            }

            if (state != _state)
            {
                onError?.Invoke("State mismatch — possible CSRF. Please try again.");
                return;
            }

            // Exchange code for token
            await ExchangeCodeForToken(code, onSuccess, onError);
        }
        catch (Exception ex)
        {
            _monitor.Log($"[TwitchAuth] OAuth listener error: {ex.Message}", LogLevel.Error);
            onError?.Invoke(ex.Message);
        }
    }

    private static async Task ExchangeCodeForToken(string code, Action<string> onSuccess, Action<string> onError)
    {
        try
        {
            using var http = new System.Net.Http.HttpClient();
            var body = new System.Net.Http.FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("client_id",     ClientId),
                new KeyValuePair<string,string>("client_secret", _clientSecret),
                new KeyValuePair<string,string>("code",          code),
                new KeyValuePair<string,string>("grant_type",    "authorization_code"),
                new KeyValuePair<string,string>("redirect_uri",  RedirectUri),
            });

            var resp = await http.PostAsync("https://id.twitch.tv/oauth2/token", body);
            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _monitor.Log($"[TwitchAuth] Token exchange failed: {json}", LogLevel.Error);
                onError?.Invoke($"Token exchange failed: {json}");
                return;
            }

            using var doc       = System.Text.Json.JsonDocument.Parse(json);
            var accessToken     = doc.RootElement.GetProperty("access_token").GetString()  ?? "";
            var refreshToken    = doc.RootElement.GetProperty("refresh_token").GetString() ?? "";

            SaveToken(accessToken);
            SaveRefreshToken(refreshToken);

            _monitor.Log("[TwitchAuth] Authorization Code flow complete — tokens saved.", LogLevel.Info);
            _pendingAuth = false;
            onSuccess?.Invoke(accessToken);

            Game1.addHUDMessage(new HUDMessage(
                "✅ Twitch authorized successfully! Auto-refresh enabled.",
                HUDMessage.newQuest_type));
        }
        catch (Exception ex)
        {
            _monitor.Log($"[TwitchAuth] Token exchange error: {ex.Message}", LogLevel.Error);
            onError?.Invoke(ex.Message);
        }
    }

    // ─── Token Submission (legacy — no longer needed with code flow) ──────────

    public static void SubmitToken(string raw)
    {
        // No longer used — Authorization Code flow handles this automatically
        _monitor.Log("[TwitchAuth] F9 token submission is no longer needed. Auth is handled automatically.", LogLevel.Info);
        Game1.addHUDMessage(new HUDMessage(
            "ℹ️ Token submission is automatic now — no need to press F9.",
            HUDMessage.newQuest_type));
    }

    private static string _refreshTokenPath => Path.Combine(_configDir, "refresh.dat");

    public static void SaveRefreshToken(string token)
    {
        try
        {
            byte[] key   = GetMachineKey();
            byte[] iv    = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(iv);
            byte[] plain = Encoding.UTF8.GetBytes(token);

            using var aes = Aes.Create();
            aes.Key = key; aes.IV = iv;
            using var enc = aes.CreateEncryptor();
            using var ms  = new MemoryStream();
            ms.Write(iv, 0, iv.Length);
            using (var cs = new CryptoStream(ms, enc, CryptoStreamMode.Write))
                cs.Write(plain, 0, plain.Length);
            File.WriteAllBytes(_refreshTokenPath, ms.ToArray());
            _monitor.Log("[TwitchAuth] Refresh token saved.", LogLevel.Debug);
        }
        catch (Exception ex)
        {
            _monitor.Log($"[TwitchAuth] Failed to save refresh token: {ex.Message}", LogLevel.Error);
        }
    }

    private static string? LoadRefreshToken()
    {
        // Try encrypted file first
        if (File.Exists(_refreshTokenPath))
        {
            try
            {
                byte[] cipher = File.ReadAllBytes(_refreshTokenPath);
                byte[] key    = GetMachineKey();
                byte[] iv     = new byte[16];
                Buffer.BlockCopy(cipher, 0, iv, 0, 16);
                using var aes = Aes.Create();
                aes.Key = key; aes.IV = iv;
                using var dec = aes.CreateDecryptor();
                using var ms  = new MemoryStream(cipher, 16, cipher.Length - 16);
                using var cs  = new CryptoStream(ms, dec, CryptoStreamMode.Read);
                using var sr  = new StreamReader(cs);
                return sr.ReadToEnd();
            }
            catch { }
        }

        // Fall back to secrets.json RefreshToken field
        if (File.Exists(_secretsPath))
        {
            try
            {
                var json = File.ReadAllText(_secretsPath);
                var token = ExtractJsonString(json, "RefreshToken");
                if (!string.IsNullOrWhiteSpace(token)) return token;
            }
            catch { }
        }

        return null;
    }

    public static async Task<string?> RefreshAccessTokenAsync()
    {
        var refreshToken = LoadRefreshToken();
        if (string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(ClientId) || string.IsNullOrWhiteSpace(_clientSecret))
        {
            _monitor.Log("[TwitchAuth] Cannot refresh — missing refresh token, client ID, or client secret.", LogLevel.Warn);
            return null;
        }

        try
        {
            using var http = new System.Net.Http.HttpClient();
            var body = new System.Net.Http.FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("grant_type",    "refresh_token"),
                new KeyValuePair<string,string>("refresh_token", refreshToken),
                new KeyValuePair<string,string>("client_id",     ClientId),
                new KeyValuePair<string,string>("client_secret", _clientSecret),
            });

            var resp = await http.PostAsync("https://id.twitch.tv/oauth2/token", body);
            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _monitor.Log($"[TwitchAuth] Token refresh failed: {json}", LogLevel.Warn);
                return null;
            }

            using var doc       = System.Text.Json.JsonDocument.Parse(json);
            var newAccessToken  = doc.RootElement.GetProperty("access_token").GetString()  ?? "";
            var newRefreshToken = doc.RootElement.TryGetProperty("refresh_token", out var rt)
                                  ? rt.GetString() ?? refreshToken : refreshToken;

            SaveToken(newAccessToken);
            SaveRefreshToken(newRefreshToken);
            _monitor.Log("[TwitchAuth] Token refreshed successfully.", LogLevel.Info);
            return newAccessToken;
        }
        catch (Exception ex)
        {
            _monitor.Log($"[TwitchAuth] Token refresh error: {ex.Message}", LogLevel.Error);
            return null;
        }
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