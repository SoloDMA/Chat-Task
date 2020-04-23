using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Chat.Data.Contract;
using Chat.Data.Models;
using Chat.Handlers;
using Chat.View.Contract;

namespace Chat.View.Implementation.HTML
{

    

    class ChatHtmlView : IChatView
    {
        private static readonly string Host = "http://localhost:8080";

        private readonly string[] Prefixes;

        private const string IndexPath = "/index/";
        private const string LoginPath = "/login/";
        private const string PostMessagePath = "/postMessage/";
        private const string DeleteMessagePath = "/deleteMessage/";
        private const string LogoutPath = "/logout/";

        private const char RequestDelimiter = '=';
        private const char GetRequestDelimeter = '&';
        private const char GetRequestDeterminator = '?';
        private const string CookiePath = "/";

        private const string SessionCookieName = "sesionId";

        private const string LoginRequstKey = "login";
        private const string MessageRequestKey = "message";
        private const string DeleteMessageRequestKey = "messageID";

        private const string MainPagePath = "../../../View/Implementation/HTML/MainPage.html";
        private const string LoginPagePath = "../../../View/Implementation/HTML/LoginPage.html";
        private const string ErrorPagePath = "../../../View/Implementation/HTML/ErrorDiscriptionPage.html";

        private readonly string MessageFormat = "\t\t\t<div class=\"message\">\n" +
                                                "\t\t\t\t<div class=\"user-name\">{0}</div>\n" +
                                                "\t\t\t\t<div class=\"message-text\">{1}</div>\n" +
                                                "\t\t\t\t<div class=\"message-time\">{2}</div>\n" +
                                                "\t\t\t\t<div class=\"delete-message-btn\">\n" +
                                                "\t\t\t\t\t<a href=\"http://localhost:8080/deleteMessage/?messageID={3}\">Удалить</a>\n" +
                                                "\t\t\t\t</div>\n" +
                                                "\t\t\t</div>\n";

        private readonly string ErrorMessageFormat =
                                                "\t\t\t<div class=\"message\">\n" +
                                                "\t\t\t\t<div class=\"user-name\">Сообщение об ошибке</div>\n" +
                                                "\t\t\t\t<div class=\"error-message-text\">{0} <a href=\"http://localhost:8080/index/\">Перейдите на главную страницу</a></div>\n" +
                                                "\t\t\t\t<div class=\"message-time\">{1}</div>\n" +
                                                "\t\t\t</div>\n";

        private readonly string MessageLocator = "<!--Messages-->";


        private readonly Dictionary<string, ClientModel> UserSessions;
        private readonly HttpListener Listener;

        private readonly ChatHandler ChatHandlers;


        public ChatHtmlView(IChatStorage storage, IDictionary<string, BotConfig> bots)
        {
            ChatHandlers = new ChatHandler(storage, bots);

            if (!HttpListener.IsSupported)
                return;

            Listener = new HttpListener();
            UserSessions = new Dictionary<string, ClientModel>();

            Prefixes = new string[]
            {
                Host + IndexPath,
                Host + LoginPath,
                Host + PostMessagePath,
                Host + DeleteMessagePath,
                Host + LogoutPath
            };

            foreach (var prefix in Prefixes)
            {
                Listener.Prefixes.Add(prefix);
            }

        }

        #region MainMethods
        public void Start()
        {
            Listener.Start();
            ChatHandlers.GetMessages();
            while (true)
            {
                var listenerContext = Listener.GetContext();
                //Thread.Sleep(1500);
                //ContextProcessing(listenerContext);
                new Thread(
                    () =>
                    {
                        ContextProcessing(listenerContext);
                    }
                ).Start();
            }
        }

