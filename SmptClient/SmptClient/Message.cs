namespace SmptClient
{
    public class Message
    {
        public string? Name { get; set; } //name of sender
        public string? From { get; set; } //mail of sender
        public IEnumerable<string>? To { get; set; } //array of mails of recivers     
        public string? Subject { get; set; } //subject of message
        public string? Content { get; set; } //content of message

        public bool IsHtml { get; set; }
        public IEnumerable<Attachment>? Files { get; set; } //attached files (name, stream_data)
    }
}
