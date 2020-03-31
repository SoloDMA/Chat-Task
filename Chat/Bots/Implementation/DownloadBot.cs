﻿using Chat.Commands;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using Chat.Bots.Contract;

namespace Chat.Bots.Implementation
{
    class DownloadBot : IBot
    {
        
        private readonly Regex TitleTagPattern = new Regex(@"<title.*?>((.*?)|(\s.*?))+</title>");
        private readonly Regex URLPattern = new Regex(@"(https?:\/\/)?([\w\.]+)\.([a-z]{2,6}\.?)(\/[\w\.]*)*\/?");

        private readonly Dictionary<string, Command> Commands = new Dictionary<string, Command>
        {
            { "site", Command.SITE }
        };

        public DownloadBot()
        {
        }

        public string ExecuteCommand(string command, string argument = null)
        {
            if (!Commands.TryGetValue(command, out var commandType))
                return $"У меня нет команды {command}";

            switch (commandType)
            {
                case Command.SITE:
                    var htmlCode = DownloadWebPage(argument);
                    return TitleTagPattern.Match(htmlCode).Value;

                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        private string DownloadWebPage(string url)
        {
            var site = new WebClient();
            return site.DownloadString(url);
        }

        public string ReactionOnEvent(CommandAndEventType.EventType chatEvent)
        {
            return chatEvent switch
            {
                CommandAndEventType.EventType.SIGNIN =>
                "Привет. Напиши адрес сайта, и я покажу, что находится в его теге <title>?",
                CommandAndEventType.EventType.LOGOUT => $"Пока :)",
                _ => ""
            };
        }

        public string ReactionOnMessage(string message)
        {
            var matchUrl = URLPattern.Match(message);

            if (!matchUrl.Success)
                return null;

            var url = matchUrl.Value;

            var htmlCode = DownloadWebPage(url);

            return TitleTagPattern.Match(htmlCode).Value;
        }

        private enum Command
        {
            SITE = 1
        }

    }
    
}
