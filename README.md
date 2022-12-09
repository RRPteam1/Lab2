# Документация к проекту
Данный проект является документацией к Smpt клиенту. С асинхронной реализацией и защитой UI thread. 
[Что такое ConfigureAwait(false)](https://devblogs.microsoft.com/dotnet/configureawait-faq/)

# Оглавление
- **Глава I**
  - [Рукопожатие](#Рукопожатие)
  - [Подключение](#Подключение)
- **Глава II**
  - [Регистрация](#Регистрация)
  - [Сообщение](#Сообщение)
- **Глава III**
  - [Утилиты](#Утилиты)
- **Заключение**
  - [Использование](#Использование)

 
## Рукопожатие
Для того, чтобы установить рукопожатие с сервером используется команда "EHLO". Если сервер отвечает 250- значит соединение было установлено и можно обработать команды, которыми сервер обладает. Чтобы считывать и отправлять что-то используется StreamReader и StreamWriter соответсвенно.

```c#
using StreamReader? reader = new(stream, Encoding.ASCII, false, 4096, true);
```
### Параметры
- Считываемый поток
- Кодировка символов ASCII
- Значение для того, чтобы не осуществялть поиск меток порядка байтов в начале файла
- Минимальный размер буфера 4096
- Значение true, чтобы оставить поток открытым после удаления объекта

```c#
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
```

## Подключение
Используя TcpClient и аргуемент использования SSL. Если SSL не испоьзуется, тогда просто используем поток, который и так есть, иначе используя SslStream защищаем поток, который служит для взаимодействия между клиентом и сервером и использует протокол безопасности SSL для проверки подлинности сервера и при необходимости клиента.

```c#
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
```

## Регистрация
"AUTH LOGIN" - это команды для того, чтобы совершить авторизацию и вход. Используется base64 для того, чтобы отправлять логин и пароль серверу.
- Если сервер отвечает 334 (т.е. он поддерживает Base64-encoded challenge)
- Если сервер отвечает 235 (то пароль или логин не верны)
- Если сервер отвечает 5хх (то произошла ошибка во время подключения или отправки)
```c#
line = line[4..];
```
Данная подстрока нужна для того, чтобы отбросить ответ сервера с пробелом к примеру: "334 "

```c#
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
```

## Сообщение
Это самый обычный DTO (Data Transfer Object) для удобства отправки сообщений. Т.к. в качестве recpt можно указать несколько получателей, то используется массив строк для передачи. Используется IEnumerable, т.к. изменение в этот момент не должно происходить.
```c#
public class Message
    {
        string From { get; set; } //mail of sender
        IEnumerable<string> To { get; set; } //array of mails of recivers     
        string Subject { get; set; } //subject of message
        string Content { get; set; } //content of message

        //todo: bool is html
        //todo: list of attached files
    }
```
Далее будет также использоваться класс для прикрепленных файлов.

## Утилиты
### Кодировка в Base64
```c#
public static string Base64Encode(string data, Encoding e = null)
        {
            if (data == null) return "";

            if (e == null) e = Encoding.UTF8;
            byte[] buffer = e.GetBytes(data);
            return Convert.ToBase64String(buffer);
        }
```

### Декодировка из Base64
```c#
public static string Base64Decode(string base64, Encoding e = null)
        {
            if (base64 == null) return "";

            if (e == null) e = Encoding.UTF8;
            byte[] buffer = Convert.FromBase64String(base64);
            return e.GetString(buffer);
        }
```

### Кодировка в Base64 EXTENDED
```c#
 public static string Base64ExtendedWordEncode(string data, Encoding e = null)
        {
            if (data == null) return "";

            if (e == null) e = Encoding.UTF8;
            return "=?" + e.HeaderName.ToUpper() + "?B?" + Base64Encode(data, e) + "?=";
        }
```

### Метод для указания кому отправлять
```c#
public static string RcptMerge(string[] to)
        {
            string retval = "";
            if (to == null) return retval;

            int index;
            for (index = 0; index < to.Length - 1; index++)          
                retval += "<" + to[index] + ">, ";
            
            retval += "<" + to[index] + ">";
            return retval;
        }
```

# Использование
```c#
string server = "smtp.example.com";
string username = "sender@example.com";
string password = "examplePass";

var message = new SmptClient.Message();

SmptClient.SmptClient smptClient = new(server);
await smptClient.SendAsync(username, password, message);
```
