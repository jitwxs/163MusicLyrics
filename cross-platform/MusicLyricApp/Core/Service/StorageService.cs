using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;
using NLog;

namespace MusicLyricApp.Core.Service;

public class StorageService : IStorageService
{
    private readonly Func<string> _configPath;
    private readonly Func<int, HttpClient> _httpClientFactory;

    public StorageService(Func<string>? configPath = null, Func<int, HttpClient>? httpClientFactory = null)
    {
        _configPath = configPath ?? Constants.GetConfigFilePath;
        _httpClientFactory = httpClientFactory ?? NetworkClientFactory.CreateHttpClient;
    }
    private ISearchService _searchService;

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    
    public void SetSearchService(ISearchService searchService)
    {
        _searchService = searchService;
    }

    public SettingBean ReadAppConfig()
    {
        SettingBean setting;
        if (File.Exists(_configPath()))
        {
            var text = File.ReadAllText(_configPath());
            setting = text.ToEntity<SettingBean>();
        }
        else
        {
            setting = new SettingBean();
        }

        LocalSongCacheService.EnsureConfigDefaults(setting);
        return setting;
    }

    private HttpClient CreateHttpClient(int timeoutSeconds = 120)
    {
        return _httpClientFactory(timeoutSeconds);
    }

    public void SaveConfig(SettingBean settingBean)
    {
        var path = _configPath();
        File.WriteAllText(path, settingBean.ToJson(), Encoding.UTF8);
        _logger.Info("Save config into {Path}", path);
    }

    public Task<string> SaveResult(SearchResultViewModel searchResult, SettingBean settingBean,
        IWindowProvider windowProvider)
    {
        switch (searchResult.SaveVoMap.Count)
        {
            case 0:
                throw new MusicLyricException(ErrorMsgConst.MUST_SEARCH_BEFORE_SAVE);
            case 1:
                return SaveSingleResult(searchResult, settingBean, windowProvider);
            default:
                return SaveBatchResult(searchResult, settingBean, windowProvider);
        }
    }

    public string SaveSongLink(SearchResultViewModel searchResult, SettingBean settingBean,
        IWindowProvider windowProvider)
    {
        var musicApi = _searchService.GetMusicApi(settingBean.Param.SearchSource);

        switch (searchResult.SaveVoMap.Count)
        {
            case 0:
                throw new MusicLyricException(ErrorMsgConst.MUST_SEARCH_BEFORE_GET_SONG_URL);
            case 1:
                var link = musicApi.GetSongLink(searchResult.SaveVoMap.Keys.First());
                if (link.IsSuccess())
                {
                    windowProvider.SetTextAsync(link.Data);
                    return ErrorMsgConst.SONG_URL_GET_SUCCESS;
                }
                else
                {
                    return link.ErrorMsg;
                }
            default:
                var csv = new CsvBean();
                csv.AddColumn("id");
                csv.AddColumn("songLink");

                foreach (var songId in searchResult.SaveVoMap.Values.Select(saveVo => saveVo.SongVo.DisplayId))
                {
                    csv.AddData(songId);
                    csv.AddData(musicApi.GetSongLink(songId).Data);
                    csv.NextLine();
                }

                searchResult.ResetConsoleOutput(csv.ToString());

                return ErrorMsgConst.SUCCESS;
        }
    }

    public string SaveSongPic(SearchResultViewModel searchResult, SettingBean settingBean,
        IWindowProvider windowProvider)
    {
        switch (searchResult.SaveVoMap.Count)
        {
            case 0:
                throw new MusicLyricException(ErrorMsgConst.MUST_SEARCH_BEFORE_GET_SONG_PIC);
            case 1:
                var pic = searchResult.SaveVoMap.Values.First().SongVo.Pics;
                if (string.IsNullOrWhiteSpace(pic))
                {
                    return ErrorMsgConst.SONG_PIC_GET_FAILED;
                }
                else
                {
                    windowProvider.SetTextAsync(pic);
                    return ErrorMsgConst.SONG_PIC_GET_SUCCESS;
                }
            default:
                var csv = new CsvBean();
                csv.AddColumn("id");
                csv.AddColumn("picLink");

                foreach (var saveVo in searchResult.SaveVoMap.Values)
                {
                    csv.AddData(saveVo.SongVo.DisplayId);
                    csv.AddData(saveVo.SongVo.Pics);
                    csv.NextLine();
                }

                searchResult.ResetConsoleOutput(csv.ToString());

                return ErrorMsgConst.SUCCESS;
        }
    }

