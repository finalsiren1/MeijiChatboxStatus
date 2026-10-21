using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace MeijiChatboxStatus.Core;

/// <summary>A published release that is newer than the running app.</summary>
public sealed record ReleaseInfo(Version Version, string Tag, string PageUrl, string Notes, string AssetUrl, long AssetSize, string? Sha256);

/// <summary>
/// Checks GitHub Releases for a newer version, downloads its exe (size + SHA-256 verified) and hands over to it:
/// the downloaded copy runs <c>--install</c>, which replaces the installed exe and starts it again.
/// </summary>
public sealed class Updater : IDisposable
{
    public const string Repo = "finalsiren1/MeijiChatboxStatus";
    private const string LatestApi = $"https://api.github.com/repos/{Repo}/releases/latest";
    public static readonly TimeSpan CheckEvery = TimeSpan.FromHours(6);

    public static string DownloadDir { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeijiChatboxStatus", "updates");

#if FRAMEWORK_DEPENDENT
    private const string AssetName = "MeijiChatboxStatus-lite.exe";
#else
    private const string AssetName = "MeijiChatboxStatus.exe";
#endif
    private const string FallbackAssetName = "MeijiChatboxStatus.exe"; // self-contained: runs everywhere

    private readonly HttpClient _http;
    private readonly Logger _log;
    private CancellationTokenSource? _downloadCts;

    public Version Current { get; }
    /// <summary>idle · checking · latest · available · downloading · installing · error</summary>
    public string State { get; private set; } = "idle";
    public ReleaseInfo? Latest { get; private set; }
    public double Progress { get; private set; }
    public string? Error { get; private set; }
    public DateTime? CheckedAt { get; private set; }
    /// <summary>Raised on the caller's thread (the UI thread) whenever the state above changes.</summary>
    public event Action? Changed;

    public Updater(Version current, Logger log)
    {
        Current = current;
        _log = log;
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        // GitHub's API rejects requests without a User-Agent.
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MeijiChatboxStatus", current.ToString(3)));
    }

    /// <summary>Asks GitHub for the latest release. <paramref name="manual"/> = the user pressed the button (errors go to the log as errors).</summary>
    public async Task CheckAsync(bool manual)
    {
        if (State is "checking" or "downloading" or "installing") return;
        Set("checking");
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var req = new HttpRequestMessage(HttpMethod.Get, LatestApi);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            using var res = await _http.SendAsync(req, cts.Token);
            if (!res.IsSuccessStatusCode) throw new HttpRequestException($"GitHub {(int)res.StatusCode} {res.ReasonPhrase}");
            string json = await res.Content.ReadAsStringAsync(cts.Token);

            CheckedAt = DateTime.Now;
            Latest = ParseRelease(json, Current);
            if (Latest is null)
            {
                Set("latest");
                if (manual) _log.Info($"เป็นเวอร์ชันล่าสุดแล้ว (v{Current.ToString(3)})");
            }
            else
            {
                Set("available");
                _log.Info($"✨ มีเวอร์ชันใหม่ v{Latest.Version.ToString(3)} (ตอนนี้ v{Current.ToString(3)})");
            }
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or InvalidDataException)
        {
            string msg = e is TaskCanceledException ? "หมดเวลาเชื่อมต่อ GitHub" : e.Message;
            if (manual) _log.Error($"เช็คอัปเดตไม่สำเร็จ: {msg}");
            else _log.Warn($"เช็คอัปเดตอัตโนมัติไม่สำเร็จ (จะลองใหม่ภายหลัง): {msg}");
            Set("error", msg);
        }
    }

