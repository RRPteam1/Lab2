string server = "smtp.example.com";
string username = "sender@example.com";
string password = "examplePass";

var message = new SmptClient.Message();

SmptClient.SmptClient smptClient = new(server);
await smptClient.SendAsync(username, password, message);