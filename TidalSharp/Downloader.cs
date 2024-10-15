﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using TidalSharp.Data;
using TidalSharp.Downloading;
using TidalSharp.Exceptions;
using TidalSharp.Metadata;

namespace TidalSharp;

public class Downloader
{
    internal Downloader(HttpClient client, API api, Session session)
    {
        _client = client;
        _api = api;
        _session = session;
    }

    private readonly HttpClient _client;
    private readonly API _api;
    private readonly Session _session;

    public async Task<DownloadData<Stream>> GetRawTrackStream(string trackId)
    {
        var (stream, manifest) = await GetTrackStream(trackId);
        return new(stream, manifest.FileExtension);
    }

    public async Task<DownloadData<byte[]>> GetRawTrackBytes(string trackId)
    {
        var (stream, manifest) = await GetTrackStream(trackId);
        var data = new DownloadData<byte[]>(stream.ToArray(), manifest.FileExtension);

        await stream.DisposeAsync();

        return data;
    }

    public async Task WriteRawTrackToFile(string trackId, string trackPath)
    {
        var (stream, manifest) = await GetTrackStream(trackId);
        using FileStream fileStream = File.Open(trackPath, FileMode.Create);

        await stream.CopyToAsync(fileStream);
        await stream.DisposeAsync();
    }

    public async Task<string> GetExtensionForTrack(string trackId)
    {
        var trackStreamData = await GetTrackStreamData(trackId);
        var streamManifest = new StreamManifest(trackStreamData);
        return streamManifest.FileExtension;
    }

    public async Task<byte[]> GetImageBytes(string id, MediaResolution resolution, CancellationToken token = default)
    {
        HttpRequestMessage message = new(HttpMethod.Get, Globals.GetImageUrl(id, resolution));
        HttpResponseMessage response = await _client.SendAsync(message, token);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new UnavailableMediaException($"The image with {id} with resolution {resolution} is unavailable.");
        }