    public async Task<string> DownloadSongLink(SearchResultViewModel searchResult, SettingBean settingBean,
        IWindowProvider windowProvider)
    {
        if (searchResult.SaveVoMap.Count == 0)
        {
            throw new MusicLyricException(ErrorMsgConst.MUST_SEARCH_BEFORE_GET_SONG_URL);
        }

        var saveVo = searchResult.SaveVoMap.Values.First();
        var link = searchResult.SongLink;
        if (string.IsNullOrWhiteSpace(link))
        {
            var musicApi = _searchService.GetMusicApi(settingBean.Param.SearchSource);
            var linkResult = musicApi.GetSongLink(saveVo.SongVo.DisplayId);
            if (!linkResult.IsSuccess())
            {
                throw new MusicLyricException(linkResult.ErrorMsg);
            }

            link = linkResult.Data;
        }

        var folder = await ResolveSaveFolderAsync(settingBean, windowProvider);
        using var client = CreateHttpClient();
        using var resp = await client.GetAsync(link);
        resp.EnsureSuccessStatusCode();

        var ext = ResolveAudioExt(link, resp.Content.Headers.ContentType?.MediaType);
        var file = await CreateOutputFileAsync(folder, $"{GetSafeFileStem(saveVo.SongVo.Name)}.{ext}", settingBean);
        await using var output = await file.OpenWriteAsync();
        await using var input = await resp.Content.ReadAsStreamAsync();
        await input.CopyToAsync(output);
        await output.FlushAsync();

        return LocalizationService.Get("Status.Storage.AudioSaved");
    }

    public async Task<string> DownloadSongPic(SearchResultViewModel searchResult, SettingBean settingBean,
        IWindowProvider windowProvider)
    {
        if (searchResult.SaveVoMap.Count == 0)
        {
            throw new MusicLyricException(ErrorMsgConst.MUST_SEARCH_BEFORE_GET_SONG_PIC);
        }

        var saveVo = searchResult.SaveVoMap.Values.First();
        if (string.IsNullOrWhiteSpace(saveVo.SongVo.Pics))
        {
            throw new MusicLyricException(ErrorMsgConst.SONG_PIC_GET_FAILED);
        }

        var folder = await ResolveSaveFolderAsync(settingBean, windowProvider);
        var ext = GetImageExt(saveVo.SongVo.Pics);
        var file = await CreateOutputFileAsync(folder, $"{GetSafeFileStem(saveVo.SongVo.Name)}-cover.{ext}", settingBean);

        using var client = CreateHttpClient();
        var bytes = await client.GetByteArrayAsync(saveVo.SongVo.Pics);
        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(bytes);
        await stream.FlushAsync();

        return LocalizationService.Get("Status.Storage.CoverSaved");
    }

    private async Task<string> SaveSingleResult(SearchResultViewModel searchResult, SettingBean settingBean,
        IWindowProvider windowProvider)
    {
        var saveVo = searchResult.SaveVoMap.Values.First();
        var preCheck = IsSkipStorage(saveVo, settingBean);

        if (preCheck != null)
        {
            throw new MusicLyricException(preCheck);
        }

        await WriteToFile(await ResolveSaveFolderAsync(settingBean, windowProvider), saveVo, settingBean);
        await DownloadExtraResourcesIfEnabled(saveVo, settingBean, windowProvider);

        return string.Format(ErrorMsgConst.SAVE_COMPLETE, 1, 0);
    }

    private async Task<string> SaveBatchResult(SearchResultViewModel searchResult, SettingBean settingBean,
        IWindowProvider windowProvider)
    {
        var folder = await ResolveSaveFolderAsync(settingBean, windowProvider);

        var skipRes = new Dictionary<string, string>();
        var successRes = new HashSet<string>();

        foreach (var saveVo in searchResult.SaveVoMap.Values)
        {
            var resKey = $"{saveVo.SongVo.DisplayId}[{saveVo.SongVo.Name}]";

            var preCheck = IsSkipStorage(saveVo, settingBean);

            if (preCheck != null)
            {
                skipRes[resKey] = preCheck;
            }
            else
            {
                await WriteToFile(folder, saveVo, settingBean);
                await DownloadExtraResourcesIfEnabled(saveVo, settingBean, folder);
                successRes.Add(resKey);
            }
        }

        searchResult.ResetConsoleOutput(RenderUtils.RenderStorageResult(skipRes, successRes));

        return string.Format(ErrorMsgConst.SAVE_COMPLETE, successRes.Count, skipRes.Count);
    }

