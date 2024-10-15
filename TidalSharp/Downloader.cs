using Newtonsoft.Json;
using TidalSharp.Data;
using TidalSharp.Downloading;

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
            throw new Exception($"The image with {id} with resolution {resolution} is unavailable."); // TODO: custom extension
        }

        return await response.Content.ReadAsByteArrayAsync(token);
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