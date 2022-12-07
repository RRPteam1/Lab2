using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        private async Task HandshakeAsync(Stream stream)
        {
            //stream set-up
            using StreamReader? reader = new(stream, Encoding.ASCII, false, 4096, true);
            using StreamWriter? writer = new(stream, Encoding.ASCII, 4096, true);
            writer.AutoFlush = true;

            var line = await reader.ReadLineAsync().ConfigureAwait(false);

            //todo: make class of exeptions and check them
            if (!line.StartsWith("220 ")) throw new Exception("200 error code expetion");

            writer.WriteLine("EHLO " + Host);
            line = await reader.ReadLineAsync().ConfigureAwait(false);
            while (line.StartsWith("250-")) //TODO: analyze supported
                line = await reader.ReadLineAsync().ConfigureAwait(false);

            if (!line.StartsWith("250 ")) throw new Exception("Unable to complete the SMTP handshake");
        }

        /// <summary>
        /// Connect to the server
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="ssl"></param>
        /// <returns>Socket IO stream</returns>
        /// <exception cref="Exception"></exception>
        private async Task<Stream> ConnectAsync(string host, int port, bool ssl)
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

        private async Task LoginAsync(Stream stream, string username, string password)
        {
            //stream set-up
            using StreamReader? reader = new(stream, Encoding.ASCII, false, 4096, true);
            using StreamWriter? writer = new(stream, Encoding.ASCII, 4096, true);
            writer.AutoFlush = true;

            await writer.WriteLineAsync("AUTH LOGIN").ConfigureAwait(false); //login request

            string line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (!line.StartsWith("334 ")) throw new Exception("Expected 334 response");

            //send login
            line = line[4..];
            line = Utils.Base64Decode(line);     
            if (line.ToLower() != "username:") throw new Exception("Expected: username");
            await writer.WriteLineAsync(Utils.Base64Encode(username)).ConfigureAwait(false); 

            //send pass
            line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (!line.StartsWith("334 ")) throw new Exception("Expected: 334 response");
            line = line[4..];
            line = Utils.Base64Decode(line);
            if (line.ToLower() != "password:") throw new Exception("Expected: password");          
            await writer.WriteLineAsync(Utils.Base64Encode(password)).ConfigureAwait(false);

            //results
            line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line.StartsWith("5")) throw new Exception("Unable to login");      
            else if (!line.StartsWith("235 ")) throw new Exception("Login or password is wrong");            
        }

        public async Task SendAsync(string username, string password, Message message)
        {
            using (var stream = await ConnectAsync(Host, Port, SSL).ConfigureAwait(false))
            {
                await HandshakeAsync(stream).ConfigureAwait(false);
                await LoginAsync(stream, username, password).ConfigureAwait(false);
                //todo: create and use await on SendAsync(message).ConfigureAwait(false);
            }
        }
    }
}
