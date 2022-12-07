namespace SmptClient
{
    public class Message
    {
        string From { get; set; } //mail of sender
        IEnumerable<string> To { get; set; } //array of mails of recivers     
        string Subject { get; set; } //subject of message
        string Content { get; set; } //content of message

        //todo: bool is html
        //todo: list of attached files
    }
}
