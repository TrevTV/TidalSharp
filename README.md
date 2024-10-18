# TidalSharp

A .NET Tidal (partial) API wrapper and track downloading library.

## Dependencies
- [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json) for every API call
- [TagLibSharp](https://www.nuget.org/packages/TagLibSharp) for applying metadata to decrypted track data (`client.Downloader.ApplyMetadataToTrackBytes()`)

## Overview
TidalSharp is built around a core class, `TidalClient`. That class itself does very little, under it is `Downloader` and `API`.
- `Downloader` provides functions for downloading tracks by their ID, as well as applying metadata. It requires you to be signed in.
- `API` is an (incomplete) wrapper for the backend API used by Tidal. It requires you to be signed in.

All API calls return a Newtonsoft.JSON JObject as I did not want to deal with parsing everything into model classes since there are many different API endpoints.

In addition to `TidalClient`, there is `TidalURL` which is a class for parsing Tidal URLs into their entity type and ID.

## Examples

### Logging in
```cs
// a config dir isn't required, but allows persistence
string dataDir = Path.Combine(Directory.GetCurrentDirectory(), "TidalSharpData");

var client = new TidalClient(dataDir: dataDir);
Console.WriteLine($"Current logged in state (should be False): " + await client.IsLoggedIn());

bool hasExistingLogin = await client.Login();

if (!hasExistingLogin)
{
    Console.WriteLine("No existing user data.");

    var url = client.GetPkceLoginUrl();
    Console.WriteLine(url);

    Console.WriteLine("Enter resulting url: ");
    var resultUrl = Console.ReadLine()!;

    await client.Login(resultUrl);
}

Console.WriteLine("Should be logged in, ensuring...");

bool loggedIn = await client.IsLoggedIn();
if (!loggedIn)
    Console.WriteLine("Failed to login.");
else
    Console.WriteLine("Successful.");
```

### Downloading an Album by URL
```cs
var client = new TidalClient();
await client.Login();
var urlData = TidalURL.Parse("https://listen.tidal.com/album/345739416");
var tracksInAlbum = await urlData.GetAssociatedTracks(client);

foreach (var track in tracksInAlbum)
{
    var trackData = await client.Downloader.GetRawTrackBytes(track, TidalSharp.Data.AudioQuality.LOSSLESS);
    await client.Downloader.ApplyMetadataToTrackBytes(track, trackData); // if you want metadata
    File.WriteAllBytes(Path.Combine(Environment.CurrentDirectory, $"{track}{trackData.FileExtension}"), trackData.Data);
}
// Saves metadata-applied M4As of every track in GLOOM DIVISION by I DONT KNOW HOW BUT THEY FOUND ME to your current working directory
```

## Credits
- This project is heavily based on [tidal-dl-ng](https://github.com/exislow/tidal-dl-ng) and [tidalapi](https://github.com/tamland/python-tidal), under the [AGPL-3.0 License](https://github.com/exislow/tidal-dl-ng/blob/master/LICENSE) and [AGPL-3.0 License](https://github.com/exislow/tidal-dl-ng/blob/master/LICENSE), respectively.