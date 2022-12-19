string server = "26.158.205.10:28"; //we can use ip and host for example: 192.168.0.1:3000 or server.example.com
string username = "sender@example.com";
string password = "examplePass";


var file1 = new SmptClient.Attachment();
file1.Name = "atachment 1";
FileStream fstream = File.OpenRead(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/" + "example.png"!); //actually here will be a path to the file

byte[] buff = new byte[fstream.Length];
await fstream.ReadAsync(buff, 0, buff.Length);
file1.Stream = fstream;


SmptClient.Attachment[] files = new SmptClient.Attachment[]
{
    file1,
};


var message = new SmptClient.Message()
{
    Name = "Example",
    From = username,
    To = new string[] { "recv1@example.com", "recv2@example.com" },

    Subject = "Test",
    Content = "<h1>Hello world!</h1>\n<p>this is same p tag.</p>",
    IsHtml = true,
    Files = files,
};


SmptClient.SmptClient smptClient = new(server);
await smptClient.SendAsync(username, password, message);