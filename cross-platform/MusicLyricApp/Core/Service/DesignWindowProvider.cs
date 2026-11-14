using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace MusicLyricApp.Core.Service;

/// <summary>Window operations are unavailable in the XAML designer.</summary>
internal sealed class DesignWindowProvider : IWindowProvider
{
    public Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options) =>
        Task.FromResult<IReadOnlyList<IStorageFolder>>([]);

    public Task SetTextAsync(string? text) => Task.CompletedTask;

    public Task<IStorageFolder?> TryGetFolderFromPathAsync(string path) =>
        Task.FromResult<IStorageFolder?>(null);
}
