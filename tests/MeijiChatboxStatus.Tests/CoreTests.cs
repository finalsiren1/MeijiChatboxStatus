using System.Globalization;
using MeijiChatboxStatus.Core;
using Xunit;

namespace MeijiChatboxStatus.Tests;

public class OscTests
{
    [Fact]
    public void ChatboxMessage_RoundTrips()
    {
        byte[] packet = Osc.Encode("/chatbox/input", "สวัสดี ✨", true, false);
        Assert.Equal(0, packet.Length % 4);

        var msg = Assert.Single(Osc.Decode(packet));
        Assert.Equal("/chatbox/input", msg.Address);
        Assert.Equal(new object?[] { "สวัสดี ✨", true, false }, msg.Args);
    }

    [Fact]
    public void EmptyString_IsPaddedWithTerminator()
    {
        // "" must still be encoded as four zero bytes, otherwise VRChat rejects the packet.
        byte[] packet = Osc.Encode("/chatbox/input", "", true, false);
        var msg = Assert.Single(Osc.Decode(packet));
        Assert.Equal("", msg.Args[0]);
    }

    [Theory]
    [InlineData(1.0f, true)]
    [InlineData(0.05f, false)]
    public void FloatParameter_UsesThreshold(float value, bool expected)
    {
        var msg = Assert.Single(Osc.Decode(Osc.Encode("/avatar/parameters/HeadPat", value)));
        Assert.Equal(expected, msg.AsBool(threshold: 0.1f));
    }

    [Fact]
    public void Bundle_IsUnpacked()
    {
        byte[] a = Osc.Encode("/avatar/parameters/A", true);
        byte[] b = Osc.Encode("/avatar/parameters/B", 3);
        using var ms = new MemoryStream();
        ms.Write("#bundle\0"u8);
        ms.Write(new byte[8]); // timetag
        foreach (var part in new[] { a, b })
        {
            var size = BitConverter.GetBytes(part.Length);
            Array.Reverse(size);
            ms.Write(size);
            ms.Write(part);
        }

        var msgs = Osc.Decode(ms.ToArray()).ToList();
        Assert.Equal(["/avatar/parameters/A", "/avatar/parameters/B"], msgs.Select(m => m.Address));
        Assert.Equal(3, msgs[1].Args[0]);
    }

    [Fact]
    public void MalformedPacket_YieldsNothing()
    {
        Assert.Empty(Osc.Decode([(byte)'/', (byte)'x', 1, 2, 3]));
        Assert.Empty(Osc.Decode([]));
    }
}

public class TextFormatTests
{
    [Fact]
    public void EmptyPlaceholders_RemoveTheirLines()
    {
        string result = TextFormat.Render("Hi ✨\n{afk}\n{music}", (n, _) => n switch { "afk" => "", "music" => "", _ => null });
        Assert.Equal("Hi ✨", result);
    }

    [Fact]
    public void MultiLinePlaceholder_Expands()
    {
        string result = TextFormat.Render("A\n{music}", (n, _) => n == "music" ? "🎵 Song\n━◉── 0:10/3:00" : null);
        Assert.Equal("A\n🎵 Song\n━◉── 0:10/3:00", result);
    }

    [Fact]
    public void DanglingSeparators_AreTrimmed()
    {
        string result = TextFormat.Render("🎵 {title} - {artist}", (n, _) => n == "title" ? "Song" : "");
        Assert.Equal("🎵 Song", result);
    }

    [Fact]
    public void UnknownPlaceholder_IsKeptVisible()
    {
        Assert.Equal("x {nope}", TextFormat.Render("x {nope}", (_, _) => null));
    }

    [Fact]
    public void PlaceholderArgument_IsPassed()
    {
        Assert.Equal("pats: 7", TextFormat.Render("pats: {count:HeadPat}", (n, a) => n == "count" && a == "HeadPat" ? "7" : null));
    }

    [Fact]
    public void Clamp_NeverSplitsEmojiOrThaiClusters()
    {
        string text = string.Concat(Enumerable.Repeat("🐰ที่", 60)); // surrogate pairs + Thai combining vowel
        string clamped = TextFormat.Clamp(text);
        Assert.True(clamped.Length <= TextFormat.MaxChars);
        Assert.EndsWith("…", clamped);
        // Every text element must be a complete one from the source.
        var e = StringInfo.GetTextElementEnumerator(clamped[..^1]);
        while (e.MoveNext()) Assert.Contains(e.GetTextElement(), new[] { "🐰", "ที่" });
    }

    [Fact]
    public void Clamp_LimitsLines()
    {
        string text = string.Join('\n', Enumerable.Range(1, 12));
        Assert.Equal(TextFormat.MaxLines, TextFormat.Clamp(text).Split('\n').Length);
    }

    [Theory]
    [InlineData("line", 0.0, "◉─────────")]
    [InlineData("line", 0.5, "━━━━━◉────")]
    [InlineData("line", 1.0, "━━━━━━━━━◉")]
    [InlineData("dots", 0.5, "●●●●●○○○○○")]
    [InlineData("classic", 0.0, "[o---------]")]
    public void ProgressBar_Styles(string style, double fraction, string expected)
    {
        Assert.Equal(expected, TextFormat.ProgressBar(style, 10, fraction));
    }

    [Fact]
    public void ProgressBar_HandlesNaN()
    {
        Assert.Equal("◉─────────", TextFormat.ProgressBar("line", 10, double.NaN));
    }