    private async Task DownloadExtraResourcesIfEnabled(SaveVo saveVo, SettingBean settingBean, IWindowProvider windowProvider)
    {
        var folder = await ResolveSaveFolderAsync(settingBean, windowProvider);
        await DownloadExtraResourcesIfEnabled(saveVo, settingBean, folder);
    }

    private async Task DownloadExtraResourcesIfEnabled(SaveVo saveVo, SettingBean settingBean, IStorageFolder folder)
    {
        if (!settingBean.Config.DownloadCoverAndSongLinkOnSave || _searchService == null)
        {
            return;
        }

        var baseName = GlobalUtils.GetOutputName(saveVo, settingBean.Config.OutputFileNameFormat,
            settingBean.Config.SingerSeparator, settingBean.Config.SingerCountLimit);

        try
        {
            if (!string.IsNullOrWhiteSpace(saveVo.SongVo.Pics))
            {
                var ext = GetImageExt(saveVo.SongVo.Pics);
                var file = await CreateOutputFileAsync(folder, $"{baseName}-cover.{ext}", settingBean);
                using var client = CreateHttpClient();
                var bytes = await client.GetByteArrayAsync(saveVo.SongVo.Pics);
                await using var stream = await file.OpenWriteAsync();
                await stream.WriteAsync(bytes);
                await stream.FlushAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "Download cover on save failed, songId={SongId}", saveVo.SongVo.DisplayId);
        }

        try
        {
            var musicApi = _searchService.GetMusicApi(saveVo.LyricVo.SearchSource);
            var linkResult = await Task.Run(() => musicApi.GetSongLink(saveVo.SongVo.DisplayId));
            if (!linkResult.IsSuccess() || string.IsNullOrWhiteSpace(linkResult.Data))
            {
                return;
            }

            using var client = CreateHttpClient();
            using var resp = await client.GetAsync(linkResult.Data);
            resp.EnsureSuccessStatusCode();
            var ext = ResolveAudioExt(linkResult.Data, resp.Content.Headers.ContentType?.MediaType);
            var file = await CreateOutputFileAsync(folder, $"{baseName}.{ext}", settingBean);
            await using var output = await file.OpenWriteAsync();
            await using var input = await resp.Content.ReadAsStreamAsync();
            await input.CopyToAsync(output);
            await output.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "Download song link on save failed, songId={SongId}", saveVo.SongVo.DisplayId);
        }
    }

    internal static string? IsSkipStorage(SaveVo saveVo, SettingBean settingBean)
    {
        // 没有歌词内容
        if (saveVo.LyricVo.IsEmpty())
        {
            return ErrorMsgConst.LRC_NOT_EXIST;
        }

        // 纯音乐跳过
        if (saveVo.LyricVo.IsPureMusic() && settingBean.Config.IgnorePureMusicInSave)
        {
            return ErrorMsgConst.PURE_MUSIC_IGNORE_SAVE;
        }

        return null;
    }

    public async Task<IStorageFolder> SelectFolderAndRememberAsync(SettingBean settingBean, IWindowProvider windowProvider)
    {
        var folder = await PickFolderAsync(windowProvider);
        RememberFolder(settingBean, folder);
        return folder;
    }

    private async Task<IStorageFolder> ResolveSaveFolderAsync(SettingBean settingBean, IWindowProvider windowProvider)
    {
        var cached = await TryGetRememberedFolderAsync(settingBean, windowProvider);
        if (cached != null)
        {
            return cached;
        }

        var picked = await PickFolderAsync(windowProvider);
        RememberFolder(settingBean, picked);
        return picked;
    }

    private async Task<IStorageFolder?> TryGetRememberedFolderAsync(SettingBean settingBean, IWindowProvider windowProvider)
    {
        var path = settingBean.Config.LastSaveFolderPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return await windowProvider.TryGetFolderFromPathAsync(path);
    }

    private static async Task<IStorageFolder> PickFolderAsync(IWindowProvider windowProvider)
    {
        var folders = await windowProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = LocalizationService.Get("Dialog.Folder.Output"),
            AllowMultiple = false
        });

        if (folders.Count == 0)
            throw new MusicLyricException(ErrorMsgConst.STORAGE_FOLDER_ERROR);

        return folders[0];
    }

    private void RememberFolder(SettingBean settingBean, IStorageFolder folder)
    {
        var path = folder.Path?.LocalPath ?? folder.Path?.ToString();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        settingBean.Config.LastSaveFolderPath = path;
        SaveConfig(settingBean);
    }

    private static string GetSafeFileStem(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "song";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }

    private static string GetImageExt(string url)
    {
        var lower = url.ToLowerInvariant();
        if (lower.Contains(".png")) return "png";
        if (lower.Contains(".webp")) return "webp";
        if (lower.Contains(".bmp")) return "bmp";
        return "jpg";
    }

    private static string ResolveAudioExt(string url, string? mediaType)
    {
        var lower = url.ToLowerInvariant();
        if (lower.Contains(".flac")) return "flac";
        if (lower.Contains(".wav")) return "wav";
        if (lower.Contains(".m4a")) return "m4a";
        if (lower.Contains(".aac")) return "aac";
        if (lower.Contains(".ogg")) return "ogg";
        if (lower.Contains(".mp3")) return "mp3";

        if (string.IsNullOrWhiteSpace(mediaType))
        {
            return "mp3";
        }

        return mediaType.ToLowerInvariant() switch
        {
            "audio/flac" => "flac",
            "audio/wav" or "audio/x-wav" => "wav",
            "audio/mp4" => "m4a",
            "audio/aac" => "aac",
            "audio/ogg" => "ogg",
            "audio/mpeg" => "mp3",
            _ => "mp3"
        };
    }

    private static async Task WriteToFile(IStorageFolder folder, SaveVo saveVo, SettingBean settingBean)
    {
        var extension = settingBean.Param.OutputFileFormat.ToDescription().ToLower();
        var encoding = GlobalUtils.GetEncoding(settingBean.Param.Encoding);
        var filename = GlobalUtils.GetOutputName(saveVo, settingBean.Config.OutputFileNameFormat,
            settingBean.Config.SingerSeparator, settingBean.Config.SingerCountLimit);

        var res = await LyricUtils.GetOutputContent(saveVo.LyricVo, settingBean);

        var isSingle = res.Count == 1;

        for (var i = 0; i < res.Count; i++)
        {
            var fullName = isSingle ? $"{filename}.{extension}" : $"{filename}-{i}.{extension}";
            var file = await CreateOutputFileAsync(folder, fullName, settingBean);

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream, encoding);

            await writer.WriteAsync(res[i]);
            await writer.FlushAsync();
        }
    }

    private static async Task<IStorageFile> CreateOutputFileAsync(IStorageFolder folder, string desiredFileName, SettingBean settingBean)
    {
        var resolvedFileName = ResolveFileNameWithConflict(folder, desiredFileName, settingBean);
        return await folder.CreateFileAsync(resolvedFileName);
    }

    private static string ResolveFileNameWithConflict(IStorageFolder folder, string desiredFileName, SettingBean settingBean)
    {
        if (settingBean.Config.FileConflictStrategy == FileConflictStrategyEnum.OVERWRITE)
        {
            return desiredFileName;
        }

        var folderPath = folder.Path?.LocalPath;
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return desiredFileName;
        }

        var desiredPath = Path.Combine(folderPath, desiredFileName);
        if (!File.Exists(desiredPath))
        {
            return desiredFileName;
        }

        var stem = Path.GetFileNameWithoutExtension(desiredFileName);
        var ext = Path.GetExtension(desiredFileName);
        var suffixPattern = NormalizeSuffixPattern(settingBean.Config.FileConflictSuffixPattern);

        for (var n = 1; n <= 100000; n++)
        {
            var suffix = SanitizeSuffix(suffixPattern.Replace("{n}", n.ToString()));
            var candidateName = $"{stem}{suffix}{ext}";
            if (!File.Exists(Path.Combine(folderPath, candidateName)))
            {
                return candidateName;
            }
        }

        return desiredFileName;
    }

    private static string NormalizeSuffixPattern(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return " ({n})";
        }

        return pattern.Contains("{n}") ? pattern : $"{pattern}{{n}}";
    }

    private static string SanitizeSuffix(string suffix)
    {
        if (string.IsNullOrEmpty(suffix))
        {
            return suffix;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var invalidPattern = $"[{Regex.Escape(new string(invalidChars))}]";
        return Regex.Replace(suffix, invalidPattern, "_");
    }
}


