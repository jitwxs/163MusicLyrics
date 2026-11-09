using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;

namespace MusicLyricApp.Core.Service.LocalMusic;

public sealed record LocalSongCandidate(SearchSourceEnum Source, string Id, string Title, string Artist,
    string Album, long Duration, int Score, bool Reliable)
{
    public override string ToString() => string.Join(" · ", new[]
    {
        Source.ToDescription(),
        string.Join(" — ", new[] { Title, Artist }.Where(x => !string.IsNullOrWhiteSpace(x))),
        Album,
        Duration > 0 ? TimeSpan.FromMilliseconds(Duration).ToString(@"mm\:ss") : "",
        Id
    }.Where(x => !string.IsNullOrWhiteSpace(x)));
}

public sealed class LocalLyricsUnavailableException() : MusicLyricException(LocalizationService.Get("LocalScan.NoLyrics"));

public sealed record LocalMatchResult(IReadOnlyList<LocalSongCandidate> Candidates, string Warning = "")
{
    // Close candidates remain a human decision; the score is not a statistical confidence percentage.
    public LocalSongCandidate? Automatic => Candidates.Count > 0 && Candidates[0].Reliable &&
        (Candidates.Count == 1 || Candidates[0].Score - Candidates[1].Score >= 10) ? Candidates[0] : null;
}

/// <summary>A single sequential queue per scanner, with bounded retries and source-qualified cache keys.</summary>
public sealed class LocalSongMatcher(Func<SearchSourceEnum, IMusicApi> api, TimeSpan? requestInterval = null)
{
    private readonly TimeSpan _interval = requestInterval ?? TimeSpan.FromMilliseconds(500);
    private readonly SemaphoreSlim _queue = new(1, 1);
    private readonly Dictionary<SearchSourceEnum, DateTime> _lastRequest = new();
    private readonly Dictionary<(SearchSourceEnum, string, bool), LyricVo> _lyrics = new();
    private readonly Dictionary<(SearchSourceEnum, string), SearchResultVo> _searches = new();

    private async Task<T> Request<T>(SearchSourceEnum source, Func<T> request, CancellationToken ct)
    {
        await _queue.WaitAsync(ct);
        try
        {
            for (var attempt = 0; ; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                if (_lastRequest.TryGetValue(source, out var last))
                {
                    var delay = _interval - (DateTime.UtcNow - last);
                    if (delay > TimeSpan.Zero) await Task.Delay(delay, ct);
                }
                try
                {
                    // Existing music APIs are synchronous. Cancellation stops queued work and discards
                    // the current response after the provider's bounded HTTP request completes.
                    var value = await Task.Run(request, ct);
                    ct.ThrowIfCancellationRequested();
                    return value;
                }
                catch (Exception ex) when (attempt < 2 &&
                    (ex is HttpRequestException { StatusCode: null or System.Net.HttpStatusCode.TooManyRequests or System.Net.HttpStatusCode.ServiceUnavailable }
                     || ex is TaskCanceledException && !ct.IsCancellationRequested
                     || ex is MusicLyricException && ex.Message == ErrorMsgConst.API_RATE_LIMIT))
                {
                    await Task.Delay(TimeSpan.FromSeconds(attempt + 1), ct);
                }
                finally { _lastRequest[source] = DateTime.UtcNow; }
            }
        }
        finally { _queue.Release(); }
    }