    [Theory]
    [InlineData(65, "1:05")]
    [InlineData(3725, "1:02:05")]
    public void Duration_Formats(int seconds, string expected)
    {
        Assert.Equal(expected, TextFormat.Duration(TimeSpan.FromSeconds(seconds)));
    }
}

public class SettingsTests
{
    [Fact]
    public void Normalize_ClampsAndRepairs()
    {
        var s = new AppSettings
        {
            Language = "jp",
            Pages = [],
            Counters = [new CounterRule { Parameter = "  HeadPat ", Seconds = 999, Count = -3 }],
        };
        s.Chatbox.IntervalSeconds = 0.1;
        s.Osc.SendPort = 70000;
        s.Normalize();

        Assert.Equal("th", s.Language);
        Assert.Single(s.Pages);
        Assert.Equal("HeadPat", s.Counters[0].Parameter);
        Assert.Equal(30, s.Counters[0].Seconds);
        Assert.Equal(0, s.Counters[0].Count);
        Assert.Equal(ChatboxSettings.MinInterval, s.Chatbox.IntervalSeconds);
        Assert.Equal(65535, s.Osc.SendPort);
    }
}

public class MediaFilterTests
{
    [Theory]
    [InlineData("Spotify.exe", true)]
    [InlineData("SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify", true)]
    [InlineData("AppleInc.AppleMusicWin_nzyj5cx40ttqa!App", true)]
    [InlineData("Microsoft.ZuneMusic_8wekyb3d8bbwe!Microsoft.ZuneMusic", true)]
    [InlineData("vlc.exe", false)]
    [InlineData("TikTok.exe", false)]
    public void MusicApps(string appId, bool expected) => Assert.Equal(expected, MediaFilter.IsMusicApp(appId, null));

    [Fact]
    public void ExtraSources_ExtendTheApps() => Assert.True(MediaFilter.IsMusicApp("vlc.exe", "foo, VLC"));

    [Theory]
    [InlineData("Chrome")]
    [InlineData("MSEdge")]
    [InlineData("308046B0AF4A39CB")] // Firefox reports a hash as its app id
    public void BrowserDetection(string appId) => Assert.True(MediaFilter.IsBrowser(appId));

    [Theory]
    [InlineData("(3) PORZAX - ยิ้มอันตราย (Official MV) - YouTube - Google Chrome", "PORZAX - ยิ้มอันตราย (Official MV)", "PORZAX", true)]
    [InlineData("Song A - YouTube Music - Personal - Microsoft Edge", "Song A", "Band", true)]
    [InlineData("Song A • Band - Google Chrome", "Song A", "Band", true)]        // Spotify web player
    [InlineData("Stream Song A by Band | Listen online for free on SoundCloud - Google Chrome", "Song A", "Band", true)]
    [InlineData("Song A | TikTok - Google Chrome", "Song A", "someone", false)]
    [InlineData("Song A - YouTubeFanPage | Facebook - Google Chrome", "Song A", "x", false)] // "YouTube" only as a word prefix
    [InlineData("Other video - YouTube - Google Chrome", "Song A", "Band", false)]          // music tab isn't the one playing
    [InlineData("หน้าแรก / X - Google Chrome", "หน้าแรก", "X", false)]
    public void BrowserTabs(string window, string title, string artist, bool expected) =>
        Assert.Equal(expected, MediaFilter.IsMusicTab(window, title, artist, null));
}
public class UpdaterTests
{
    private static string Release(string tag, string assetName = "MeijiChatboxStatus.exe", bool prerelease = false) => $$"""
        {
          "tag_name": "{{tag}}", "draft": false, "prerelease": {{(prerelease ? "true" : "false")}},
          "html_url": "https://github.com/finalsiren1/MeijiChatboxStatus/releases/tag/{{tag}}",
          "body": "## Changes\n- stuff",
          "assets": [
            { "name": "README.md", "size": 10, "browser_download_url": "https://example.invalid/readme" },
            { "name": "{{assetName}}", "size": 1234, "digest": "sha256:ABCDEF",
              "browser_download_url": "https://example.invalid/exe" }
          ]
        }
        """;

    [Fact]
    public void NewerRelease_IsReturnedWithAsset()
    {
        var r = Updater.ParseRelease(Release("v2.0.2"), new Version(2, 0, 1, 0));
        Assert.NotNull(r);
        Assert.Equal(new Version(2, 0, 2), r.Version);
        Assert.Equal("https://example.invalid/exe", r.AssetUrl);
        Assert.Equal(1234, r.AssetSize);
        Assert.Equal("abcdef", r.Sha256);
    }

    [Theory]
    [InlineData("v2.0.1")]
    [InlineData("v2.0.0")]
    [InlineData("2.0")]
    public void SameOrOlderRelease_IsIgnored(string tag) =>
        Assert.Null(Updater.ParseRelease(Release(tag), new Version(2, 0, 1, 0)));

    [Fact]
    public void Prerelease_IsIgnored() =>
        Assert.Null(Updater.ParseRelease(Release("v9.0.0", prerelease: true), new Version(2, 0, 1)));

    [Fact]
    public void ReleaseWithoutExe_IsAnError() =>
        Assert.Throws<InvalidDataException>(() => Updater.ParseRelease(Release("v9.0.0", "other.zip"), new Version(2, 0, 1)));
}
