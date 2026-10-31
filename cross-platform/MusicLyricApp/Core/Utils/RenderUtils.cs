using System;
using System.Collections.Generic;
using System.Text;
using MusicLyricApp.Models;
using MusicLyricApp.Core.Localization;

namespace MusicLyricApp.Core.Utils;

public static class RenderUtils
{
    public static string RenderSearchResult(Dictionary<string, ResultVo<SaveVo>> resDict, Dictionary<string, SaveVo> successMap)
    {
        var log = new StringBuilder();

        foreach (var (songId, resultVo) in resDict)
        {
            log.Append($"{songId}");

            if (resultVo.IsSuccess())
            {
                var saveVo = resultVo.Data;
                successMap.Add(songId, saveVo);

                log.Append($" => {saveVo.SongVo.Name}");
            }

            log
                .Append($" => {resultVo.ErrorMsg}")
                .Append(Environment.NewLine);
        }

        log
            .Append(Environment.NewLine)
            .Append(
                LocalizationService.Get("Message.SearchSummary")
                    .Replace("{total}", resDict.Count.ToString(), StringComparison.Ordinal)
                    .Replace("{success}", successMap.Count.ToString(), StringComparison.Ordinal)
                    .Replace("{failed}", (resDict.Count - successMap.Count).ToString(), StringComparison.Ordinal))
            .Append(Environment.NewLine);
        
        return log.ToString();
    }
    
    public static string RenderStorageResult(Dictionary<string, string> skipRes, HashSet<string> successRes)
    {
        var log = new StringBuilder();

        if (successRes.Count > 0)
        {
            log
                .Append(LocalizationService.Get("Message.StorageSuccessHeader"))
                .Append(Environment.NewLine);
            foreach (var e in successRes)
            {
                log
                    .Append(e)
                    .Append(Environment.NewLine);
            }
        }

        if (skipRes.Count > 0)
        {
            log
                .Append(LocalizationService.Get("Message.StorageFailureHeader"))
                .Append(Environment.NewLine);
            foreach (var pair in skipRes)
            {
                log
                    .Append($"{pair.Key} => {pair.Value}")
                    .Append(Environment.NewLine);
            }
        }
        
        return log.ToString();
    }
}
