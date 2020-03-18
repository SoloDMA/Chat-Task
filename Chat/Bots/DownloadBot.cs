using Chat.Commands;
using Chat.View;
using System;
using System.Net;
using System.Text.RegularExpressions;

namespace Chat.Bots
{
    class DownloadBot : IBot
    {
        public string BotName { get; set; }
        private readonly Regex TitleTagPattern = new Regex(@"<title.*?>((.*?)|(\s.*?))+</title>");
        private readonly Regex URLPattern = new Regex(@"(https?:\/\/)?([\w\.]+)\.([a-z]{2,6}\.?)(\/[\w\.]*)*\/?");
        public DownloadBot(string Name, params string[] CommandsAndtriggersForThatBot)
        {
            BotName = Name;
            foreach (var commandOrTrigger in CommandsAndtriggersForThatBot)
            {
                if (commandOrTrigger.StartsWith('/'))
                {
                    CommandsForBot.AddNewCommand(commandOrTrigger, this);
                }
                else
                {
                    CommandsForBot.AddNewTrigger(commandOrTrigger, this);
                }

            }
        }

        public string Execute(string command)
        {
            if (CommandsForBot.GetTypeOfCommand(command) is DownloadBot)
            {
                ConsoleView.Write("Введите адрес сайта...");
                string URL = ConsoleView.Read();
                string HTMLCode = ConnectToSite(URL);
                ConsoleView.Clear();
                return TitleTagPattern.Match(HTMLCode).Value;
            }
            else
            {
                return $"У {BotName} нет команды {command}";
            }

        }

        private string ConnectToSite(string URL)
        {
            WebClient Site = new WebClient();
            return Site.DownloadString(URL);
        }

        public string ReactionOnEvent(CommandAndEventType.EventType ChatEvent)
        {
            switch (ChatEvent)
            {
                case CommandAndEventType.EventType.SIGNIN:
                    return $"Привет. Напиши адрес сайта, и я покажу, что находится в его теге <title>?";
                case CommandAndEventType.EventType.LOGOUT:
                    return $"Пока :)";
                default:
                    return "";
            }
        }

        public string ReciveNewMessageFromClient(string message)
        {
            if (CommandsForBot.GetTypeOfTrigger(message) is DownloadBot)
            {
                string URL = URLPattern.Match(message).Value;
                string HTMLCode = ConnectToSite(URL);
                return TitleTagPattern.Match(HTMLCode).Value;
            }
            else
            {
                return null;
            }
        }
    }
}