    public async Task<LocalMatchResult> MatchAsync(LocalAudioFile file, string keyword, SearchSourceEnum preferred,
        bool useFallback, bool filenameIsId, CancellationToken ct)
    {
        var candidates = new List<LocalSongCandidate>();
        var warnings = new List<string>();
        var sources = useFallback && !filenameIsId
            ? new[] { preferred, preferred == SearchSourceEnum.NET_EASE_MUSIC ? SearchSourceEnum.QQ_MUSIC : SearchSourceEnum.NET_EASE_MUSIC }
            : new[] { preferred };
        foreach (var source in sources)
        {
            try
            {
                if (filenameIsId)
                {
                    var id = System.IO.Path.GetFileNameWithoutExtension(file.Path);
                    // Explicit ID mode is always a song lookup, independent of the main search settings.
                    var parsed = GlobalUtils.CheckInputId(id, source, SearchTypeEnum.SONG_ID);
                    var songs = await Request(source, () => api(source).GetSongVo([parsed.QueryId]), ct);
                    var song = songs.TryGetValue(parsed.QueryId, out var result) ? result.Assert().Data : null;
                    if (song != null) candidates.Add(new(source, song.DisplayId, song.Name,
                        string.Join(" / ", song.Singer), song.Album, song.Duration, 100, true));
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(keyword)) return new LocalMatchResult([]);
                    if (!_searches.TryGetValue((source, keyword), out var response))
                    {
                        response = (await Request(source, () => api(source).Search(keyword, SearchTypeEnum.SONG_ID).Assert(), ct)).Data;
                        if (!response.IsEmpty()) _searches[(source, keyword)] = response;
                    }
                    foreach (var song in response.SongVos.Take(20))
                    {
                        if (string.IsNullOrWhiteSpace(song.DisplayId)) continue;
                        candidates.Add(Score(file, source, song));
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                warnings.Add($"{source.ToDescription()}: {ex.Message}");
            }
            var ranked = Rank(candidates);
            if (new LocalMatchResult(ranked).Automatic != null) break;
        }
        return new LocalMatchResult(Rank(candidates), string.Join(Environment.NewLine, warnings));
    }

    private static LocalSongCandidate[] Rank(IEnumerable<LocalSongCandidate> candidates) => candidates
        .GroupBy(x => (x.Source, x.Id)).Select(g => g.OrderByDescending(x => x.Score).First())
        .OrderByDescending(x => x.Score).ToArray();

    private static string Normalize(string value) => string.Concat(value.Normalize(NormalizationForm.FormKC)
        .Where(char.IsLetterOrDigit)).ToUpperInvariant();

    private static HashSet<string> Artists(string value) => Regex.Split(value, @"\s*(?:/|&|、|,|，|;|；| feat\.? | ft\.? )\s*",
        RegexOptions.IgnoreCase).Select(Normalize).Where(x => x.Length > 0).ToHashSet();

    public static LocalSongCandidate Score(LocalAudioFile file, SearchSourceEnum source, SearchResultVo.SongSearchResultVo song)
    {
        var title = Normalize(file.Title);
        var otherTitle = Normalize(song.Title);
        var exactTitle = title.Length > 0 && title == otherTitle;
        var artists = Artists(file.Artist);
        var otherArtists = song.AuthorName.SelectMany(Artists).ToHashSet();
        var exactArtists = artists.Count > 0 && artists.SetEquals(otherArtists);
        var sameAlbum = Normalize(file.Album).Length > 0 && Normalize(file.Album) == Normalize(song.AlbumName);
        var closeDuration = file.Duration > 0 && song.Duration > 0 && Math.Abs(file.Duration - song.Duration) <= 2000;
        var conflictingDuration = file.Duration > 0 && song.Duration > 0 && Math.Abs(file.Duration - song.Duration) > 5000;
        var score = (exactTitle ? 55 : title.Length > 0 && otherTitle.Contains(title, StringComparison.Ordinal) ? 20 : 0)
                    + (exactArtists ? 30 : artists.Overlaps(otherArtists) ? 10 : 0)
                    + (sameAlbum ? 5 : 0) + (closeDuration ? 10 : 0);
        return new(source, song.DisplayId, song.Title, string.Join(" / ", song.AuthorName), song.AlbumName,
            song.Duration, score, exactTitle && exactArtists && !conflictingDuration && (closeDuration || sameAlbum));
    }

    public async Task<LyricVo> GetLyricsAsync(LocalSongCandidate candidate, bool verbatim, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var key = (candidate.Source, candidate.Id, verbatim);
        if (_lyrics.TryGetValue(key, out var cached)) return cached;
        var provider = api(candidate.Source);
        var songs = await Request(candidate.Source, () => provider.GetSongVo([candidate.Id]), ct);
        if (!songs.TryGetValue(candidate.Id, out var result)) throw new MusicLyricException(ErrorMsgConst.SONG_NOT_EXIST);
        var song = result.Assert().Data;
        var response = await Request(candidate.Source, () => provider.GetLyricVo(song.Id, song.DisplayId, verbatim), ct);
        if (!response.IsSuccess() && response.ErrorMsg == ErrorMsgConst.LRC_NOT_EXIST)
            throw new LocalLyricsUnavailableException();
        var lyrics = response.Assert().Data;
        lyrics.Duration = song.Duration;
        if (lyrics.IsEmpty()) throw new LocalLyricsUnavailableException();
        _lyrics[key] = lyrics;
        return lyrics;
    }
}
