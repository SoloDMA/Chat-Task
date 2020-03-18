using Chat.Commands;
using System;
using System.Text.RegularExpressions;

namespace Chat.Bots
{
    class TimeBot : IBot
    {
        public string BotName { get; set; }
        private readonly Regex DetermineTimePattern = new Regex(@"(через|после) (\d+) минут");

        public TimeBot(string Name, params string[] CommandsAndtriggersForThatBot)
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
            if (CommandsForBot.GetTypeOfCommand(command) is TimeBot)
            {
                return $"Текущее время {DateTime.Now.TimeOfDay.ToString("hh\\:mm\\:ss")}";
            }
            else
            {
                return $"У {BotName} нет команды {command}";
            }
        }

        public string ReciveNewMessageFromClient(string Message)
        {
            if (CommandsForBot.GetTypeOfTrigger(Message) is TimeBot)
            {
                int min = int.Parse(DetermineTimePattern.Match(Message).Groups[2].Value);
                return $"Через {min} минут время будет {DateTime.Now.AddMinutes(min).TimeOfDay.ToString("hh\\:mm\\:ss")}";
            }
            else
            {
                return null;
            }
        }

        public string ReactionOnEvent(CommandAndEventType.EventType ChatEvent)
        {
            switch (ChatEvent)
            {
                case CommandAndEventType.EventType.SIGNIN:
                    return $"Привет. Ты присоединился к нам в {DateTime.Now.TimeOfDay.ToString("hh\\:mm\\:ss")}";
                case CommandAndEventType.EventType.LOGOUT:
                    return $"Ты покинул чат в {DateTime.Now.TimeOfDay.ToString("hh\\:mm\\:ss")}. Ждём снова!";
                default:
                    return "";
            }
        }
    }
}
