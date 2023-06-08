// See https://aka.ms/new-console-template for more information

using System.Text;
using System.Text.Json;

Console.WriteLine("Ask DyChat:");

var treesLayer = new DyLayer("Special_Tree_Layer", new List<DyField>() {new DyField("Tree_Name", "Tree Name", "string"), new DyField("TT", "Type", "string")});
var parcelLayer = new DyLayer("My_Parcels", new List<DyField>() {new DyField("Parcel_Name", "Parcel Name", "string")});

var context = new DyChatContext(new List<DyLayer>() {treesLayer, parcelLayer}, "My_Parcels");

var messages = new DyChatMessages(new List<DyChatMessage>()
{
    new DyChatMessage("How do I count trees in each parcel?", DyChatMessageType.User)
});

// Add this if you want to see how it looks with multiple requests
messages.Messages.Add(new DyChatMessage(Sender: DyChatMessageType.Bot, Content: """
To count the trees in each parcel, you can use the `Intersects()` and `Count()` functions within Arcade. In this example, we will count the number of trees from the "Special_Tree_Layer" that intersect with the current parcel feature:

```arcade
var treesLayer = FeatureSetByName($map, "Special_Tree_Layer");
var intersectingTrees = Intersects(treesLayer, $feature);
var treeCount = Count(intersectingTrees);

return {
    type: 'text',
    text: "Number of trees in parcel: " + treeCount
}
```
"""));
messages.Messages.Add(new DyChatMessage("filter it only for tree type \"Redwood\"", DyChatMessageType.User));

// loop through the messages and print them to the console
foreach (var message in messages.Messages)
{
    Console.WriteLine(message.Content);
}

Console.WriteLine("And now the response:");

var request = new DyRequest(messages, context);


// create a new http client and send the request
var client = new HttpClient();
//var requestBody = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5231/api/v1/NotSkynet/GetAsStream/ArcadePopups");
var requestBody = new HttpRequestMessage(HttpMethod.Post, "https://dymaptic-skynet.azurewebsites.net/api/v1/NotSkynet/GetAsStream/ArcadePopups");
requestBody.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
requestBody.Headers.Add("Authorization", "Bearer b6b5c58afdde4f30850ba2212472896d9221ba81ff3a443aa58e6ef06d549a81");

var response = await client.SendAsync(requestBody, 
    HttpCompletionOption.ResponseHeadersRead);

char[] ReadUTF8Char(BinaryReader s)
{
    byte[] bytes = new byte[4];
    var enc = new UTF8Encoding(false, true);
    if (1 != s.Read(bytes, 0, 1))
        return null;
    if (bytes[0] <= 0x7F) //Single byte character
    {
        return enc.GetChars(bytes, 0, 1);
    }
    else
    {
        var remainingBytes =
            ((bytes[0] & 240) == 240) ? 3 : (
                ((bytes[0] & 224) == 224) ? 2 : (
                    ((bytes[0] & 192) == 192) ? 1 : -1
                ));
        if (remainingBytes == -1)
            return null;
        s.Read(bytes, 1, remainingBytes);
        return enc.GetChars(bytes, 0, remainingBytes + 1);
    }
}

// read the response via a stream reader and write it out to the console as the stream is populated
await using var sw = new StreamWriter(Console.OpenStandardOutput());
using var sr = new BinaryReader(response.Content.ReadAsStream(), Encoding.UTF8, false);
// while the stream is open, keep reading it
// TODO: I'm not sure how to detect the end of the stream. I know that it does end b/c if you don't read it like this
// you will eventually just get the entire response back when it finishes on the server, but I don't know how
// to figure out when that is via the stream or the response object.
while (sr.BaseStream.CanRead)
{
    // I stole this from the internet, there might be a better way to read this stream, you might checkout the
    // Azure.OpenAI package to see how they do it, because they do something similar where they are reading a stream
    var c = ReadUTF8Char(sr);
    //var line = await sr.ReadLineAsync();
    await sw.WriteAsync(c);
    await sw.FlushAsync();
}
