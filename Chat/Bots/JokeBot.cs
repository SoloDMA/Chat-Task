using Chat.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chat.Bots
{
    class JokeBot : IBot
    {
        public string BotName { get; set; }
        public JokeBot(string Name, params string[] CommandsAndtriggersForThatBot)
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
            if(CommandsForBot.GetTypeOfCommand(command) is JokeBot)
            {
                return "Какой-то анектод :-) ";
            }
            else
            {
                return $"У {BotName} нет команды {command}";
            }
            
        }

        public string ReciveNewMessageFromClient(string message)
        {
            if (CommandsForBot.GetTypeOfTrigger(message) is JokeBot)
            {
                return $"Какой-то анекдот :-)";
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
                    return "Привет. Хочешь анекдот?";
                case CommandAndEventType.EventType.LOGOUT:
                    return "Эх, а я только придумал новый анекдот";
                default:
                    return "";
            }
        }
    }
}