        return await response.Content.ReadAsByteArrayAsync(token);
    }

    public async Task ApplyMetadataToTrackStream(string trackId, DownloadData<Stream> trackData, MediaResolution coverResolution = MediaResolution.s640, string lyrics = "", CancellationToken token = default)
    {
        byte[] magicBuffer = new byte[4];
        await trackData.Data.ReadAsync(magicBuffer.AsMemory(0, 4), token);

        trackData.Data.Seek(0, SeekOrigin.Begin);

        StreamAbstraction abstraction = new("track" + trackData.FileExtension, trackData.Data);
        using TagLib.File file = TagLib.File.Create(abstraction);
        await ApplyMetadataToTagLibFile(file, trackId, coverResolution, lyrics, token);

        trackData.Data.Seek(0, SeekOrigin.Begin);
    }

    public async Task ApplyMetadataToTrackBytes(string trackId, DownloadData<byte[]> trackData, MediaResolution coverResolution = MediaResolution.s640, string lyrics = "", CancellationToken token = default)
    {
        FileBytesAbstraction abstraction = new("track" + trackData.FileExtension, trackData.Data);
        using TagLib.File file = TagLib.File.Create(abstraction);
        await ApplyMetadataToTagLibFile(file, trackId, coverResolution, lyrics, token);

        byte[] finalData = abstraction.MemoryStream.ToArray();
        await abstraction.MemoryStream.DisposeAsync();
        trackData.Data = finalData;
    }

    public async Task ApplyMetadataToFile(string trackId, string trackPath, MediaResolution coverResolution = MediaResolution.s640, string lyrics = "", CancellationToken token = default)
    {
        using TagLib.File file = TagLib.File.Create(trackPath);
        await ApplyMetadataToTagLibFile(file, trackId, coverResolution, lyrics, token);
    }

    public async Task<(string? plainLyrics, string? syncLyrics)?> FetchLyricsFromTidal(string trackId)
    {
        var lyrics = await _api.GetTrackLyrics(trackId);
        if (lyrics == null)
            return null;

        return (lyrics.Lyrics, lyrics.Subtitles);
    }

    public async Task<(string? plainLyrics, string? syncLyrics)?> FetchLyricsFromLRCLIB(string instance, string trackName, string artistName, string albumName, int duration, CancellationToken token = default)
    {
        var requestUrl = $"https://{instance}/api/get?artist_name={Uri.EscapeDataString(artistName)}&track_name={Uri.EscapeDataString(trackName)}&album_name={Uri.EscapeDataString(albumName)}&duration={duration}";
        var response = await _client.GetAsync(requestUrl, token);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(token);
            var json = JObject.Parse(content);
            return (json["plainLyrics"]?.ToString(), json["syncedLyrics"]?.ToString());
        }

        return null;
    }

    // TODO: video downloading, this is less important as this is mainly for lidarr

    private async Task ApplyMetadataToTagLibFile(TagLib.File track, string trackId, MediaResolution coverResolution = MediaResolution.s640, string lyrics = "", CancellationToken token = default)
    {
        JToken trackData = await _api.GetTrack(trackId);
        string albumId = trackData["album"]!["id"]!.ToString();
        JToken albumPage = await _api.GetAlbum(albumId);

        byte[]? albumArt = null;
        try { albumArt = await GetImageBytes(trackData["album"]!["cover"]!.ToString(), coverResolution, token); } catch (UnavailableMediaException) { }

        track.Tag.Title = trackData["title"]!.ToString();
        track.Tag.Album = trackData["album"]!["title"]!.ToString();
        track.Tag.Performers = trackData["artists"]!.Select(a => a["name"]!.ToString()).ToArray();
        track.Tag.AlbumArtists = albumPage["artists"]!.Select(a => a["name"]!.ToString()).ToArray();
        DateTime releaseDate = DateTime.Parse(trackData["streamStartDate"]!.ToString());
        track.Tag.Year = (uint)releaseDate.Year;
        track.Tag.Track = uint.Parse(trackData["trackNumber"]!.ToString());
        track.Tag.TrackCount = uint.Parse(albumPage["numberOfTracks"]!.ToString());
        if (albumArt != null)
            track.Tag.Pictures = [new TagLib.Picture(new TagLib.ByteVector(albumArt))];
        track.Tag.Lyrics = lyrics;

        track.Save();
    }

    private async Task<(MemoryStream stream, StreamManifest manifest)> GetTrackStream(string trackId)
    {
        var trackStreamData = await GetTrackStreamData(trackId);
        var streamManifest = new StreamManifest(trackStreamData);

        var urls = streamManifest.Urls;

        var outStream = new MemoryStream();

        foreach (var url in urls)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _client.SendAsync(message);
            var stream = await response.Content.ReadAsStreamAsync();

            stream.CopyTo(outStream);
        }

        // TODO: test decryption, don't know of any tracks yet that need it

        if (!string.IsNullOrEmpty(streamManifest.EncryptionKey))
        {
            var (key, nonce) = Decryption.DecryptSecurityToken(streamManifest.EncryptionKey);
            var decryptedStream = new MemoryStream();
            Decryption.DecryptStream(outStream, decryptedStream, key, nonce);

            decryptedStream.Seek(0, SeekOrigin.Begin);
            await outStream.DisposeAsync();
            return (decryptedStream, streamManifest);
        }

        outStream.Seek(0, SeekOrigin.Begin);
        return (outStream, streamManifest);
    }

    private async Task<TrackStreamData> GetTrackStreamData(string trackId)
    {
        var result = await _api.Call(HttpMethod.Get, $"tracks/{trackId}/playbackinfopostpaywall",
            urlParameters: new()
            {
                { "playbackmode", "STREAM" },
                { "assetpresentation", "FULL" },
                { "audioquality", $"{_session.AudioQuality}" }
            }
        );
        return result.ToObject<TrackStreamData>()!;
    }
}

public class DownloadData<T>(T data, string fileExtension) : IDisposable
{
    public T Data { get; set; } = data;
    public string FileExtension { get; set; } = fileExtension;

    public void Dispose()
    {
        if (Data is Stream stream)
            stream.Dispose();

        GC.SuppressFinalize(this);
    }
}