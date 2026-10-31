using System.Net;
using System.Net.Http;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Platform.Storage;
using Moq;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;

[assembly: AvaloniaTestApplication(typeof(MusicLyricAppTest.Regression.TestApp))]
// NetworkClientFactory and GlobalCache are process-wide mutable application state.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace MusicLyricAppTest.Regression;

public class TestApp : MusicLyricApp.App
{
    public override void OnFrameworkInitializationCompleted() { }
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApp>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

public class TestWorkspace : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "MusicLyricRegression", Guid.NewGuid().ToString("N"));
    public SettingBean Settings { get; } = new();
    public string ConfigPath => Path.Combine(Root, "settings.json");
    public Mock<IWindowProvider> Window { get; } = new(MockBehavior.Strict);
    public Mock<IStorageFolder> Folder { get; } = new(MockBehavior.Strict);
    public List<string> CreatedFiles { get; } = [];
    public string? Clipboard { get; private set; }

    public TestWorkspace()
    {
        Directory.CreateDirectory(Root);
        Settings.Config.SearchCacheFolderPath = Path.Combine(Root, "cache");
        Settings.Config.LastSaveFolderPath = Root;
        Settings.Config.OutputLyricTypes = "0";
        Settings.Config.AutoCheckUpdate = false;
        Folder.SetupGet(x => x.Path).Returns(new Uri(Root + Path.DirectorySeparatorChar));
        Folder.Setup(x => x.CreateFileAsync(It.IsAny<string>())).Returns((string name) =>
        {
            var path = Path.Combine(Root, name);
            CreatedFiles.Add(path);
            var file = new Mock<IStorageFile>(MockBehavior.Strict);
            file.Setup(x => x.OpenWriteAsync()).Returns(() => Task.FromResult<Stream>(File.Create(path)));
            file.Setup(x => x.Dispose());
            return Task.FromResult<IStorageFile?>(file.Object);
        });
        Window.Setup(x => x.TryGetFolderFromPathAsync(Root)).ReturnsAsync(Folder.Object);
        Window.Setup(x => x.OpenFolderPickerAsync(It.IsAny<FolderPickerOpenOptions>()))
            .ReturnsAsync(new List<IStorageFolder> { Folder.Object });
        Window.Setup(x => x.SetTextAsync(It.IsAny<string?>())).Callback((string? value) => Clipboard = value)
            .Returns(Task.CompletedTask);
    }

    public StorageService Storage(Func<int, HttpClient>? factory = null) => new(() => ConfigPath, factory);
    public SearchService Search(Mock<IMusicApi> netease, Mock<IMusicApi>? qq = null) => new(Settings,
        new Dictionary<SearchSourceEnum, IMusicApi>
        {
            [SearchSourceEnum.NET_EASE_MUSIC] = netease.Object,
            [SearchSourceEnum.QQ_MUSIC] = (qq ?? new Mock<IMusicApi>(MockBehavior.Strict)).Object
        });
    public SearchParamViewModel Param(string text)
    {
        var vm = new SearchParamViewModel();
        vm.Bind(Settings.Param);
        vm.SearchText = text;
        return vm;
    }
    public static SaveVo Song(string id = "123", string lyric = "[00:01.000]测试歌词", SearchSourceEnum source = SearchSourceEnum.NET_EASE_MUSIC) =>
        new(1, new SongVo { Id = id, DisplayId = id, Name = "测试歌曲" + id, Singer = ["歌手甲", "歌手乙"], Album = "测试专辑", Duration = 65000, Pics = "https://test.invalid/cover.png", PublishDate = "2020-01-01" },
            new LyricVo { Lyric = lyric, SearchSource = source, Duration = 65000 });
    public static SearchResultViewModel Result(params SaveVo[] songs) => new()
    {
        SaveVoMap = songs.ToDictionary(x => x.SongVo.DisplayId)
    };
    public static InputSongId Input(string id = "123", SearchSourceEnum source = SearchSourceEnum.NET_EASE_MUSIC) => new(id, new InputSongId(id, source, SearchTypeEnum.SONG_ID));
    public void Dispose() => Directory.Delete(Root, true);
}

public sealed class ResponseHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<Uri> Requests { get; } = [];
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        Requests.Add(request.RequestUri!);
        return Task.FromResult(respond(request));
    }
    public static HttpResponseMessage Bytes(byte[] bytes, string mime = "audio/mpeg", HttpStatusCode code = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(code) { Content = new ByteArrayContent(bytes) };
        response.Content.Headers.ContentType = new(mime);
        return response;
    }
}
