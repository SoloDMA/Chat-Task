using Chat.Commands;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Chat.Bots.Contract;

namespace Chat.Bots.Implementation
{
    class TimeBot : IBot
    {
        private readonly Regex DetermineTimePattern = new Regex(@"(через|после) (\d+) минут");

        private readonly Dictionary<string, Command> Commands = new Dictionary<string, Command>
        {
            { "current", Command.CURRENT }
        };

        public string ExecuteCommand(string command, string argument = null)
        {

            if (!Commands.TryGetValue(command, out var commandType))
                return $"У меня нет команды {command}";

            return commandType switch
            {
                Command.CURRENT => $"Текущее время {DateTime.Now.TimeOfDay.ToString("hh\\:mm\\:ss")}",
                _ => throw new ArgumentOutOfRangeException()
            };

        }

        public string ReactionOnMessage(string message)
        {
            var patternMatch = DetermineTimePattern.Match(message);
            if (!patternMatch.Success)
                return null;

            if (patternMatch.Groups.Count < 2)
                return null;

            var min = int.Parse(patternMatch.Groups[2].Value);

            return $"Через {min} минут время будет {DateTime.Now.AddMinutes(min).TimeOfDay.ToString("hh\\:mm\\:ss")}";
        }

        public string ReactionOnEvent(CommandAndEventType.EventType chatEvent)
        {
            switch (chatEvent)
            {
                case CommandAndEventType.EventType.SIGNIN:
                    return $"Привет. Ты присоединился к нам в {DateTime.Now.TimeOfDay.ToString("hh\\:mm\\:ss")}";
                case CommandAndEventType.EventType.LOGOUT:
                    return $"Ты покинул чат в {DateTime.Now.TimeOfDay.ToString("hh\\:mm\\:ss")}. Ждём снова!";
                default:
                    return "";
            }
        }


        private enum Command
        {
            CURRENT = 1
        }
    }
}
