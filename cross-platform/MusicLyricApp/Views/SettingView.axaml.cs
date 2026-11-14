using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;

namespace MusicLyricApp.Views;

/// <summary>
/// Settings page showing one section, picked from the main sidebar. Changes apply to the in-memory settings
/// as they are made and are persisted when the user leaves the settings area or quits.
/// </summary>
public partial class SettingView : UserControl
{
    private SettingViewModel? _viewModel;

    public SettingView()
    {
        InitializeComponent();

        // The shortcut rows carry their own texts, so they are built again after a language switch.
        ShortcutListHost.Content = ShortcutList.Build();
        LocalizationService.Observe(this, static view => view.ShortcutListHost.Content = ShortcutList.Build(),
            LanguageChangePhase.View);

        // Screen readers announce lyric types by name while they are moved.
        LyricTypesList.ItemTitleSelector = item => (item as LyricsTypeEnumModel)?.Name ?? item?.ToString() ?? "";
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = DataContext as SettingViewModel;

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            FocusPendingCookie();
            _viewModel.ConfirmDeleteOldLogsAsync = usage => DialogHelper.ConfirmOnAsync(this,
                LocalizationService.Get("Settings.DeleteOldLogs.Title"),
                LocalizationService.Format("Settings.DeleteOldLogs.Message", usage.OldFiles,
                    LoggingService.FormatSize(usage.OldBytes)),
                LocalizationService.Get("Settings.DeleteOldLogs"), isDanger: true);
        }
    }

    /// <summary>A newly chosen section starts at its top, not at the previous section's scroll offset.</summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingViewModel.CurrentSection))
        {
            SectionScroller.Offset = new Vector(0, 0);
        }
        else if (e.PropertyName == nameof(SettingViewModel.PendingCookieFocus))
        {
            FocusPendingCookie();
        }
    }

    /// <summary>
    /// Brings the cookie group into view and puts the caret in the requested source's field, once the section is
    /// laid out (it may have just become visible).
    /// </summary>
    private void FocusPendingCookie()
    {
        if (_viewModel?.PendingCookieFocus is not { } source) return;

        Dispatcher.UIThread.Post(() =>
        {
            if (_viewModel?.PendingCookieFocus != source) return;
            _viewModel.PendingCookieFocus = null;

            var field = this.FindControl<Control>(source == SearchSourceEnum.QQ_MUSIC ? "QqCookieField" : "NetEaseCookieField");
            this.FindControl<Control>("CookieGroup")?.BringIntoView();
            field?.Focus();
        }, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// A token button inserts its token at the caret of the custom format input (replacing a selection, or the
    /// existing millisecond run for S, SS and SSS), then gives focus back to the input to keep typing.
    /// </summary>
    private void OnTimestampTokenClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: TimestampToken token } button) return;
        var tokens = button.FindAncestorOfType<ItemsControl>();
        if (tokens?.Tag is not string inputName || this.FindControl<TextBox>(inputName) is not { } input) return;

        var (text, caret) = TimestampFormatValidator.InsertToken(input.Text, token.Token,
            input.SelectionStart, input.SelectionEnd);
        input.Text = text;
        input.Focus();
        input.SelectionStart = input.SelectionEnd = input.CaretIndex = caret;
    }
}
