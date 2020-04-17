using Chat.Bots.Contract;
using Chat.Commands;
using Chat.Data.Contract;
using Chat.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Chat.Handlers
{

    public struct BotConfig
    {
        public string Discription { get; set; }
        public Regex[] Triggers { get; set; }
        public IBot Bot { get; set; }
    }

    class ChatHandler
    {

        private readonly IChatStorage ChatStorage;
        private readonly IDictionary<string, BotConfig> Bots;

        public ChatHandler(IChatStorage storage, IDictionary<string, BotConfig> bots)
        {
            ChatStorage = storage ?? throw new ArgumentNullException(nameof(storage));
            Bots = bots ?? throw new ArgumentNullException(nameof(bots));

        }

        public ClientModel SignInChat(string clientName, out string error)
        {

            if (Encoding.UTF8.GetBytes(clientName).Length > byte.MaxValue)
            {
                error = "Слишком длинное имя пользователя. Повторите регистрацию снова.";
                return null;
            }

            var client = new ClientModel(clientName, DateTime.Now);

            if (!ChatStorage.AddAction($"Зарегистрирован пользователь - {clientName}", out error))
            {
                return null;
            }
                

            if (!ChatStorage.AddMessage(
                new MessageModel
                { UserName = "@CHAT", TextOfMessage = $"Привет, {clientName}!", TimeOfMessage = DateTime.Now },
                out error))
                return null;

            foreach (var (botName, botConfig) in Bots)
            {
                var botMessage = botConfig.Bot.ReactionOnEvent(CommandAndEventType.EventType.SIGNIN);

                if (string.IsNullOrWhiteSpace(botMessage))
                    continue;

                if (!ChatStorage.AddMessage(
                    new MessageModel { UserName = botName, TextOfMessage = botMessage, TimeOfMessage = DateTime.Now },
                    out error))
                    return null;
            }

            error = null;
            return client;
        }

        public void LogOutChat(ClientModel client, out string error)
        {

            if (!ChatStorage.AddAction($"delete {client.UserName}", out error))
                return;

            if (!ChatStorage.AddMessage(
                new MessageModel
                { UserName = "@CHAT", TextOfMessage = $"Пользователь {client.UserName} вышел из чата!", TimeOfMessage = DateTime.Now },
                out error))
                return;

            foreach (var (botName, botConfig) in Bots)
            {
                var botMessage = botConfig.Bot.ReactionOnEvent(CommandAndEventType.EventType.LOGOUT);
                if (string.IsNullOrWhiteSpace(botMessage))
                    continue;

                if (!ChatStorage.AddMessage(
                    new MessageModel 
                    {  UserName = botName, TextOfMessage = botMessage, TimeOfMessage = DateTime.Now },
                    out error))
                    return;
            }

            error = null;
        }

        public void AddMessageChat(ClientModel user, string userMessage, out string error)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                error = "Не удалось распознать сообщение. Повторите отправку снова";
                return;
            }


            if (!ChatStorage.AddMessage(
                new MessageModel 
                { UserName = user.UserName, TextOfMessage = userMessage, TimeOfMessage = DateTime.Now }, 
                out error))
                return;

            foreach (var (botName, botConfig) in Bots)
            {

                foreach (var botTrigger in botConfig.Triggers)
                {
                    if (!botTrigger.IsMatch(userMessage))
                        continue;

                    var botTriggerMessage = botConfig.Bot.ReactionOnMessage(userMessage);

                    if (string.IsNullOrWhiteSpace(botTriggerMessage))
                        continue;

                    if (!ChatStorage.AddMessage(
                        new MessageModel
                        { UserName = botName, TextOfMessage = botTriggerMessage, TimeOfMessage = DateTime.Now }, 
                        out error))
                        return;
                }

            }

            error = null;
        }

        public void DeleteMessageChat(int messageId, out string error)
        {

            if (!ChatStorage.TryDeleteMessage(messageId, out error))
                return;

            error = null;
        }

        public void CallBot(ClientModel user, string botName, string botCommand, string botCommandArgument, out string error)
        {

            if (botCommand == null)
            {
                error = "Не корректный формат обращения к боту.";
                return;
            }

            if (!ChatStorage.AddAction($"Сall {botName} from {user.UserName}", out error))
                return;

            if (!Bots.TryGetValue(botName, out var botConfig))
            {
                error = $"Не нашли бота по имени {botName}";
                return;
            }
            
            var botResultMessage = botConfig.Bot.ExecuteCommand(botCommand, botCommandArgument);

            if (!ChatStorage.AddMessage(
                new MessageModel {  UserName = botName, TextOfMessage = botResultMessage, TimeOfMessage = DateTime.Now },
                out error))
                return;

            error = null;
        }

        public List<MessageModel> GetMessages()
        {
            return ChatStorage.GetAllMessages();
        }
    }
}
