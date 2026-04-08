using System.IO.Compression;
using System.Reflection;
using System.Text.Json;

namespace CafeteriaNotifier;

internal static class AutoUpdater
{
    const string LatestReleaseUrl = "https://api.github.com/repos/kripet-bouvet/compass-group-cafeteria-notifier/releases/latest";

    internal static async Task CheckAndApplyUpdateAsync()
    {
        string? exePath = Environment.ProcessPath;
        if (exePath is null) return;

        string newExePath = exePath + ".new";
        string oldExePath = exePath + ".old";

        if (File.Exists(oldExePath))
            File.Delete(oldExePath);

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CafeteriaNotifier-AutoUpdater");

            var response = await client.GetAsync(LatestReleaseUrl);
            if (!response.IsSuccessStatusCode) return;

            using var releaseDoc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = releaseDoc.RootElement;

            if (!root.TryGetProperty("tag_name", out var tagElement)) return;
            if (!Version.TryParse(tagElement.GetString()?.TrimStart('v'), out var latestVersion)) return;

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            if (currentVersion is null || latestVersion <= currentVersion) return;

            if (!root.TryGetProperty("assets", out var assetsElement)) return;
            string? downloadUrl = null;
            foreach (var asset in assetsElement.EnumerateArray())
            {
                if (asset.TryGetProperty("name", out var nameEl) &&
                    nameEl.GetString()?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true &&
                    asset.TryGetProperty("browser_download_url", out var urlEl))
                {
                    downloadUrl = urlEl.GetString();
                    break;
                }
            }
            if (downloadUrl is null) return;

            var zipBytes = await client.GetByteArrayAsync(downloadUrl);
            using var zipStream = new MemoryStream(zipBytes);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            var exeEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
            if (exeEntry is null) return;

            using (var entryStream = exeEntry.Open())
            using (var fileStream = File.Create(newExePath))
                await entryStream.CopyToAsync(fileStream);

            File.Move(exePath, oldExePath);
            File.Move(newExePath, exePath);
            // .old will be deleted on the next run
        }
        catch
        {
            if (File.Exists(newExePath)) File.Delete(newExePath);
            if (File.Exists(oldExePath) && !File.Exists(exePath))
                File.Move(oldExePath, exePath);
        }
    }
}
