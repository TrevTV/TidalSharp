using Dumpify;
using TidalSharp;

string dataDir = Path.Combine(Directory.GetCurrentDirectory(), "TidalSharpData");

var client = new TidalClient(dataDir);
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

var tidalUrl = TidalURL.Parse("https://tidal.com/browse/mix/01613ec5953d5060ec4b61a28e522c");
tidalUrl.Dump();

(await tidalUrl.GetAssociatedTracks(client)).Dump();
(await tidalUrl.GetCoverUrl(client, TidalSharp.Data.MediaResolution.s160)).Dump();
(await tidalUrl.GetTitle(client)).Dump();

Console.WriteLine("-----------------------");

Console.WriteLine("Downloading with bytes...");
using var downloadData = await client.Downloader.GetRawTrackBytes("389909667");
Console.WriteLine("Download complete, applying metadata");
await client.Downloader.ApplyMetadataToTrackBytes("389909667", downloadData);
File.WriteAllBytes(Path.Combine(dataDir, "test_byte" + downloadData.FileExtension), downloadData.Data);
Console.WriteLine("Done");

Console.WriteLine("-----------------------");

Console.WriteLine("Downloading with stream...");
using var downloadData2 = await client.Downloader.GetRawTrackStream("256946543");
Console.WriteLine("Download complete, applying metadata");
await client.Downloader.ApplyMetadataToTrackStream("256946543", downloadData2);
using FileStream fileStream = File.Open(Path.Combine(dataDir, "test_stream" + downloadData2.FileExtension), FileMode.Create);
downloadData2.Data.CopyTo(fileStream);
Console.WriteLine("Done");

Console.WriteLine("-----------------------");

Console.WriteLine("Downloading with file...");
var ext = await client.Downloader.GetExtensionForTrack("256946552");
await client.Downloader.WriteRawTrackToFile("256946552", Path.Combine(dataDir, "test_file" + ext));
Console.WriteLine("Download complete, applying metadata");
await client.Downloader.ApplyMetadataToFile("256946552", Path.Combine(dataDir, "test_file" + ext));
Console.WriteLine("Done");