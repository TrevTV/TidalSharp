using TidalSharp;

var client = new TidalClient();
var url = client.Session.GetPkceLoginUrl();
Console.WriteLine(url);

Console.WriteLine("Enter resulting url: ");
var resultUrl = Console.ReadLine()!;

await client.Session.LoginWithRedirectUri(resultUrl);