        private void ContextProcessing(HttpListenerContext context)
        {
            var requestPath = context.Request.Url.LocalPath;
            string error = null;
            switch (requestPath)
            {
                case IndexPath:
                    IndexHandler(context);
                    return;

                case LoginPath:
                    var clientName = LoginHandler(context, out var SessionId);
                    var newClientObject = ChatHandlers.SignInChat(clientName, out error);
                    if (newClientObject != null)
                    {
                        UserSessions.Add(SessionId, newClientObject);
                    }
                    break;

                case LogoutPath:
                    var clientObject = LogoutHandler(context);
                    ChatHandlers.LogOutChat(clientObject, out error);
                    break;

                case PostMessagePath:
                    clientObject = PostMessageHandler(context, out var userMessage, out error);
                    if (userMessage != null)
                        ChatHandlers.AddMessageChat(clientObject, userMessage, out error);
                    break;

                case DeleteMessagePath:
                    var messageId = DeleteMessageHandler(context);
                    ChatHandlers.DeleteMessageChat(messageId, out error);
                    break;

            }

            if (error == null)
                Redirect(context, Host + IndexPath);
            else
                ErrorHandler(context, error);

        }
        #endregion

        #region RequestHandlers
        private void IndexHandler(HttpListenerContext context)
        {
            context.Response.ContentType = "text/html; charset=utf-8";

            using var ResponseStream = context.Response.OutputStream;

            var Page = GetHtmlPage(context);

            ResponseStream.Write(Page, 0, Page.Length);

            context.Response.Close();
        }

        private void ErrorHandler(HttpListenerContext context, string error)
        {
            context.Response.ContentType = "text/html; charset=utf-8";

            var ErrorPage = Encoding.UTF8.GetString(GetHTMLFromFile(ErrorPagePath));
            var ErrorDiscription = string.Format(ErrorMessageFormat, error, DateTime.Now);
            
            var LocatorPosition = ErrorPage.IndexOf(MessageLocator);
            ErrorPage = ErrorPage.Insert(LocatorPosition, ErrorDiscription);

            var ErrorPageBytes = Encoding.UTF8.GetBytes(ErrorPage);
            context.Response.OutputStream.Write(ErrorPageBytes, 0, ErrorPageBytes.Length);

            context.Response.Close();
        }

        private string LoginHandler(HttpListenerContext context, out string SessionId)
        {
            var RequestDictinary = ParseRequestBody(GetRequestBody(context.Request));

            var Username = RequestDictinary[LoginRequstKey];


            SessionId = Guid.NewGuid().ToString();

            var Cookie = new Cookie() { Name = SessionCookieName, Value = SessionId, Expires = DateTime.Now.AddDays(30), Path = CookiePath };
            SetCookies(new CookieCollection() { Cookie }, context.Response);


            return Username;
        }

        private ClientModel PostMessageHandler(HttpListenerContext context, out string message, out string error)
        {
            if (!CheckCookies(context.Request.Cookies))
            {
                error = "Произошла ошибка. Вы не авторизованы и не можете писать соощения в чат.";
                message = null;
                return null;
            }
                
            var RequestDictinary = ParseRequestBody(GetRequestBody(context.Request));
            var MessageText = RequestDictinary[MessageRequestKey];

            var Cookie = context.Request.Cookies;
            var SessionKey = Cookie[SessionCookieName].Value;

            var UserObject = UserSessions[SessionKey];
            
            if (!ChatRegExes.BotCommandPattern.IsMatch(MessageText))
            {
                error = null;
                message = MessageText;
            }
            else
            {
                CallBot(UserObject, in MessageText, out error);
                message = null; 
            }


            return UserObject;
        }

        private int DeleteMessageHandler(HttpListenerContext context)
        {
            var RequestDictinary = ParseRequestBody(GetRequestBody(context.Request));

            if (!CheckCookies(context.Request.Cookies))
                throw new CookieException();

            var MessageID = int.Parse(RequestDictinary[DeleteMessageRequestKey]);


            return MessageID;
        }

        private ClientModel LogoutHandler(HttpListenerContext context)
        {
            var SessionCookie = context.Request.Cookies[SessionCookieName];
            var SessionKey = SessionCookie.Value;
            var UserObject = UserSessions[SessionKey];
            
            UserSessions.Remove(SessionKey);

            //Чтобы удалить куки, нужно в ответ добавить новые куки с таким же именем, но в Expires = DateTime.Now.AddDays(-1)
            var DeletedCookie = new Cookie() { Name = SessionCookieName, Expires = DateTime.Now.AddDays(-1), Path = CookiePath };
            SetCookies(new CookieCollection() { DeletedCookie }, context.Response);


            return UserObject;
        }
        #endregion

