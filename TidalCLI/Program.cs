using Dumpify;
using TidalSharp;

string dataDir = Path.Combine(Directory.GetCurrentDirectory(), "TidalSharpData");

var client = new TidalClient(dataDir);
bool loggedIn = await client.Login();

if (!loggedIn)
{
    Console.WriteLine("No existing user data.");

    var url = client.Session.GetPkceLoginUrl();
    Console.WriteLine(url);

    Console.WriteLine("Enter resulting url: ");
    var resultUrl = Console.ReadLine()!;

    await client.Login(resultUrl);
}

Console.WriteLine("Logged in.");