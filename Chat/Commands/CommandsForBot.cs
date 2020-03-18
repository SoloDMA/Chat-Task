using Chat.Bots;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chat.Commands
{
    class CommandsForBot 
    {
        
        private static Dictionary<string, IBot> CommandsDict = new Dictionary<string, IBot>(); //комманда - бот
        private static Dictionary<string, IBot> TriggersDict = new Dictionary<string, IBot>(); //триггер - бот

        public static IBot GetTypeOfCommand(string Command) //возвращает к какому боту относится команда
        {
            return CommandsDict[Command];
        }

        public static IBot GetTypeOfTrigger(string Message) //ищет тригеры для бота в сообщении пользователя
        {
            foreach (var Trigger in TriggersDict)
            {
                if (Message.ToLower().IndexOf(Trigger.Key) > -1) //если найден триггер
                {
                    return Trigger.Value;
                }
            }
            return null;
        }

        public static void AddNewCommand(string Command, IBot Bot) //добавление новой команды конкретному боту
        {
            CommandsDict.Add(Command, Bot);
        }

        public static void AddNewTrigger(string Trigger, IBot Bot) //добавление нового триггера конкретному боту
        {
            TriggersDict.Add(Trigger, Bot);
        }
    }
}
