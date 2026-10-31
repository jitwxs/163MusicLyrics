using System.Net;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Core.Service.Translate;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;

namespace MusicLyricAppTest.Regression;

public class CacheAndSettingsRegressionTests
{
    [Fact]
    public void ConfigRoundTripPreservesNestedSettings()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.ThemeMode = ThemeModeEnum.DARK;
        ws.Settings.Config.NetworkProxyMode = NetworkProxyModeEnum.HTTP_PROXY;
        ws.Settings.Config.ProxyHost = "127.0.0.1";
        ws.Settings.Config.ProxyPort = 8765;
        ws.Settings.Config.TransConfig.MatchPrecisionDeviation = 25;
        ws.Settings.Config.OutputLyricTypes = "4,0,1";
        ws.Settings.Param.Encoding = OutputEncodingEnum.UTF_8_BOM;
        ws.Settings.Param.ShowLrcType = ShowLrcTypeEnum.MERGE;
        ws.Settings.Param.LrcMergeSeparator = " / ";
        ws.Storage().SaveConfig(ws.Settings);
        var read = ws.Storage().ReadAppConfig();
        Assert.Equal(ws.Settings.ToJson(), read.ToJson());
        Assert.Equal(new[] { LyricsTypeEnum.TRANSLITERATION, LyricsTypeEnum.ORIGIN, LyricsTypeEnum.ORIGIN_TRANS }, read.Config.DeserializationOutputLyricsTypes());
    }

    [Fact]
    public void MissingConfigUsesDefaults()
    {
        using var ws = new TestWorkspace();
        var config = ws.Storage().ReadAppConfig();
        Assert.Equal(ThemeModeEnum.FOLLOW_SYSTEM, config.Config.ThemeMode);
        Assert.Equal(128, config.Config.SearchCacheMaxSizeMb);
        Assert.False(string.IsNullOrWhiteSpace(config.Config.SearchCacheFolderPath));
        Assert.False(File.Exists(ws.ConfigPath));
    }

    [Fact]
    public void OldConfigPreservesNewDefaults()
    {
        using var ws = new TestWorkspace();
        File.WriteAllText(ws.ConfigPath, "{\"Config\":{\"SingerSeparator\":\" & \"},\"Param\":{\"SearchSource\":1}}");
        var settings = ws.Storage().ReadAppConfig();
        Assert.Equal(" & ", settings.Config.SingerSeparator);
        Assert.Equal(SearchSourceEnum.QQ_MUSIC, settings.Param.SearchSource);
        Assert.True(settings.Config.ConfirmBeforeExit);
        Assert.Equal(128, settings.Config.SearchCacheMaxSizeMb);
    }

    [Theory]
    [InlineData(SearchSourceEnum.NET_EASE_MUSIC)]
    [InlineData(SearchSourceEnum.QQ_MUSIC)]
    public void CachePersistsSongLyricsAndLink(SearchSourceEnum source)
    {
        using var ws = new TestWorkspace();
        var song = TestWorkspace.Song(source: source);
        LocalSongCacheService.SaveCache(ws.Settings, source, "123", song, "https://test.invalid/first");
        Assert.True(LocalSongCacheService.TryLoadSaveVo(ws.Settings, source, "123", out var read));
        Assert.Equal(song.ToJson(), read.ToJson());
        LocalSongCacheService.UpdateSongLink(ws.Settings, source, "123", "https://test.invalid/second");
        LocalSongCacheService.SaveCache(ws.Settings, source, "123", song);
        Assert.True(LocalSongCacheService.TryLoadSongLink(ws.Settings, source, "123", out var link));
        Assert.Equal("https://test.invalid/second", link);
        var other = source == SearchSourceEnum.QQ_MUSIC ? SearchSourceEnum.NET_EASE_MUSIC : SearchSourceEnum.QQ_MUSIC;
        Assert.False(LocalSongCacheService.TryLoadSaveVo(ws.Settings, other, "123", out _));
    }

    [Theory]
    [InlineData("invalid json")]
    [InlineData("null")]
    [InlineData("{}")]
    public void CorruptCacheIsAMiss(string content)
    {
        using var ws = new TestWorkspace();
        Directory.CreateDirectory(ws.Settings.Config.SearchCacheFolderPath);
        File.WriteAllText(Path.Combine(ws.Settings.Config.SearchCacheFolderPath, "网易云_123.json"), content);
        Assert.False(LocalSongCacheService.TryLoadSaveVo(ws.Settings, SearchSourceEnum.NET_EASE_MUSIC, "123", out _));
    }

    [Fact]
    public void MissingCacheAndEmptyLinkAreSafe()
    {
        using var ws = new TestWorkspace();
        Assert.False(LocalSongCacheService.TryLoadSaveVo(ws.Settings, SearchSourceEnum.NET_EASE_MUSIC, "missing", out _));
        Assert.False(LocalSongCacheService.TryLoadSongLink(ws.Settings, SearchSourceEnum.NET_EASE_MUSIC, "missing", out _));
        LocalSongCacheService.UpdateSongLink(ws.Settings, SearchSourceEnum.NET_EASE_MUSIC, "missing", "");
        Assert.False(Directory.Exists(ws.Settings.Config.SearchCacheFolderPath));
    }

    [Fact]
    public void CacheLimitEvictsOldestEntry()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.SearchCacheMaxSizeMb = 1;
        var large = "[00:01.000]" + new string('x', 600000);
        LocalSongCacheService.SaveCache(ws.Settings, SearchSourceEnum.NET_EASE_MUSIC, "1", TestWorkspace.Song("1", large));
        var old = Assert.Single(Directory.GetFiles(ws.Settings.Config.SearchCacheFolderPath));
        File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddDays(-1));
        LocalSongCacheService.SaveCache(ws.Settings, SearchSourceEnum.NET_EASE_MUSIC, "2", TestWorkspace.Song("2", large));
        Assert.False(LocalSongCacheService.TryLoadSaveVo(ws.Settings, SearchSourceEnum.NET_EASE_MUSIC, "1", out _));
        Assert.True(LocalSongCacheService.TryLoadSaveVo(ws.Settings, SearchSourceEnum.NET_EASE_MUSIC, "2", out _));
    }

    [Theory]
    [InlineData(NetworkProxyModeEnum.DIRECT_CONNECT)]
    [InlineData(NetworkProxyModeEnum.HTTP_PROXY)]
    public void ProxySelectionAppliesToLegacyClient(NetworkProxyModeEnum mode)
    {
        try
        {
            NetworkClientFactory.Configure(mode, "127.0.0.1", 8765, "test-user", "test-password");
#pragma warning disable SYSLIB0014
            using var client = new WebClient();
#pragma warning restore SYSLIB0014
            NetworkClientFactory.ConfigureWebClient(client);
            if (mode == NetworkProxyModeEnum.DIRECT_CONNECT) Assert.Null(client.Proxy);
            else
            {
                var proxy = Assert.IsType<WebProxy>(client.Proxy);
                Assert.Equal(new Uri("http://127.0.0.1:8765"), proxy.Address);
                Assert.Equal("test-user", proxy.Credentials!.GetCredential(proxy.Address!, "Basic")!.UserName);
            }
        }
        finally { NetworkClientFactory.Configure(NetworkProxyModeEnum.SYSTEM_PROXY); }
    }

    [Fact]
    public void HttpClientUsesRequestedTimeout()
    {
        using var client = NetworkClientFactory.CreateHttpClient(7);
        Assert.Equal(TimeSpan.FromSeconds(7), client.Timeout);
    }

    [Fact]
    public void TranslationRequiresCredentialsAndSelectsConfiguredProvider()
    {
        Assert.Throws<MusicLyricException>(() => new CaiYunTranslateApi(""));
        Assert.Throws<MusicLyricException>(() => new BaiduTranslateApi("id", ""));
        Assert.Empty(LyricUtils.GetAvailableTranslateApi(new()));
        Assert.IsType<CaiYunTranslateApi>(Assert.Single(LyricUtils.GetAvailableTranslateApi(new() { CaiYunToken = "test-only" })));
        Assert.IsType<BaiduTranslateApi>(Assert.Single(LyricUtils.GetAvailableTranslateApi(new() { BaiduTranslateAppId = "test-only", BaiduTranslateSecret = "test-only" })));
    }

    [Theory]
    [InlineData(LanguageEnum.CHINESE, LanguageEnum.ENGLISH, true)]
    [InlineData(LanguageEnum.ENGLISH, LanguageEnum.CHINESE, true)]
    [InlineData(LanguageEnum.JAPANESE, LanguageEnum.CHINESE, true)]
    [InlineData(LanguageEnum.CHINESE, LanguageEnum.JAPANESE, true)]
    [InlineData(LanguageEnum.ENGLISH, LanguageEnum.JAPANESE, false)]
    [InlineData(LanguageEnum.CHINESE, LanguageEnum.CHINESE, false)]
    public void CaiYunLanguageSupport(LanguageEnum from, LanguageEnum to, bool supported) =>
        Assert.Equal(supported, new CaiYunTranslateApi("test-only").IsSupport(from, to));

    private sealed class FakeTranslator : TranslateCacheableApi
    {
        public int Calls;
        protected override bool IsSupport0(LanguageEnum from, LanguageEnum to) => true;
        protected override ResultVo<string[]> Translate0(string[] inputs, LanguageEnum from, LanguageEnum to)
        {
            Calls++;
            return new(inputs.Select(x => to + ":" + x).ToArray());
        }
    }

    [Fact]
    public void TranslationCacheReusesSameLanguageButSeparatesTargetLanguages()
    {
        var text = Guid.NewGuid().ToString();
        var api = new FakeTranslator();
        Assert.Equal("ENGLISH:" + text, Assert.Single(api.Translate([text], LanguageEnum.CHINESE, LanguageEnum.ENGLISH)));
        Assert.Equal("ENGLISH:" + text, Assert.Single(api.Translate([text], LanguageEnum.CHINESE, LanguageEnum.ENGLISH)));
        Assert.Equal(1, api.Calls);
        Assert.Equal("JAPANESE:" + text, Assert.Single(api.Translate([text], LanguageEnum.CHINESE, LanguageEnum.JAPANESE)));
        Assert.Equal(2, api.Calls);
        Assert.Empty(api.Translate([], LanguageEnum.CHINESE, LanguageEnum.ENGLISH));
    }

    [Theory]
    [InlineData("MusicLyricApp v7.3", "v7.4", true)]
    [InlineData("MusicLyricApp v7.3", "v7.3", false)]
    [InlineData("MusicLyricApp v7.3", "v7.2", false)]
    [InlineData("MusicLyricApp v7.9", "v8.0", true)]
    public void UpdateComparison(string current, string latest, bool expected) =>
        Assert.Equal(expected, new UpdateCheckService(() => new GitHubInfo { TagName = latest }).CheckLatestVersion(current).HasUpdate);

    [Fact]
    public void UpdateFailureAndRateLimitAreReported()
    {
        Assert.Equal(ErrorMsgConst.GET_LATEST_VERSION_FAILED, Assert.Throws<MusicLyricException>(() => new UpdateCheckService(() => null).CheckLatestVersion("v7.3")).Message);
        Assert.Equal(ErrorMsgConst.API_RATE_LIMIT, Assert.Throws<MusicLyricException>(() => new UpdateCheckService(() => new GitHubInfo { Message = "API rate limit exceeded" }).CheckLatestVersion("v7.3")).Message);
    }
}
