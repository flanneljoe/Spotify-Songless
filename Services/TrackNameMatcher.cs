public static class TrackNameMatcher
{
    // Patterns to strip from track titles before comparison
    private static readonly string[] StripPatterns = new[]
    {
        @"\(.*?remaster.*?\)",     // (2004 Remaster), (Remastered)
        @"\[.*?remaster.*?\]",     // [2004 Remaster]
        @"\(.*?deluxe.*?\)",       // (Deluxe Edition)
        @"\[.*?deluxe.*?\]",       // [Deluxe Edition]
        @"\(.*?bonus.*?\)",        // (Bonus Track)
        @"\(.*?live.*?\)",         // (Live at...)
        @"\[.*?live.*?\]",         // [Live at...]
        @"\(feat\..*?\)",          // (feat. Artist)
        @"\(ft\..*?\)",            // (ft. Artist)
        @"\(with.*?\)",            // (with Artist)
        @"\(.*?version.*?\)",      // (Acoustic Version), (Radio Version)
        @"\(.*?edit.*?\)",         // (Radio Edit)
        @"\(.*?mix.*?\)",          // (Extended Mix)
    };

    private static readonly string[] DashSuffixKeywords = new[]
    {
        "remaster", "remastered", "deluxe", "bonus", "live",
        "version", "edit", "mix", "anniversary", "expanded",
        "soundtrack", "ost", "original score"
    };

    public static string Normalize(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;

        var normalized = title.ToLowerInvariant();

        // Strip dash-separated metadata suffixes e.g. "Song Title - Year Remaster"
        var dashIndex = normalized.IndexOf(" - ");
        if (dashIndex > 0)
        {
            var suffix = normalized.Substring(dashIndex + 3);
            if (DashSuffixKeywords.Any(k => suffix.Contains(k)))
                normalized = normalized.Substring(0, dashIndex);
        }

        // Strip parenthetical and bracketed metadata
        foreach (var pattern in StripPatterns)
            normalized = System.Text.RegularExpressions.Regex.Replace(
                normalized, pattern, "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove remaining punctuation and collapse whitespace
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[^\w\s]", "");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ");

        return normalized.Trim();
    }



    public static string NormalizeStrict(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;

        var normalized = title.ToLowerInvariant();

        // Strip dash suffix if it contains metadata keywords (same as Normalize)
        var dashIndex = normalized.IndexOf(" - ");
        if (dashIndex > 0)
        {
            var suffix = normalized.Substring(dashIndex + 3);
            if (DashSuffixKeywords.Any(k => suffix.Contains(k)))
                normalized = normalized.Substring(0, dashIndex);
        }

        // Strip ALL parenthetical and bracketed content regardless of keywords
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\(.*?\)", "");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\[.*?\]", "");

        // Remove punctuation and collapse whitespace
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[^\w\s]", "");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ");

        return normalized.Trim();
    }

    public static bool IsMatch(string guess, string trackName)
    {
        var normalizedGuess = Normalize(guess);
        var normalizedTrack = Normalize(trackName);
        var strictTrack = NormalizeStrict(trackName);

        if (normalizedGuess == normalizedTrack) return true;
        if (normalizedGuess == strictTrack) return true;

        // Scale max distance with title length
        var maxDistance = Math.Clamp(normalizedTrack.Length / 8, 1, 4);

        if (LevenshteinDistance(normalizedGuess, normalizedTrack) <= maxDistance) return true;
        if (LevenshteinDistance(normalizedGuess, strictTrack) <= maxDistance) return true;

        return false;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;

        var dp = new int[a.Length + 1, b.Length + 1];

        for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) dp[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost
                );
            }
        }

        return dp[a.Length, b.Length];
    }
}