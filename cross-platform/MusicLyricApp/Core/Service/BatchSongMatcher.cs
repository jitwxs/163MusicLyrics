using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MusicLyricApp.Models;

namespace MusicLyricApp.Core.Service;

public sealed record BatchSongQuery(
    string Title,
    string? Artist,
    string? DirectSongId = null,
    SearchSourceEnum? DirectSource = null);

public sealed record BatchSongCandidate(
    SearchSourceEnum Source,
    string SongId,
    string Title,
    string[] Artists,
    string Album,
    long Duration);

public static class BatchSongMatcher
{
    private static readonly Regex ListPrefixRegex = new(@"^\s*(?:[-*+]\s+|\d+[.、)]\s*)", RegexOptions.Compiled);
    private static readonly Regex MarkdownSeparatorRegex = new(@"^:?-{3,}:?$", RegexOptions.Compiled);
    private static readonly Regex NetEaseSongIdRegex = new(
        @"(?:music\.163\.com/(?:#/)?song\?(?:[^\s|]*&)?id=)(\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static IReadOnlyList<BatchSongQuery> ParseQueries(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        var lines = input.Replace("\r", string.Empty).Split('\n');
        var containsMarkdownTable = lines.Any(IsMarkdownSeparatorRow);
        var results = new List<BatchSongQuery>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rawLine in lines)
        {
            if (containsMarkdownTable && !rawLine.Contains('|'))
            {
                continue;
            }

            var parsed = ParseLine(rawLine);
            if (parsed == null)
            {
                continue;
            }

            var key = $"{Normalize(parsed.Title)}\u001f{Normalize(parsed.Artist)}";
            if (seen.Add(key))
            {
                results.Add(parsed);
            }
        }

        return results;
    }

    public static IReadOnlyList<BatchSongCandidate> FindExactCandidates(
        BatchSongQuery query,
        IEnumerable<SearchResultVo> searchResults)
    {
        var normalizedTitle = Normalize(query.Title);
        return searchResults
            .SelectMany(result => result.SongVos.Select(song => new BatchSongCandidate(
                result.SearchSource,
                song.DisplayId,
                song.Title,
                song.AuthorName ?? [],
                song.AlbumName,
                song.Duration)))
            .Where(candidate => Normalize(candidate.Title) == normalizedTitle)
            .GroupBy(candidate => (candidate.Source, candidate.SongId))
            .Select(group => group.First())
            .ToList();
    }

    public static IReadOnlyList<BatchSongCandidate> RankCandidates(
        IEnumerable<BatchSongCandidate> candidates,
        string? preferredArtist)
    {
        var materialized = candidates.ToList();
        var matchingArtistExists = materialized.Any(candidate => ArtistMatches(candidate.Artists, preferredArtist));
        var eligible = matchingArtistExists
            ? materialized.Where(candidate => ArtistMatches(candidate.Artists, preferredArtist))
            : materialized;

        return eligible
            .OrderBy(candidate => candidate.Source == SearchSourceEnum.NET_EASE_MUSIC ? 0 : 1)
            .ThenBy(candidate => candidate.Title, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.SongId, StringComparer.Ordinal)
            .ToList();
    }

    public static bool ArtistMatches(IEnumerable<string> artists, string? preferredArtist)
    {
        var normalizedPreferred = Normalize(preferredArtist);
        if (string.IsNullOrEmpty(normalizedPreferred))
        {
            return false;
        }

        return artists
            .Select(Normalize)
            .Where(artist => !string.IsNullOrEmpty(artist))
            .Any(artist => artist.Contains(normalizedPreferred, StringComparison.Ordinal) ||
                           normalizedPreferred.Contains(artist, StringComparison.Ordinal));
    }

    public static bool DirectSongMatchesQuery(
        BatchSongQuery query,
        string actualTitle,
        IEnumerable<string> actualArtists)
    {
        if (Normalize(query.Title) != Normalize(actualTitle))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(query.Artist) || ArtistMatches(actualArtists, query.Artist);
    }

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }

    private static BatchSongQuery? ParseLine(string rawLine)
    {
        var line = rawLine.Trim();
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        if (line.Contains('|'))
        {
            var cells = line.Split('|', StringSplitOptions.TrimEntries)
                .Where(cell => !string.IsNullOrWhiteSpace(cell))
                .ToList();

            if (cells.Count == 0 || cells.All(cell => MarkdownSeparatorRegex.IsMatch(cell)))
            {
                return null;
            }

            if (IsHeader(cells))
            {
                return null;
            }

            if (cells.Count >= 2 && int.TryParse(cells[0], out _))
            {
                return CreateQuery(
                    cells[1],
                    cells.Count >= 3 ? cells[2] : null,
                    cells.Count >= 4 ? cells.Skip(3) : null);
            }

            if (cells.Count >= 2)
            {
                return CreateQuery(
                    cells[0],
                    cells[1],
                    cells.Count >= 3 ? cells.Skip(2) : null);
            }

            return CreateQuery(cells[0], null);
        }

        var tabCells = line.Split('\t', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (tabCells.Length >= 2)
        {
            return CreateQuery(tabCells[0], tabCells[1]);
        }

        return CreateQuery(ListPrefixRegex.Replace(line, string.Empty), null);
    }

    private static bool IsHeader(IReadOnlyList<string> cells)
    {
        var first = Normalize(cells[0]);
        var second = cells.Count > 1 ? Normalize(cells[1]) : string.Empty;
        return cells[0].Trim() == "#" ||
               first is "序号" or "编号" or "no" or "number" ||
               second is "歌曲" or "歌曲名" or "歌名" or "title";
    }

    private static bool IsMarkdownSeparatorRow(string rawLine)
    {
        if (!rawLine.Contains('|'))
        {
            return false;
        }

        var cells = rawLine.Split('|', StringSplitOptions.TrimEntries)
            .Where(cell => !string.IsNullOrWhiteSpace(cell))
            .ToList();
        return cells.Count > 0 && cells.All(cell => MarkdownSeparatorRegex.IsMatch(cell));
    }

    private static BatchSongQuery? CreateQuery(
        string? title,
        string? artist,
        IEnumerable<string>? extraCells = null)
    {
        var cleanTitle = title?.Trim();
        if (string.IsNullOrWhiteSpace(cleanTitle))
        {
            return null;
        }

        var cleanArtist = string.IsNullOrWhiteSpace(artist) ? null : artist.Trim();
        var directLinkText = extraCells == null ? string.Empty : string.Join(" ", extraCells);
        var netEaseMatch = NetEaseSongIdRegex.Match(directLinkText);
        return netEaseMatch.Success
            ? new BatchSongQuery(
                cleanTitle,
                cleanArtist,
                netEaseMatch.Groups[1].Value,
                SearchSourceEnum.NET_EASE_MUSIC)
            : new BatchSongQuery(cleanTitle, cleanArtist);
    }
}