    /// <summary>
    /// Returns the release if it is newer than <paramref name="current"/> and has an exe for this build, otherwise null.
    /// </summary>
    public static ReleaseInfo? ParseRelease(string json, Version current)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.TryGetProperty("draft", out var d) && d.GetBoolean()) return null;
        if (root.TryGetProperty("prerelease", out var p) && p.GetBoolean()) return null;

        string tag = root.GetProperty("tag_name").GetString() ?? "";
        if (!Version.TryParse(tag.TrimStart('v', 'V'), out var version))
            throw new InvalidDataException($"Unrecognised release tag \"{tag}\"");
        version = new Version(version.Major, version.Minor, Math.Max(0, version.Build));
        if (version <= new Version(current.Major, current.Minor, Math.Max(0, current.Build))) return null;

        var list = root.TryGetProperty("assets", out var assets) ? assets.EnumerateArray().ToList() : [];
        var a = list.Find(x => x.GetProperty("name").GetString() == AssetName);
        if (a.ValueKind != JsonValueKind.Object) a = list.Find(x => x.GetProperty("name").GetString() == FallbackAssetName);
        if (a.ValueKind != JsonValueKind.Object) throw new InvalidDataException($"Release {tag} has no {AssetName}");

        string? sha = a.TryGetProperty("digest", out var dg) && dg.GetString() is { } digest && digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)
            ? digest[7..].ToLowerInvariant()
            : null;
        string notes = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
        return new ReleaseInfo(version, tag,
            root.GetProperty("html_url").GetString() ?? $"https://github.com/{Repo}/releases",
            notes.Length > 2000 ? notes[..2000] + "…" : notes,
            a.GetProperty("browser_download_url").GetString() ?? throw new InvalidDataException("Asset has no download URL"),
            a.GetProperty("size").GetInt64(),
            sha);
    }

    /// <summary>Downloads and verifies the new exe, then starts it in install mode. Returns true when the app should exit now.</summary>
    public async Task<bool> DownloadAndInstallAsync()
    {
        string? exe = await DownloadAsync();
        if (exe is null) return false;
        try
        {
            Set("installing");
            _log.Info($"ดาวน์โหลด v{Latest!.Version.ToString(3)} แล้ว กำลังติดตั้ง…");
            Process.Start(new ProcessStartInfo(exe, Installer.InstallArg) { UseShellExecute = false, WorkingDirectory = DownloadDir })?.Dispose();
            return true;
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or IOException)
        {
            _log.Error($"เปิดตัวติดตั้งไม่สำเร็จ: {e.Message}");
            Set("error", e.Message);
            return false;
        }
    }

    /// <summary>Downloads the latest release's exe and checks its size and SHA-256. Returns its path, or null on failure/cancel.</summary>
    public async Task<string?> DownloadAsync()
    {
        if (Latest is not { } release || State is "downloading" or "installing") return null;
        _downloadCts = new CancellationTokenSource();
        var ct = _downloadCts.Token;
        Progress = 0;
        Set("downloading");
        string target = Path.Combine(DownloadDir, $"MeijiChatboxStatus-{release.Version.ToString(3)}.exe");
        string partial = target + ".part";
        try
        {
            Directory.CreateDirectory(DownloadDir);
            using (var res = await _http.GetAsync(release.AssetUrl, HttpCompletionOption.ResponseHeadersRead, ct))
            {
                res.EnsureSuccessStatusCode();
                long total = res.Content.Headers.ContentLength ?? release.AssetSize;
                await using var src = await res.Content.ReadAsStreamAsync(ct);
                await using var dst = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
                using var sha = SHA256.Create();
                var buffer = new byte[81920];
                long done = 0;
                double lastReported = 0;
                int n;
                while ((n = await src.ReadAsync(buffer, ct)) > 0)
                {
                    await dst.WriteAsync(buffer.AsMemory(0, n), ct);
                    sha.TransformBlock(buffer, 0, n, null, 0);
                    done += n;
                    Progress = total > 0 ? Math.Min(1, (double)done / total) : 0;
                    if (Progress - lastReported >= 0.01) { lastReported = Progress; Changed?.Invoke(); }
                }
                sha.TransformFinalBlock([], 0, 0);

                if (done != release.AssetSize)
                    throw new InvalidDataException($"ไฟล์ไม่ครบ ({done:N0} / {release.AssetSize:N0} ไบต์)");
                string hash = Convert.ToHexString(sha.Hash!).ToLowerInvariant();
                if (release.Sha256 is not null && hash != release.Sha256)
                    throw new InvalidDataException("ไฟล์ที่ดาวน์โหลดไม่ตรงกับบน GitHub (SHA-256 ไม่ตรง)");
            }
            File.Move(partial, target, overwrite: true);
            return target;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            TryDelete(partial);
            _log.Info("ยกเลิกการอัปเดตแล้ว");
            Set("available");
            return null;
        }
        catch (Exception e) when (e is HttpRequestException or IOException or UnauthorizedAccessException
                                      or InvalidDataException or TaskCanceledException)
        {
            TryDelete(partial);
            string msg = e is TaskCanceledException ? "หมดเวลาดาวน์โหลด" : e.Message;
            _log.Error($"อัปเดตไม่สำเร็จ: {msg}");
            Set("error", msg);
            return null;
        }
        finally
        {
            _downloadCts?.Dispose();
            _downloadCts = null;
        }
    }

    public void CancelDownload() => _downloadCts?.Cancel();

    /// <summary>Removes installers left over from a finished update (the old one may still be exiting, so this runs later).</summary>
    public void CleanupDownloads()
    {
        if (!Directory.Exists(DownloadDir) || State is "downloading" or "installing") return;
        foreach (string file in Directory.GetFiles(DownloadDir))
        {
            try { File.Delete(file); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                _log.Warn($"ลบไฟล์อัปเดตเก่าไม่ได้ ({Path.GetFileName(file)}): {e.Message}");
            }
        }
    }

    private void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { _log.Warn($"ลบ {path} ไม่ได้: {e.Message}"); }
    }

    private void Set(string state, string? error = null)
    {
        State = state;
        Error = error;
        Changed?.Invoke();
    }

    public void Dispose()
    {
        _downloadCts?.Cancel();
        _http.Dispose();
    }
}
