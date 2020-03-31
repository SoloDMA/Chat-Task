using System;
using System.Collections.Generic;
using System.Text;
using Chat.Data.Contract;
using Chat.Server;
using Chat.View.Contract;

namespace Chat.Server
{
    class ChatServerHttp : ChatServer
    {

        private readonly string Host = "http://localhost:8080";

        private readonly IDictionary<string, Action> RequestHandler;

        public ChatServerHttp(IChatView view, IChatStorage store, IDictionary<string, BotConfig> bots) : base(view, store, bots)
        {

        }


        
    }
}
