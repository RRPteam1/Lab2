using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Security;

namespace SmptClient
{
    //We use ASCII
    public class SmptClient
    {
        public string Host { get; protected set; }
        public int Port { get; protected set; }
        public bool SSL { get; protected set; }

        /// <summary>
        /// Simple Smpt Client using tcp
        /// </summary>
        /// <param name="server">Domain server</param>
        /// <param name="SSL">Use SSL or not</param>
        /// <exception cref="FormatException">Wrong format of server name</exception>
        public SmptClient(string server, bool SSL = false)
        {
            var reg = new Regex(@"^([^\:]+)\:?(\d*)$", RegexOptions.Compiled);
            var match = reg.Match(server);
            if (!match.Success) throw new FormatException();

            this.SSL = SSL;
            Host = match.Groups[1].Value;
            Port = string.IsNullOrEmpty(match.Groups[2].Value) ? (SSL ? 465 : 25) : int.Parse(match.Groups[2].Value); //we can add more checkups later
        }

        /// <summary>
        /// Handshake with server send ehlo to get commands
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task HandshakeAsync(Stream stream)
        {
            //stream set-up
            using StreamReader? reader = new(stream, Encoding.ASCII, false, 4096, true);
            using StreamWriter? writer = new(stream, Encoding.ASCII, 4096, true);
            writer.AutoFlush = true;

            var line = await reader.ReadLineAsync().ConfigureAwait(false);

            if (!line!.StartsWith("220 ")) throw new Exception($"epxected 200 |GOT: {line}");

            writer.WriteLine("EHLO " + Host);
            line = await reader.ReadLineAsync().ConfigureAwait(false);
            while (line!.StartsWith("250-")) //TODO: analyze supported
            {
                line = await reader.ReadLineAsync().ConfigureAwait(false);
            }

            if (!line.StartsWith("250 ")) throw new Exception($"Unable to complete the SMTP handshake |GOT: {line}");
        }

