using StardewModdingAPI;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SDVChatVsStreamer.Twitch;

public class ClipService
{
    private readonly ModConfig  _config;
    private readonly IMonitor   _monitor;
    private readonly HttpClient _http = new();

    private DateTime _lastClipTime = DateTime.MinValue;
    private DateTime _clipCooldownUntil = DateTime.MinValue;

    public ClipService(ModConfig config, IMonitor monitor)
    {
        _config  = config;
        _monitor = monitor;
    }

    public void TryClipForTier(SDVChatVsStreamer.Sabotage.SabotageTier tier, string command, string triggeredBy, ModConfig config)
    {
        bool shouldClip = tier switch
        {
            SDVChatVsStreamer.Sabotage.SabotageTier.Nuisance    => config.ClipNuisance,
            SDVChatVsStreamer.Sabotage.SabotageTier.Disruptive  => config.ClipDisruptive,
            SDVChatVsStreamer.Sabotage.SabotageTier.Painful     => config.ClipPainful,
            SDVChatVsStreamer.Sabotage.SabotageTier.Devastating => config.ClipDevastating,
            SDVChatVsStreamer.Sabotage.SabotageTier.Blessing    => config.ClipBlessings,
            _                                                   => false
        };

        if (!shouldClip) return;

        int cooldown = tier switch
        {
            SDVChatVsStreamer.Sabotage.SabotageTier.Nuisance    => config.ClipNuisanceCooldownSeconds,
            SDVChatVsStreamer.Sabotage.SabotageTier.Disruptive  => config.ClipDisruptiveCooldownSeconds,
            SDVChatVsStreamer.Sabotage.SabotageTier.Painful     => config.ClipPainfulCooldownSeconds,
            SDVChatVsStreamer.Sabotage.SabotageTier.Devastating => config.ClipDevastatingCooldownSeconds,
            _                                                   => 120
        };

        TryClip(command, triggeredBy, cooldown);
    }

    /// <summary>
    /// Attempt to create a clip after the configured delay.
    /// </summary>
    public void TryClip(string sabotageCommand, string triggeredBy, int tierCooldownSeconds)
    {
        if (!_config.AutoClipEnabled) return;
        if (DateTime.UtcNow < _clipCooldownUntil)
        {
            _monitor.Log($"[ClipService] Skipping clip for {sabotageCommand} — on cooldown", LogLevel.Debug);
            return;
        }

        _clipCooldownUntil = DateTime.UtcNow.AddSeconds(tierCooldownSeconds);

        // Fire and forget — delay then create clip
        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(_config.ClipDelaySeconds));
            await CreateClipAsync(sabotageCommand, triggeredBy);
        });
    }

    private async Task CreateClipAsync(string sabotageCommand, string triggeredBy)
    {
        try
        {
            var token          = LoadToken();
            var broadcasterId  = _config.BroadcasterUserId;

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(broadcasterId)
                || broadcasterId == "your_broadcaster_user_id")
            {
                _monitor.Log("[ClipService] Cannot create clip — missing OAuth token or broadcaster ID", LogLevel.Warn);
                return;
            }

            var clientId = LoadClientId();
            if (string.IsNullOrWhiteSpace(clientId))
            {
                _monitor.Log("[ClipService] Cannot create clip — missing Client ID", LogLevel.Warn);
                return;
            }

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"https://api.twitch.tv/helix/clips?broadcaster_id={broadcasterId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Client-Id", clientId);

            var response = await _http.SendAsync(request);
            var json     = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var doc  = JsonDocument.Parse(json);
                var id   = doc.RootElement.GetProperty("data")[0].GetProperty("id").GetString();
                var url  = $"https://clips.twitch.tv/{id}";
                _monitor.Log($"[ClipService] Clip created for !buy {sabotageCommand} by {triggeredBy}: {url}", LogLevel.Info);

                // Set custom clip title
                await SetClipTitleAsync(id!, sabotageCommand, triggeredBy, token, clientId);
            }
            else
            {
                _monitor.Log($"[ClipService] Clip creation failed ({response.StatusCode}): {json}", LogLevel.Warn);
            }
        }
        catch (Exception ex)
        {
            _monitor.Log($"[ClipService] Clip error: {ex.Message}", LogLevel.Warn);
        }
    }

    private async Task SetClipTitleAsync(string clipId, string command, string triggeredBy, string token, string clientId)
    {
        try
        {
            // Twitch requires a short delay before editing a new clip
            await Task.Delay(3000);

            var title   = $"Chat vs Streamer — {triggeredBy} bought !buy {command}";
            if (title.Length > 100) title = title[..100]; // Twitch clip title max 100 chars

            var payload = JsonSerializer.Serialize(new { id = clipId, title });
            var request = new HttpRequestMessage(HttpMethod.Patch, "https://api.twitch.tv/helix/clips");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Client-Id", clientId);
            request.Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            if (response.IsSuccessStatusCode)
                _monitor.Log($"[ClipService] Clip title set: {title}", LogLevel.Debug);
            else
                _monitor.Log($"[ClipService] Clip title update failed ({response.StatusCode})", LogLevel.Warn);
        }
        catch (Exception ex)
        {
            _monitor.Log($"[ClipService] Clip title error: {ex.Message}", LogLevel.Warn);
        }
    }

    private string LoadToken()
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "StardewValley", "Mods", "ChatVsStreamer", _config.AuthConfigDirName, "secrets.json");
            if (!File.Exists(path)) return "";
            var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.TryGetProperty("OAuthToken", out var t) ? t.GetString() ?? "" : "";
        }
        catch { return ""; }
    }

    private string LoadClientId()
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "StardewValley", "Mods", "ChatVsStreamer", _config.AuthConfigDirName, "secrets.json");
            if (!File.Exists(path)) return "";
            var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.TryGetProperty("ClientId", out var c) ? c.GetString() ?? "" : "";
        }
        catch { return ""; }
    }
}