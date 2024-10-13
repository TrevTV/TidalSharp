using Dumpify;
using TidalSharp;

string dataDir = Path.Combine(Directory.GetCurrentDirectory(), "TidalSharpData");

var client = new TidalClient(dataDir);
Console.WriteLine($"Current logged in state (should be False): " + await client.IsLoggedIn());

bool hasExistingLogin = await client.Login();

if (!hasExistingLogin)
{
    Console.WriteLine("No existing user data.");

    var url = client.Session.GetPkceLoginUrl();
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