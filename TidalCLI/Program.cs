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

var trackBytes = await client.Downloader.GetRawTrackBytes("46755209");
File.WriteAllBytes(Path.Combine(dataDir, "test.m4a"), trackBytes);