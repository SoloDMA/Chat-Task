using Chat.Commands;
using System;
using System.Collections.Generic;
using Chat.Bots.Contract;

namespace Chat.Bots.Implementation
{
    class JokeBot : IBot
    {
        private readonly Dictionary<string, Command> Commands = new Dictionary<string, Command>
        {
            { "joke", Command.JOKE }
        };

        public JokeBot()
        {
        }

        public string ExecuteCommand(string command, string argument = null)
        {
            if (!Commands.TryGetValue(command, out var commandType))
                return $"У меня нет команды {command}";

            return commandType switch
            {
                Command.JOKE => "Какой-то анектод :-) ",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public string ReactionOnMessage(string message)
        {
            return $"Какой-то анекдот :-)";
        }

        public string ReactionOnEvent(CommandAndEventType.EventType chatEvent)
        {
            return chatEvent switch
            {
                CommandAndEventType.EventType.SIGNIN => "Привет. Хочешь анекдот?",
                CommandAndEventType.EventType.LOGOUT => "Эх, а я только придумал новый анекдот",
                _ => ""
            };
        }

        private enum Command
        {
            JOKE = 1
        }
    }
}
