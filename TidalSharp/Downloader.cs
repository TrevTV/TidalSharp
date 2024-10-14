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

    // TODO: rework the api so that we can return additional info like extensions
    public async Task<byte[]> GetRawTrackBytes(string trackId)
    {
        var trackStreamData = await GetTrackStreamData(trackId);
        var streamManifest = new StreamManifest(trackStreamData);

        var urls = streamManifest.Urls;

        var outStream = new MemoryStream();

        int i = 0;
        foreach (var url in urls)
        {
            i++;
            Console.WriteLine("Downloading chunk " + i.ToString());
            var message = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _client.SendAsync(message);
            var stream = await response.Content.ReadAsStreamAsync();

            stream.CopyTo(outStream);
            Console.WriteLine("done");
        }

        // TODO: decrypt if needed

        return outStream.ToArray();
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