        #region HelperMethods
        private byte[] GetHtmlPage(HttpListenerContext context)
        {

            if (CheckCookies(context.Request.Cookies))
            {
                var MainPageStr = Encoding.UTF8.GetString(GetHTMLFromFile(MainPagePath));
                var Messages = ChatHandlers.GetMessages();
                foreach (var message in Messages)
                {
                    var LocatorPosition = MainPageStr.IndexOf(MessageLocator);
                    var FormattedMessage = string.Format(MessageFormat, message.UserName, message.TextOfMessage, message.TimeOfMessage, message.MessageID);
                    MainPageStr = MainPageStr.Insert(LocatorPosition, FormattedMessage);
                }

                return Encoding.UTF8.GetBytes(MainPageStr);
            }
            else
            {
                return GetHTMLFromFile(LoginPagePath);
            }
        }

        private byte[] GetHTMLFromFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Файл не найден!");

            using var FileStream = File.OpenRead(path);
            var Buffer = new byte[FileStream.Length];
            FileStream.Read(Buffer, 0, Buffer.Length);

            return Buffer;
        }

        private void CallBot(ClientModel userObject, in string message, out string error)
        {

            var botCommandMatch = ChatRegExes.BotCommandPattern.Match(message);

            var botName = botCommandMatch.Groups[ChatRegExes.BotCommandPatternBotNameGroup].Value;
            var botCommand = botCommandMatch.Groups[ChatRegExes.BotCommandPatternBotCommandGroup].Value;
            var commandArgument = botCommandMatch.Groups[ChatRegExes.BotCommandPatternBotCommandArgumentGroup].Value;

            ChatHandlers.CallBot(userObject, botName, botCommand, commandArgument, out error);

        }

        private bool CheckCookies(CookieCollection cookies)
        {
            if (cookies == null)
                throw new ArgumentNullException();

            var SessionCookie = cookies[SessionCookieName];
            if (SessionCookie == null)
                return false;

            if (!UserSessions.ContainsKey(SessionCookie.Value))
                return false;

            return true;
        }

        private void Redirect(HttpListenerContext context, string redirectPath)
        {
            context.Response.Redirect(redirectPath);
            context.Response.Close();
        }


        private void SetCookies(CookieCollection cookies, HttpListenerResponse response)
        {
            if (cookies == null)
                throw new ArgumentNullException("Не созданы cookies");
            if (cookies.Count == 0)
                throw new ArgumentException("Cookies не заполнены");

            response.Cookies = cookies;
        }
        #endregion

        #region RequestParsers
        private List<string> GetRequestBody(HttpListenerRequest request)
        {
            return request.HttpMethod switch
            {
                "POST" => ParsePostRequest(request),
                "GET" => ParseGetRequest(request),
                _ => null
            };
        }

        private List<string> ParsePostRequest(HttpListenerRequest request)
        {
            using var RequestStream = request.InputStream;
            using var StreamReader = new StreamReader(RequestStream);
            var RequestBody = new List<string>();
            while (!StreamReader.EndOfStream)
            {
                RequestBody.Add(StreamReader.ReadLine());
            }

            return RequestBody;
        }

        private List<string> ParseGetRequest(HttpListenerRequest request)
        {
            var QueryRequest = request.Url.Query.Trim(GetRequestDeterminator);
            var RequestItems = QueryRequest.Split(GetRequestDelimeter);
            var RequestBody = new List<string>();

            foreach (var Item in RequestItems)
            {
                RequestBody.Add(Item);
            }
            return RequestBody;
        }

        private Dictionary<string, string> ParseRequestBody(List<string> requestBody)
        {
            var RequestDictionary = new Dictionary<string, string>();

            foreach (var requestItem in requestBody)
            {
                var DelimiterPos = requestItem.IndexOf(RequestDelimiter);
                var KeyLength = DelimiterPos;
                var ValueLength = requestItem.Length - DelimiterPos - 1;

                var Key = requestItem.Substring(0, KeyLength);
                var Value = requestItem.Substring(DelimiterPos + 1, ValueLength);

                RequestDictionary.Add(Key, Value);
            }

            return RequestDictionary;
        }
        #endregion

    }

}
