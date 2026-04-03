using System.Text;
using System.Text.RegularExpressions;

namespace SDVChatVsStreamer.UI;

public static class ContentFilter
{
    // Leet-speak substitution map
    private static readonly Dictionary<char, char> LeetMap = new()
    {
        { '0', 'o' },
        { '1', 'i' },
        { '!', 'i' },
        { '3', 'e' },
        { '4', 'a' },
        { '5', 's' },
        { '7', 't' },
        { '@', 'a' },
        { '$', 's' },
        { '+', 't' },
        { '|', 'i' },
    };

    // Hard-coded slurs that are always blocked regardless of config
    private static readonly string[] HardBlockedSlurs =
    {
        // Anti-Black
        "nigger", "nigga", "coon", "spook", "sambo", "spade", "jigaboo", "porch monkey",
        "spearchucker", "darkie", "darky", "pickaninny", "jungle bunny", "tar baby",

        // Anti-Latino
        "spic", "wetback", "beaner", "greaser",

        // Anti-Asian
        "chink", "gook", "slant", "slope", "zipperhead", "nip", "jap", "chinaman",
        "ching chong", "yellow", "zippy",

        // Anti-Jewish
        "kike", "yid", "heeb", "hymie", "sheeny", "jewboy",

        // Anti-Arab / Anti-Muslim
        "raghead", "towelhead", "sand nigger", "camel jockey", "jihadi",

        // Anti-Gay / Anti-Trans
        "faggot", "fag", "dyke", "tranny", "shemale", "heshe", "queer",

        // Anti-White (slurs)
        "cracker", "honky", "redneck",

        // Anti-Indigenous
        "redskin", "injun", "squaw",

        // Anti-South Asian
        "paki", "dothead", "curry muncher",

        // Anti-Irish / Anti-Italian / Other European
        "mick", "paddy", "wop", "dago", "guinea", "kraut", "frog", "limey",

        // Ableist
        "retard", "retarded", "spaz", "spastic",
    };

    private static readonly Regex NoisePattern = new(
        @"[^a-z0-9]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Normalizes text through unicode decomposition, noise stripping, and leet-speak
    /// substitution to produce a clean comparable string.
    /// </summary>
    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";

        // Step 1: NFKD unicode normalization — converts bold/italic/fancy unicode
        // characters like 𝐧 𝕟 ｎ into their standard Latin equivalents
        var normalized = input.Normalize(NormalizationForm.FormKD);

        // Step 2: Keep only ASCII characters (drops combining diacritics, zero-width chars, etc.)
        var ascii = new StringBuilder();
        foreach (var ch in normalized)
            if (ch < 128) ascii.Append(ch);

        // Step 3: Leet-speak substitution
        var leet = new StringBuilder();
        foreach (var ch in ascii.ToString().ToLower())
            leet.Append(LeetMap.TryGetValue(ch, out var mapped) ? mapped : ch);

        // Step 4: Strip all remaining non-alphanumeric characters
        // (dots, underscores, spaces, emojis, invisible separators)
        return NoisePattern.Replace(leet.ToString(), "");
    }

    /// <summary>
    /// Returns true if the message contains a hard-blocked slur or any of the
    /// caller-supplied blocked keywords, using the normalization pipeline.
    /// </summary>
    public static bool IsBlocked(string message, string[] additionalKeywords)
    {
        var clean    = Normalize(message);
        var original = message.ToLower();

        // Always-blocked slurs
        foreach (var slur in HardBlockedSlurs)
        {
            var cleanSlur = Normalize(slur);
            // Check normalized (catches leet/unicode evasion)
            if (clean.Contains(cleanSlur, StringComparison.Ordinal)) return true;
            // Also check original for multi-word slurs (spaces preserved)
            if (slur.Contains(' ') && original.Contains(slur, StringComparison.OrdinalIgnoreCase)) return true;
        }

        // Configurable keywords (also normalized for consistency)
        foreach (var kw in additionalKeywords)
        {
            if (string.IsNullOrWhiteSpace(kw)) continue;
            var cleanKw = Normalize(kw);
            if (clean.Contains(cleanKw, StringComparison.Ordinal)) return true;
            if (kw.Contains(' ') && original.Contains(kw, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }
}