using SDVChatVsStreamer.Economy;
using SDVChatVsStreamer.Sabotage;
using StardewModdingAPI;
using System.Text.Json;

namespace SDVChatVsStreamer.DonorDrive;

/// <summary>
/// Polls a DonorDrive-powered donation page (Extra Life, etc.) and turns each new
/// donation into chaos points plus a random sabotage/blessing scaled by donation size —
/// points follow the same cents-to-points shape as bits, and the size-scaled random
/// event follows the same escalating-tier idea as raids.
/// </summary>
public class DonorDriveManager
{
    private readonly ModConfig _config;
    private readonly PointsEngine _points;
    private readonly SabotageEngine _sabotage;
    private readonly IMonitor _monitor;
    private readonly HttpClient _http = new();

    private readonly HashSet<string> _seenDonationIds = new();
    private bool _initialized = false;
    private bool _gameActive  = false;
    private bool _running     = false;

    private Action<string>? _sendChatMessage;

    public DonorDriveManager(ModConfig config, PointsEngine points, SabotageEngine sabotage, IMonitor monitor)
    {
        _config   = config;
        _points   = points;
        _sabotage = sabotage;
        _monitor  = monitor;
    }

    public void SetChatSender(Action<string> sender) => _sendChatMessage = sender;
    public void SetGameActive(bool active) => _gameActive = active;

    public void Start()
    {
        if (_running) return;
        _running = true;
        Task.Run(PollLoop);
    }

    private async Task PollLoop()
    {
        while (_running)
        {
            try
            {
                if (_config.DonorDriveEnabled && !string.IsNullOrWhiteSpace(_config.DonorDriveParticipantId))
                    await PollOnce();
            }
            catch (Exception ex)
            {
                _monitor.Log($"[DonorDriveManager] Poll failed: {ex.Message}", LogLevel.Warn);
            }

            // DonorDrive asks integrators to limit requests to one every 15 seconds
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(15, _config.DonorDrivePollSeconds)));
        }
    }

    private async Task PollOnce()
    {
        var url  = $"{_config.DonorDriveApiBaseUrl.TrimEnd('/')}/api/participants/{_config.DonorDriveParticipantId}/donations";
        var json = await _http.GetStringAsync(url);

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array) return;

        // The first poll just baselines whatever donation history already exists,
        // so the mod doesn't replay someone's entire campaign as "new" on startup.
        if (!_initialized)
        {
            foreach (var donation in doc.RootElement.EnumerateArray())
                _seenDonationIds.Add(GetDonationId(donation));
            _initialized = true;
            _monitor.Log($"[DonorDriveManager] Baselined {_seenDonationIds.Count} existing donations.", LogLevel.Info);
            return;
        }

        foreach (var donation in doc.RootElement.EnumerateArray())
        {
            var id = GetDonationId(donation);
            if (string.IsNullOrEmpty(id) || !_seenDonationIds.Add(id)) continue;

            var amount = GetAmount(donation);
            if (amount <= 0) continue;

            HandleDonation(GetDisplayName(donation), amount);
        }
    }

    private void HandleDonation(string username, double amount)
    {
        _points.OnDonation(username, amount);

        var tier   = GetTier(amount);
        var points = (int)Math.Round(amount * 100) * _config.DonationPointsPerCent;
        _monitor.Log($"[DonorDriveManager] {username} donated ${amount:F2} — tier {tier}, +{points}pts", LogLevel.Info);

        _sendChatMessage?.Invoke($"💸 {username} just donated ${amount:F2}! Thank you! (+{points}pts)");

        if (_gameActive)
            _sabotage.TriggerDonationEvent(username, tier, amount);
    }

    private DonationTier GetTier(double amount) => amount switch
    {
        _ when amount >= _config.DonationMassiveThreshold => DonationTier.Massive,
        _ when amount >= _config.DonationLargeThreshold   => DonationTier.Large,
        _ when amount >= _config.DonationMediumThreshold  => DonationTier.Medium,
        _ => DonationTier.Small
    };

    // DonorDrive's public donation JSON shape varies a little by org/site version,
    // so these read a few likely property names defensively instead of assuming one.
    private static string GetDonationId(JsonElement donation)
    {
        foreach (var name in new[] { "donationID", "donationId", "id" })
            if (donation.TryGetProperty(name, out var v))
                return v.ToString() ?? "";
        return donation.GetRawText().GetHashCode().ToString();
    }

    private static double GetAmount(JsonElement donation)
    {
        foreach (var name in new[] { "amount", "donationAmount" })
            if (donation.TryGetProperty(name, out var v) && v.TryGetDouble(out var d))
                return d;
        return 0;
    }

    private static string GetDisplayName(JsonElement donation)
    {
        foreach (var name in new[] { "displayName", "donorName", "name" })
            if (donation.TryGetProperty(name, out var v))
            {
                var s = v.GetString();
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }
        return "Anonymous";
    }
}