        /// <summary>
        /// Connect to the server
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="ssl"></param>
        /// <returns>Socket IO stream</returns>
        /// <exception cref="Exception"></exception>
        public async Task<Stream> ConnectAsync(string host, int port, bool ssl)
        {
            var client = new TcpClient();
            await client.ConnectAsync(host, port).ConfigureAwait(false);

            if(!ssl) return client.GetStream();

            var ssl_stream = new SslStream(client.GetStream());
            try
            {
                await ssl_stream.AuthenticateAsClientAsync(host).ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return ssl_stream;
        }

        public async Task LoginAsync(Stream stream, string username, string password)
        {
            //stream set-up
            using StreamReader? reader = new(stream, Encoding.ASCII, false, 4096, true);
            using StreamWriter? writer = new(stream, Encoding.ASCII, 4096, true);
            writer.AutoFlush = true;

            await writer.WriteLineAsync("AUTH LOGIN").ConfigureAwait(false); //login request

            string line = (await reader.ReadLineAsync().ConfigureAwait(false))!;
            if (!line!.StartsWith("334 ")) throw new Exception($"Expected 334 response |GOT: {line}");

            //send login
            line = line[4..];
            line = Utils.Base64Decode(line);     
            if (line.ToLower() != "username:") throw new Exception("Expected: username");
            await writer.WriteLineAsync(Utils.Base64Encode(username)).ConfigureAwait(false); 

            //send pass
            line = (await reader.ReadLineAsync().ConfigureAwait(false))!;
            if (!line!.StartsWith("334 ")) throw new Exception($"Expected: 334 response |GOT: {line}");
            line = line[4..];
            line = Utils.Base64Decode(line);
            if (line.ToLower() != "password:") throw new Exception("Expected: password");          
            await writer.WriteLineAsync(Utils.Base64Encode(password)).ConfigureAwait(false);

            //results
            line = (await reader.ReadLineAsync().ConfigureAwait(false))!;
            if (line!.StartsWith("5")) throw new Exception($"Unable to login |GOT: {line}");      
            else if (!line.StartsWith("235 ")) throw new Exception("Login or password is wrong");            
        }

        public async Task SendAsync(Stream stream, Message message)
        {
            using (var reader = new StreamReader(stream, Encoding.ASCII, false, 4096, true))
            using (var writer = new StreamWriter(stream, Encoding.ASCII, 4096, true))
            {
                writer.AutoFlush = true;

                await writer.WriteLineAsync("MAIL FROM: <" + message.From + ">").ConfigureAwait(false);
                string line = (await reader.ReadLineAsync().ConfigureAwait(false))!;
                if (!line!.StartsWith("250 "))
                throw new Exception($"Unexpected response received while sending: MAIL FROM |GOT: {line}");
                

                //recipients
                foreach (string r in message.To!)
                {
                    await writer.WriteLineAsync("RCPT TO: <" + r + ">").ConfigureAwait(false);
                    line = (await reader.ReadLineAsync().ConfigureAwait(false))!;
                    if (!line!.StartsWith("250 "))  throw new Exception($"Unexpected response received while sending：RCPT TO |GOT: {line}");                    
                }

                //prepare text
                await writer.WriteLineAsync("DATA").ConfigureAwait(false);
                line = (await reader.ReadLineAsync().ConfigureAwait(false))!;
                if (!line!.StartsWith("354 ")) throw new Exception($"Unexpected response received while sending：DATA |GOT: {line}");           

                //MIME
                string boundary = "=====SMPT_NextPart" + DateTime.Now.Ticks + "=====";
                string send = "";
                send += "From: \"" + Utils.Base64ExtendedWordEncode(message.Name!) + "\" <" + message.From + ">" + Environment.NewLine;
                send += "To: " + Utils.RcptMerge(message.To.ToArray()) + Environment.NewLine;
                send += "Subject: " + Utils.Base64ExtendedWordEncode(message.Subject!) + Environment.NewLine;
                send += "Mime-Version: 1.0" + Environment.NewLine;
                send += "Content-Type: multipart/mixed;" + Environment.NewLine;
                send += "\tboundary=\"" + boundary + "\"" + Environment.NewLine;
                send += "Content-Transfer-Encoding: 7bit" + Environment.NewLine;
                send += Environment.NewLine;
                send += "This is a multi-part message in MIME format." + Environment.NewLine;
                send += Environment.NewLine + "--" + boundary + Environment.NewLine;

                if (message.IsHtml)
                {
                    send += "Content-Type: text/html; charset=\"utf-8\"" + Environment.NewLine;
                    send += "Content-Transfer-Encoding: base64" + Environment.NewLine;
                    send += Environment.NewLine;
                }
                else
                {
                    send += "Content-Type: text/plain; charset=\"utf-8\"" + Environment.NewLine;
                    send += "Content-Transfer-Encoding: base64" + Environment.NewLine;
                    send += Environment.NewLine;
                }
                send += Utils.Base64Encode(message.Content!) + Environment.NewLine;
                send += Environment.NewLine + "--" + boundary + Environment.NewLine;

                await writer.WriteAsync(send).ConfigureAwait(false);

                //Appending files
                if (message.Files != null && message.Files.Any())
                {
                    int index = 0;
                    byte[] buffer = new byte[1024 * 40 * 3];     //after Base64 encoding =
                    foreach (var file in message.Files)
                    {
                        send = "";
                        send += "Content-Type: application/octet-stream; name=\"" + Utils.Base64ExtendedWordEncode(file.Name!) + "\"" + Environment.NewLine; //TODO: use other types too
                        send += "Content-Transfer-Encoding: base64" + Environment.NewLine;
                        send += "Content-Disposition: attachment; filename=\"" + Utils.Base64ExtendedWordEncode(file.Name!) + "\"" + Environment.NewLine; send += Environment.NewLine;
                        await writer.WriteAsync(send).ConfigureAwait(false);

                        index++;
                        file.Stream!.Position = 0;
                        int read = await file.Stream.ReadAsync(buffer).ConfigureAwait(false);
                        while (read > 0)
                        {
                            string base64 = Convert.ToBase64String(buffer, 0, read);

                            await writer.WriteAsync(Convert.ToBase64String(buffer, 0, read)).ConfigureAwait(false);
                            read = await file.Stream.ReadAsync(buffer).ConfigureAwait(false);
                        }

                        send = "";
                        send += Environment.NewLine;
                        send += Environment.NewLine + "--" + boundary + Environment.NewLine;
                        await writer.WriteAsync(send).ConfigureAwait(false);
                    }
                }

                //Send method
                await writer.WriteAsync("\r\n.\r\n").ConfigureAwait(false);
                line = (await reader.ReadLineAsync().ConfigureAwait(false))!;
                if (!line.StartsWith("250 ")) throw new Exception($"Failed to send |GOT: {line}");              
            }
        }

        public async Task QuitAsync(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.ASCII, false, 4096, true))
            using (var writer = new StreamWriter(stream, Encoding.ASCII, 4096, true))
            {
                writer.AutoFlush = true;

                //send quit
                await writer.WriteAsync("QUIT\r\n").ConfigureAwait(false);
                string line = (await reader.ReadLineAsync().ConfigureAwait(false))!;
                if (!line!.StartsWith("221 ")) throw new Exception($"Can`t disconnect lmao, but I will |GOT: {line}");
            }
        }

        public async Task SendAndQuitAsync(string username, string password, Message message)
        {
            using (var stream = await ConnectAsync(Host, Port, SSL).ConfigureAwait(false))
            {
                await HandshakeAsync(stream).ConfigureAwait(false);
                await LoginAsync(stream, username, password).ConfigureAwait(false);
                await SendAsync(stream, message).ConfigureAwait(false);
                await QuitAsync(stream).ConfigureAwait(false);
            }
        }
    }
}
