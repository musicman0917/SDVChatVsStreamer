using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace SDVChatVsStreamer.UI;

public class ChatHud
{
    private readonly ModConfig _config;
    private readonly ChatFeed  _feed;
    private readonly IMonitor  _monitor;

    private Texture2D? _twitchIcon;
    private Texture2D? _tiktokIcon;

    private static readonly Color TwitchColor  = new Color(145, 70, 255);
    private static readonly Color TikTokColor  = new Color(255, 50, 80);
    private static readonly Color BgColor      = new Color(0, 0, 0, 160);
    private static readonly Color TextColor    = Color.White;

    private const int Padding    = 8;
    private const int LineHeight = 22;
    private const int BoxWidth   = 480;
    private const int IconSize   = 16;

    public ChatHud(ModConfig config, ChatFeed feed, IMonitor monitor, IModHelper helper)
    {
        _config  = config;
        _feed    = feed;
        _monitor = monitor;

        try
        {
            _twitchIcon = helper.ModContent.Load<Texture2D>("assets/twitch.png");
            _tiktokIcon = helper.ModContent.Load<Texture2D>("assets/tiktok.png");
        }
        catch
        {
            _monitor.Log("[ChatHud] Platform icons not found — using color badges instead.", LogLevel.Debug);
        }
    }

    public void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!_config.EnableChatOverlay) return;
        if (!Context.IsWorldReady)      return;

        // Filter to recent non-expired messages
        var now      = DateTime.UtcNow;
        var ttl      = TimeSpan.FromSeconds(_config.ChatOverlayMessageTTL);
        var messages = _feed.GetRecent(_config.ChatOverlayMaxMessages)
                            .Where(m => now - m.Timestamp < ttl)
                            .ToList();

        if (messages.Count == 0) return;

        var sb   = e.SpriteBatch;
        var font = Game1.smallFont;

        // Calculate total height accounting for wrapped lines
        int totalLines = messages.Sum(m =>
        {
            float nameW  = font.MeasureString($"{m.Username}: ").X * 0.75f;
            float msgW   = BoxWidth - Padding * 2 - 16 - nameW;
            return Math.Max(1, WrapText(font, m.Text, msgW, 0.75f).Count);
        });
        int boxH = totalLines * LineHeight + Padding * 2 + (messages.Count - 1) * 2;

        var pos = GetPosition(boxH);

        // Background
        sb.Draw(Game1.fadeToBlackRect,
            new Rectangle((int)pos.X, (int)pos.Y, BoxWidth, boxH),
            BgColor);

        // Messages
        int yOffset = Padding;
        for (int i = 0; i < messages.Count; i++)
        {
            var msg      = messages[i];
            var color    = msg.Platform == ChatPlatform.Twitch ? TwitchColor : TikTokColor;
            var icon     = msg.Platform == ChatPlatform.Twitch ? "T" : "♪";
            float lineY  = pos.Y + yOffset;

            // Platform icon — texture if loaded, colored badge fallback
            var badgeColor  = msg.Platform == ChatPlatform.Twitch ? TwitchColor : TikTokColor;
            var iconTexture = msg.Platform == ChatPlatform.Twitch ? _twitchIcon : _tiktokIcon;
            var iconRect    = new Rectangle((int)pos.X + Padding, (int)lineY + 1, IconSize, IconSize);

            if (iconTexture != null)
            {
                sb.Draw(iconTexture, iconRect, Color.White);
            }
            else
            {
                sb.Draw(Game1.fadeToBlackRect, iconRect, badgeColor);
                sb.DrawString(font,
                    msg.Platform == ChatPlatform.Twitch ? "T" : "K",
                    new Vector2(pos.X + Padding + 3, lineY),
                    Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 1f);
            }

            // Username + message as one string, color-coded name
            string nameText = $"{msg.Username}: ";
            sb.DrawString(font, nameText,
                new Vector2(pos.X + Padding + 16, lineY),
                color, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 1f);

            float nameWidth = font.MeasureString(nameText).X * 0.75f;
            float msgX      = pos.X + Padding + 16 + nameWidth;
            float maxMsgW   = BoxWidth - Padding * 2 - 16 - nameWidth;

            // Wrap message text
            var lines = WrapText(font, msg.Text, maxMsgW, 0.75f);
            for (int l = 0; l < lines.Count; l++)
            {
                float lx = l == 0 ? msgX : pos.X + Padding + 16;
                sb.DrawString(font, lines[l],
                    new Vector2(lx, lineY + l * LineHeight),
                    TextColor, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 1f);
            }

            yOffset += Math.Max(1, lines.Count) * LineHeight + 2;
        }
    }

    private Vector2 GetPosition(int boxHeight)
    {
        int vw = Game1.uiViewport.Width;
        int vh = Game1.uiViewport.Height;
        int margin = 16;

        return _config.ChatOverlayCorner switch
        {
            "TopLeft"     => new Vector2(margin, margin),
            "TopRight"    => new Vector2(vw - BoxWidth - margin, margin),
            "BottomRight" => new Vector2(vw - BoxWidth - margin, vh - boxHeight - margin),
            _             => new Vector2(margin, vh - boxHeight - margin) // BottomLeft default
        };
    }

    private static List<string> WrapText(SpriteFont font, string text, float maxWidth, float scale)
    {
        var result = new List<string>();
        var line   = new System.Text.StringBuilder();

        foreach (var word in text.Split(' '))
        {
            // Force-break words that are wider than maxWidth on their own
            var remaining = word;
            while (remaining.Length > 0)
            {
                // Find how many chars of 'remaining' fit
                int fit = remaining.Length;
                while (fit > 0 && font.MeasureString(remaining[..fit]).X * scale > maxWidth)
                    fit--;

                if (fit == 0) fit = 1; // always advance at least 1 char

                var chunk = remaining[..fit];
                remaining = remaining[fit..];

                var test = line.Length == 0 ? chunk : line + " " + chunk;
                if (font.MeasureString(test).X * scale > maxWidth && line.Length > 0)
                {
                    result.Add(line.ToString());
                    line.Clear();
                    line.Append(chunk);
                }
                else
                {
                    if (line.Length > 0 && remaining.Length == 0 && word != chunk)
                        line.Append(chunk);
                    else
                    {
                        if (line.Length > 0) line.Append(' ');
                        line.Append(chunk);
                    }
                }

                // If more of this word remains, flush line
                if (remaining.Length > 0 && line.Length > 0)
                {
                    result.Add(line.ToString());
                    line.Clear();
                }
            }
        }

        if (line.Length > 0) result.Add(line.ToString());
        return result.Count > 0 ? result : new List<string> { "" };
    }
}