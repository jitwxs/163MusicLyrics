using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using MusicLyricApp.Models;

namespace MusicLyricApp.ViewModels;

/// <summary>How complete the lyrics of the last search are, for the requested output types.</summary>
public enum LampStateEnum
{
    NONE,
    COMPLETE,
    PARTIAL,
    EMPTY
}

public partial class SignalLampViewModel : ObservableObject
{
    [ObservableProperty] private IBrush _lampColor = Brushes.Gray;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsComplete), nameof(IsPartial), nameof(IsEmpty))]
    private LampStateEnum _state = LampStateEnum.NONE;

    public bool IsComplete => State == LampStateEnum.COMPLETE;
    public bool IsPartial => State == LampStateEnum.PARTIAL;
    public bool IsEmpty => State == LampStateEnum.EMPTY;

    [ObservableProperty] private string _details = "";

    public void UpdateLampInfo(Dictionary<string, ResultVo<SaveVo>> resDict, SettingBean settingBean)
    {
        var outputTypes = settingBean.Config.DeserializationOutputLyricsTypes();
        
        var lyricTypeSelectors = new Dictionary<LyricsTypeEnum, Func<LyricVo, string?>>
        {
            { LyricsTypeEnum.ORIGIN, vo => vo.Lyric },
            { LyricsTypeEnum.ORIGIN_TRANS, vo => vo.TranslateLyric },
            { LyricsTypeEnum.TRANSLITERATION, vo => vo.TransliterationLyric }
        };
        
        // 初始化输出字典
        var outputDict = outputTypes
            .Where(lyricTypeSelectors.ContainsKey)
            .ToDictionary(type => type.ToDescription(), _ => 0);
        
        // 统计每种类型
        foreach (var lyricVo in resDict.Values.Where(vo => vo.IsSuccess()).Select(vo => vo.Data.LyricVo))
        {
            foreach (var type in outputTypes)
            {
                if (!lyricTypeSelectors.TryGetValue(type, out var selector)) continue;

                if (string.IsNullOrEmpty(selector(lyricVo))) continue;
                
                var key = type.ToDescription();
                if (outputDict.ContainsKey(key))
                {
                    outputDict[key]++;
                }
            }
        }

        var totalCnt = outputDict.Values.Sum();
        if (totalCnt == 0)
        {
            LampColor = Brushes.Red;
            State = LampStateEnum.EMPTY;
        }
        else if (totalCnt == outputDict.Keys.Count * resDict.Count)
        {
            LampColor = Brushes.LimeGreen;
            State = LampStateEnum.COMPLETE;
        }
        else
        {
            LampColor = Brushes.Chocolate;
            State = LampStateEnum.PARTIAL;
        }

        var sb = new StringBuilder();
        foreach (var pair in outputDict)
        {
            sb.Append($"{pair.Key}: {pair.Value}\t");
        }

        Details = sb.ToString();
    }
}