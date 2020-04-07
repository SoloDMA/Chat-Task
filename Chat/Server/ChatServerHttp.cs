using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using Chat.Data.Contract;
using Chat.Server;
using Chat.View.Contract;
using System.IO;

namespace Chat.Server
{
    class ChatServerHttp : ChatServer
    {

        private static readonly string Host = "http://localhost:8080";

        private readonly string[] Prefixes;

        private string IndexPath = "/index/";
        private string LoginPath = "/login/";
        private string PostMessagePath = "/postMessage/";
        private string DeleteMessagePath = "/deleteMessage/";
        private string LogoutPath = "/logout/";

        private readonly char RequestDelimiter = '=';
        private readonly char GetRequestDelimeter = '&';
        private readonly char GetRequestDeterminator = '?';
        private readonly string CookiePath = "/";

        private readonly string SessionCookieName = "sesionId";

        private readonly string SignInCommandFormat = "signin @{0}";
        private readonly string LogoutCommandFormat = "logout @{0}";
        private readonly string AddMessageCommandFormat = "add-mes @{0} {1}";
        private readonly string DeleteMessageCommandFormat = "del-mes {0}";
        private readonly string BotCommandFormat = "bot @{0} {1}";

        private readonly string LoginRequstKey = "login";
        private readonly string MessageRequestKey = "message";
        private readonly string DeleteMessageRequestKey = "messageID";

        private readonly IDictionary<string, Action<HttpListenerContext>> RequestHandler;
        private readonly HttpListener Listener;

        private Dictionary<string, string> Sessions;

        public ChatServerHttp(IChatView view, IChatStorage store, IDictionary<string, BotConfig> bots) : base(view, store, bots)
        {
            if (!HttpListener.IsSupported)
                return;

            Listener = new HttpListener();
            Sessions = new Dictionary<string, string>();

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

            RequestHandler = new Dictionary<string, Action<HttpListenerContext>>
            {
                {IndexPath, IndexHandler},
                {LoginPath, LoginHandler},
                {PostMessagePath, PostMessageHandler},
                {DeleteMessagePath, DeleteMessageHandler},
                {LogoutPath, LogoutHandler}
            };

            
        }

        public override void StartServer()
        {
            Listener.Start();
            WorkingServer();
        }

        public void StartListening()
        {
            Listener.Start();
            Console.WriteLine("Listening...");
        }


        protected override void WorkingServer()
        {
            StartListening();
            while (true)
            {
                var listenerContext = Listener.GetContext();

                var requestPath = listenerContext.Request.Url.LocalPath;
                RequestHandler[requestPath].Invoke(listenerContext);

            }
        }

        #region RequestHandlers
        private void IndexHandler(HttpListenerContext context)
        {
            SendHtmlPage(context);
        }

        
        private void LoginHandler(HttpListenerContext context)
        {
            var RequestDictinary = ParseRequestBody(GetRequestBody(context.Request));

            var Username = RequestDictinary[LoginRequstKey];
            

            using var Response = context.Response;

            var SessionId = Guid.NewGuid().ToString();
            Sessions.Add(SessionId, Username);

            var Cookie = new Cookie() { Name = SessionCookieName, Value = SessionId, Expires = DateTime.Now.AddDays(30), Path = CookiePath };
            SetCookies(new CookieCollection() {Cookie}, Response);

            MessageProcessing(string.Format(SignInCommandFormat, Username));

            Redirect(context, Host + IndexPath);
        }

        private void PostMessageHandler(HttpListenerContext context)
        {
            var RequestDictinary = ParseRequestBody(GetRequestBody(context.Request));

            var Cookie = context.Request.Cookies;
            var SessionKey = Cookie[SessionCookieName].Value;

            var SenderUsername = Sessions[SessionKey];
            var MessageText = RequestDictinary[MessageRequestKey];
            if (BotCommandPattern.IsMatch(MessageText))
            {
                MessageProcessing(string.Format(BotCommandFormat, SenderUsername, MessageText));
            }
            else
            {
                MessageProcessing(string.Format(AddMessageCommandFormat, SenderUsername, MessageText));
            }


            Redirect(context, Host + IndexPath);
        }

        private void DeleteMessageHandler(HttpListenerContext context)
        {
            var RequestDictinary = ParseRequestBody(GetRequestBody(context.Request));
            //сделать проверку пользователя
            var MessageID = RequestDictinary[DeleteMessageRequestKey];
            MessageProcessing(string.Format(DeleteMessageCommandFormat, MessageID));

            Redirect(context, Host + IndexPath);
        }

        private void LogoutHandler(HttpListenerContext context)
        {
            var DeletedCookie = context.Request.Cookies[SessionCookieName];
            var SessionKey = DeletedCookie.Value;
            var Username = Sessions[SessionKey];

            Sessions.Remove(SessionKey);

            MessageProcessing(string.Format(LogoutCommandFormat, Username));

            
            Redirect(context, Host + IndexPath);
        }
        #endregion

        private void Redirect(HttpListenerContext context, string redirectPath)
        {
            context.Response.Redirect(redirectPath);
            context.Response.Close();
        }

        private void SendHtmlPage(HttpListenerContext context)
        {
            using var Response = context.Response;
            Response.ContentType = "text/html; charset=utf-8";

            using var ResponseStream = Response.OutputStream;
            var Page = GetHtmlPage(context);


            ResponseStream.Write(Page, 0, Page.Length);

            Response.Close();
        }



        private byte[] GetHtmlPage(HttpListenerContext context)
        {
            if (CheckCookies(context.Request.Cookies))
            {
                return Encoding.UTF8.GetBytes(View.ReadHtml(Pages.MAIN));
            }
            else
            {
                return Encoding.UTF8.GetBytes(View.ReadHtml(Pages.LOGIN));
            }
        }

        private bool CheckCookies(CookieCollection cookies)
        {
            if (cookies == null)
                throw new ArgumentNullException();

            var SessionCookie = cookies[SessionCookieName];
            if (SessionCookie == null)
                return false;

            if (!Sessions.ContainsKey(SessionCookie.Value))
                return false;

            return true;
        }

        private void SetCookies(CookieCollection cookies, HttpListenerResponse response)
        {
            if (cookies == null)
                throw new ArgumentNullException("Не созданы cookies");
            if (cookies.Count == 0)
                throw new ArgumentException("Cookies не заполнены");

            response.Cookies = cookies;
        }


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
            var RequestBody  = new List<string>();

            foreach (var Item in RequestItems)
            {
                RequestBody.Add(Item);
            }
            return RequestBody;
        }

        private Dictionary<string, string> ParseRequestBody(List<string> requestBody)
        {
            var RequestDictionary = new Dictionary<string, string>();
            
            foreach(var requestItem in requestBody)
            {
                var DelimiterPos = requestItem.IndexOf(RequestDelimiter);
                var KeyLength    = DelimiterPos;
                var ValueLength  = requestItem.Length - DelimiterPos - 1;

                var Key   = requestItem.Substring(0, KeyLength);
                var Value = requestItem.Substring(DelimiterPos + 1, ValueLength);

                RequestDictionary.Add(Key, Value);
            }

            return RequestDictionary;
        }
    }
}
