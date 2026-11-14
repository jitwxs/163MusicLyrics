using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using MusicLyricApp.Core.Localization;

namespace MusicLyricApp.Core.Service;

/// <summary>Asks for one folder with the system picker.</summary>
public static class FolderPicker
{
    /// <summary>The chosen folder, or null when the picker was cancelled.</summary>
    public static async Task<IStorageFolder?> PickFolderAsync(this IWindowProvider window, string titleKey) =>
        (await window.OpenFolderPickerAsync(Options(titleKey))).FirstOrDefault();

    /// <inheritdoc cref="PickFolderAsync(IWindowProvider, string)"/>
    public static async Task<IStorageFolder?> PickFolderAsync(this IStorageProvider storage, string titleKey) =>
        (await storage.OpenFolderPickerAsync(Options(titleKey))).FirstOrDefault();

    /// <summary>The folder's path on disk, or null when it has none.</summary>
    public static string? LocalPath(this IStorageFolder? folder)
    {
        var path = folder?.Path?.LocalPath ?? folder?.Path?.ToString();
        return string.IsNullOrWhiteSpace(path) ? null : path;
    }

    private static FolderPickerOpenOptions Options(string titleKey) =>
        new() { Title = LocalizationService.Get(titleKey), AllowMultiple = false };
}
