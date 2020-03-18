using Chat.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chat.Bots
{
    interface IBot
    {
        string BotName { get; set; }
        public string ReactionOnEvent(CommandAndEventType.EventType ChatEvent); //реакция бота на события чата
        string ReciveNewMessageFromClient(string message); //Бот, получив сообщение от пользователя проверяет, есть ли в сообщении тригерры
        string Execute(string command); //Выполнение команды (строка, начинающаяся с слэша)
    }
}
