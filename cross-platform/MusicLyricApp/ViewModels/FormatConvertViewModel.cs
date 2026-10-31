using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;

namespace MusicLyricApp.ViewModels;

public partial class FormatConvertViewModel : ViewModelBase
{
    public ObservableCollection<EnumDisplayHelper.EnumDisplayItem<OutputFormatEnum>> Formats { get; } =
        EnumDisplayHelper.GetEnumDisplayCollection<OutputFormatEnum>();

    [ObservableProperty] private EnumDisplayHelper.EnumDisplayItem<OutputFormatEnum> _sourceFormatItem;
    [ObservableProperty] private EnumDisplayHelper.EnumDisplayItem<OutputFormatEnum> _targetFormatItem;

    [ObservableProperty] private string _inputText = "";
    [ObservableProperty] private string _outputText = "";
    [ObservableProperty] private string _tipMessage = "";

    private readonly SettingBean _settingBean;
    private readonly DispatcherTimer _debounceTimer = new() { Interval = TimeSpan.FromMilliseconds(200) };

    public FormatConvertViewModel(SettingBean settingBean)
    {
        _settingBean = settingBean;
        SourceFormatItem = Formats.First(x => x.Value == OutputFormatEnum.LRC);
        TargetFormatItem = Formats.First(x => x.Value == OutputFormatEnum.SRT);
        _debounceTimer.Tick += (_, _) =>
        {
            _debounceTimer.Stop();
            TryConvert();
        };
    }

    partial void OnInputTextChanged(string value)
    {
        RequestDebouncedConvert();
    }

    partial void OnSourceFormatItemChanged(EnumDisplayHelper.EnumDisplayItem<OutputFormatEnum> value)
    {
        RequestDebouncedConvert();
    }

    partial void OnTargetFormatItemChanged(EnumDisplayHelper.EnumDisplayItem<OutputFormatEnum> value)
    {
        RequestDebouncedConvert();
    }

    [RelayCommand]
    private void SwapFormat()
    {
        (SourceFormatItem, TargetFormatItem) = (TargetFormatItem, SourceFormatItem);
        (InputText, OutputText) = (OutputText, InputText);
        RequestDebouncedConvert();
        TipMessage = LocalizationService.Get("Status.Format.Swapped");
    }

    private void RequestDebouncedConvert()
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void TryConvert()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(InputText))
            {
                OutputText = string.Empty;
                TipMessage = string.Empty;
                return;
            }

            if (SourceFormatItem.Value == TargetFormatItem.Value)
            {
                OutputText = InputText;
                TipMessage = LocalizationService.Get("Status.Format.SameFormat");
                return;
            }

            if (SourceFormatItem.Value == OutputFormatEnum.LRC && TargetFormatItem.Value == OutputFormatEnum.SRT)
            {
                OutputText = ConvertLrcTextToSrt(InputText);
                TipMessage = LocalizationService.Get("Status.Format.LrcToSrt");
                return;
            }

            if (SourceFormatItem.Value == OutputFormatEnum.SRT && TargetFormatItem.Value == OutputFormatEnum.LRC)
            {
                OutputText = SrtUtils.SrtToLrc(InputText, _settingBean.Config.LrcTimestampFormat, _settingBean.Config.DotType);
                TipMessage = LocalizationService.Get("Status.Format.SrtToLrc");
                return;
            }

            throw new MusicLyricException(LocalizationService.Get("Status.Format.Unsupported"));
        }
        catch (Exception ex)
        {
            TipMessage = LocalizationService.Format("Status.Format.Failed", ex.Message);
        }
    }

    private string ConvertLrcTextToSrt(string lrcText)
    {
        var lines = LyricUtils.SplitLrc(lrcText);
        var voList = new List<LyricLineVo>();

        foreach (var line in lines)
        {
            var parsed = new LyricLineVo(line);
            var splitList = LyricLineVo.Split(parsed);
            voList.AddRange(splitList);
        }

        voList = voList
            .Where(x => x.Timestamp.TimeOffset >= 0)
            .OrderBy(x => x.Timestamp.TimeOffset)
            .ToList();

        if (voList.Count == 0)
        {
            return string.Empty;
        }

        var duration = voList.Max(x => x.Timestamp.TimeOffset) + 3000;
        return SrtUtils.LrcToSrt(voList, _settingBean.Config.SrtTimestampFormat, _settingBean.Config.DotType, duration);
    }
}
