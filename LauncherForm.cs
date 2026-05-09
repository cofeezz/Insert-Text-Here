// Consult the GitHub Releases API to get the latest version.
// and the download URL for ProcessSuspender.exe

using System.Text.Json;
using System.Text.RegularExpressions;

record ReleaseInfo(string TagName, string DownloadUrl, string Body);

static class GitHubRelease
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    const string Owner = "cofeezz";
    const string Repo  = "ProcessSuspender";
    const string AssetName = "ProcessSuspender.exe";
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    static readonly string ApiUrl =
        $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest";

    /// <summary>
    /// Search for the most recent release. Returns null if offline or if there are no releases.
    /// </summary>
    public static async Task<ReleaseInfo?> FetchLatestAsync()
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "ProcessSuspender-Launcher");
        http.Timeout = TimeSpan.FromSeconds(10);

        try
        {
            var json = await http.GetStringAsync(ApiUrl);
            var doc  = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tag  = root.GetProperty("tag_name").GetString() ?? "";
            var body = root.GetProperty("body").GetString() ?? "";

            // Find the asset with the correct name.
            foreach (var asset in root.GetProperty("assets").EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? "";
                if (name.Equals(AssetName, StringComparison.OrdinalIgnoreCase))
                {
                    var url = asset.GetProperty("browser_download_url").GetString() ?? "";
                    return new ReleaseInfo(tag, url, body);
                }
            }
            return null; // The release exists but it's missing the expected asset.
        }
        catch
        {
            return null; // Offline or network error.
        }
    }

    /// <summary>
    /// Compares two semantic tags (e.g., "v1.2.0" vs "v1.3.0").
    /// Returns true if <paramref name="remote"/> is newer.
    /// </summary>
    public static bool IsNewer(string local, string remote)
    {
        static Version Parse(string tag)
        {
            var digits = Regex.Replace(tag, "[^0-9.]", "");
            return Version.TryParse(digits, out var v) ? v : new Version(0, 0);
        }
        return Parse(remote) > Parse(local);
    }
}
