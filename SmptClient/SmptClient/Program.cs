string server = "smtp.mail.ru:465"; //we can use ip and host for example: 26.158.205.10:28 or smtp.mail.ru:465
string username = "********"; 
string password = "********";

Stream FileOpener(string name) //gets file from desktop
{
    FileStream fstream = File.OpenRead(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/" + name!); //actually here will be a path to the file

    byte[] buff = new byte[fstream.Length];
    fstream.Read(buff, 0, buff.Length);
    return fstream;
}

void MainMenu() => Console.WriteLine("All comands:\nMESG - set message\nSEND - sends message\nQUIT - quits");
void MessageMenu() => Console.WriteLine("\nParams of message:\nName\nSubject\nTo\nContent\nisHtml\nFiles\nDone");


SmptClient.SmptClient smptClient = new(server, true); //set true to use for mail.ru
using (var stream = await smptClient.ConnectAsync(smptClient.Host, smptClient.Port, smptClient.SSL).ConfigureAwait(false))
{
    await smptClient.HandshakeAsync(stream).ConfigureAwait(false);
    await smptClient.LoginAsync(stream, username, password).ConfigureAwait(false);

    SmptClient.Message msg = new();
    List<SmptClient.Attachment> attachments = new();
    List<string> to_list = new();

    var end = true;
    MainMenu();
    while (end)
    {
        Console.Write("> "); var command = Console.ReadLine();
        switch (command)
        {
            case "HELP" or "help":
                MainMenu();
                break;

            case "CLS" or "cls":
                Console.Clear();
                break;

            case "MESG" or "mesg":
                MessageMenu();
                msg = new();
                var ender = true;
                while (ender)
                {
                    Console.Write(">> ");
                    var param = Console.ReadLine();
                    switch (param)
                    {
                        case "Name" or "name":
                            Console.Write(">>> ");
                            msg.Name = Console.ReadLine();
                            break;

                        case "Subject" or "subject":
                            Console.Write(">>> ");
                            msg.Subject = Console.ReadLine();
                            break;

                        case "To" or "to":
                            var to_while = true;
                            while (to_while)
                            {
                                Console.WriteLine("\nTo commands:\nAdd\nPrint\nClear\nDelete\nDone - if u are done with editor of rcvs");
                                Console.Write(">>> ");
                                var input_rcv = Console.ReadLine();
                                switch (input_rcv)
                                {
                                    case "Add" or "add":
                                        Console.Write("Enter name of rcv: "); var name = Console.ReadLine()!;
                                        to_list.Add(name);
                                        break;

                                    case "Print" or "print":
                                        Console.WriteLine("RCVS list:");
                                        to_list.ForEach(x => global::System.Console.WriteLine(x));
                                        break;

                                    case "Clear" or "clear":
                                        to_list.Clear();
                                        break;

                                    case "Delete" or "delete":
                                        Console.Write("Enter name of rcvr to remove from the list: "); var delete = Console.ReadLine()!;
                                        var element = to_list.Find(x => x.Equals(delete));
                                        if (element != null) to_list.Remove(element);
                                        else Console.WriteLine("RCVR not found!");
                                        break;

                                    default:
                                        Console.WriteLine("Error command!");
                                        break;

                                    case "Done" or "done":
                                        msg.To = to_list.ToArray();
                                        msg.From = username;
                                        to_while = false;
                                        break;
                                }
                            }
                            break;

                        case "Content" or "content":
                            Console.Write(">>> ");
                            msg.Content = Console.ReadLine();
                            break;

                        case "isHtml" or "IsHtml" or "ishtml":
                        ishtml_label:
                            Console.WriteLine("true\nfalse");
                            Console.Write(">>> ");
                            var input_html = Console.ReadLine()!;
                            if (input_html.Equals("true")) msg.IsHtml = true;
                            else if (input_html.Equals("false")) msg.IsHtml = false;
                            else goto ishtml_label;
                            break;

                        case "Files" or "files":
                            var some = true;
                            while (some)
                            {
                                Console.WriteLine("\nFiles commands:\nAdd\nPrint\nClear\nDelete\nDone - if u are done with editor of files");
                                Console.Write(">>> ");
                                var input_files = Console.ReadLine();
                                switch (input_files)
                                {
                                    case "Add" or "add":
                                        Console.Write("Enter name of file: "); var name = Console.ReadLine()!;
                                        attachments.Add(new SmptClient.Attachment() { Name = name, Stream = FileOpener(name) });
                                        break;

                                    case "Print" or "print":
                                        Console.WriteLine("Files list:");
                                        attachments.ForEach(x => global::System.Console.WriteLine(x.Name));
                                        break;

                                    case "Clear" or "clear":
                                        attachments.Clear();
                                        break;

                                    case "Delete" or "delete":
                                        Console.Write("Enter name of file to remove from the list (with extension): "); var delete = Console.ReadLine()!;
                                        var element = attachments.Find(x => x.Name.Equals(delete));
                                        if (element != null) attachments.Remove(element);
                                        else Console.WriteLine("File not found!");
                                        break;

                                    default:
                                        Console.WriteLine("Error command!");
                                        break;

                                    case "Done" or "done":
                                        msg.Files = attachments.ToArray();
                                        some = false;
                                        break;
                                }
                            }
                            break;

                        case "Done" or "done":
                            ender = false;
                            break;
                    }
                }
                break;

            case "default":
                attachments.Add(new SmptClient.Attachment() { Name = "abz.png", Stream = FileOpener("abz.png") });
                to_list.Add("tester_smtp@bk.ru");
                msg.Name = "example_name";
                msg.Subject = "example_subject";
                msg.To = to_list.ToArray();
                msg.Content = "<h1>example_content using html</h1>";               
                msg.IsHtml = true;
                msg.Files = attachments.ToArray();
                msg.From = username;
                break;

            case "SEND" or "send":
                if (msg.To == null) { Console.WriteLine("Message is empty!"); break; }
                await smptClient.SendAsync(stream, msg).ConfigureAwait(false);
                break;

            case "QUIT" or "quit":
                await smptClient.QuitAsync(stream).ConfigureAwait(false);
                end = false;
                attachments.ForEach(x => x.Stream!.Close()); //just to be shure that we closed all streams
                break;

            default:
                Console.WriteLine("WRONG Command!");
                break;
        }
    }
     
}
//we can do it like this and close connection: await smptClient.SendAsyncAndQuit(username, password, message);
