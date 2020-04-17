using Chat.Commands;
using Chat.Data.Contract;
using Chat.Data.Models;
using Chat.Handlers;
using Chat.View.Contract;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chat.View.Implementation
{
    class ChatConsoleView : IChatView
    {
        private readonly ChatHandler ChatHandlers;
        private readonly CommandsForChat ChatCmd;
        private readonly Dictionary<string, ClientModel> OnlineClients;

        private const string MessageFormat = "{0} : {1} [{2}]";

        private bool IsWorking;

        private const string ChatInfoBase = "Добро пожаловать в чат \n" +
                                           "Список команд: \n" +
                                           "1. start-chat \n" +
                                           "2. signin @username \n" +
                                           "3. logout @username \n" +
                                           "4. add-mes @username | message \n" +
                                           "5. del-mes messageId \n" +
                                           "6. bot @username @botname bot-command [command-argument (optional)]\n" +
                                           "7. stop-chat \n" +
                                           "8. help \n \n" +
                                           "Доступные боты: \n";

        private readonly string ChatInfo;

        public ChatConsoleView(IChatStorage storage, IDictionary<string, BotConfig> bots)
        {
            ChatHandlers = new ChatHandler(storage, bots);
            ChatCmd = new CommandsForChat();
            OnlineClients = new Dictionary<string, ClientModel>();

            var chatInfoBuilder = new StringBuilder(ChatInfoBase);
            foreach (var (botName, botConfig) in bots)
            {
                if (botConfig.Bot == null)
                    throw new ArgumentNullException(nameof(bots));

                chatInfoBuilder.AppendLine(botName + $" - {botConfig.Discription}");
            }

            ChatInfo = chatInfoBuilder.ToString();

            IsWorking = false;
        }

        public void Start()
        {
            Console.WriteLine("Введите начальную команду для старта сервера");
            ChatHandlers.GetMessages();
            do
            {
                string message = Console.ReadLine();
                Console.Clear();
                MessageProcessing(message);
            } while (IsWorking);
        }

        private void MessageProcessing(string message)
        {
            string error = null;
            switch (ChatCmd.GetCommandType(message))
            {
                case CommandAndEventType.CommandTypes.START_CHAT:
                    IsWorking = true;
                    Render(ChatInfo);
                    break;
                case CommandAndEventType.CommandTypes.SIGNIN:
                    var clientName = GetClientName(message);
                    var newClientObject = ChatHandlers.SignInChat(clientName, out error);
                    if(newClientObject != null)
                        OnlineClients.Add(clientName, newClientObject);
                    break;
                case CommandAndEventType.CommandTypes.LOGOUT:
                    var clientObject = GetUserFromMessage(message);
                    ChatHandlers.LogOutChat(clientObject, out error);
                    break;
                case CommandAndEventType.CommandTypes.ADD_MESSAGE:
                    clientObject = GetUserFromMessage(message, out var userMessage);
                    ChatHandlers.AddMessageChat(clientObject, userMessage, out error);
                    break;
                case CommandAndEventType.CommandTypes.DELETE_MESSAGE:
                    var messageId = GetMessageId(message);
                    ChatHandlers.DeleteMessageChat(messageId, out error);
                    break;
                case CommandAndEventType.CommandTypes.BOT:
                    clientObject = GetUserFromMessage(message);
                    GetBotInfo(message, out var botName, out var botCommand, out var commandArgument);
                    ChatHandlers.CallBot(clientObject, botName, botCommand, commandArgument, out error);
                    break;
                case CommandAndEventType.CommandTypes.STOP_CHAT:
                    IsWorking = false;
                    break;
                case CommandAndEventType.CommandTypes.HELP:
                    Render(ChatInfoBase);
                    break;
                case CommandAndEventType.CommandTypes.ERROR:
                    error = "Введена неверная команда";
                    break;
            }

            if(error != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Render(error);
                Console.ResetColor();
            }
            else
            {
                var messages = ChatHandlers.GetMessages();
                foreach (var messageText in messages)
                {
                    Render(string.Format(MessageFormat, messageText.UserName, messageText.TextOfMessage, messageText.TimeOfMessage));
                }    
            }
        }

        public void Render(string text)
        {
            Console.WriteLine(text);
        }

        #region Parsers
        private string GetClientName(string message)
        {
            var clientNameMatch = ChatRegExes.UserNamePattern.Match(message);
            if (!clientNameMatch.Success)
                throw new Exception();

            return clientNameMatch.Groups[ChatRegExes.UserMessagePatternUserNameGroup].Value;
        }

        private ClientModel GetUserFromMessage(string message, out string userMessage)
        {
            var userMassageMatch = ChatRegExes.UserNameMessagePattern.Match(message);

            if (!userMassageMatch.Success)
                throw new Exception($"Не верный формат \"{message}\".");


            var userName = userMassageMatch.Groups[ChatRegExes.UserMessagePatternUserNameGroup].Value;

            if (!OnlineClients.TryGetValue(userName, out var client))
                throw new Exception($"Не нашли пользователя по имени \"{userName}\".");

            userMessage = userMassageMatch.Groups[ChatRegExes.UserMessagePatternUserMessageGroup].Value;
            return client;
        }

        private ClientModel GetUserFromMessage(string message)
        {
            var userMassageMatch = ChatRegExes.UserNameMessagePattern.Match(message);

            if (!userMassageMatch.Success)
                throw new Exception($"Не верный формат \"{message}\".");

            var userName = userMassageMatch.Groups[ChatRegExes.UserMessagePatternUserNameGroup].Value;

            if (!OnlineClients.TryGetValue(userName, out var client))
                throw new Exception($"Не нашли пользователя по имени \"{userName}\".");

            return client;
        }

        private int GetMessageId(string message)
        {
            return int.Parse(ChatRegExes.MessageIdPattern.Match(message).Value);
        }

        private void GetBotInfo(string message, out string botName, out string botCommand, out string commandArgument)
        {
            var botCommandMatch = ChatRegExes.BotCommandPattern.Match(message);

            botName = botCommandMatch.Groups[ChatRegExes.BotCommandPatternBotNameGroup].Value;
            botCommand = botCommandMatch.Groups[ChatRegExes.BotCommandPatternBotCommandGroup].Value;
            commandArgument = botCommandMatch.Groups[ChatRegExes.BotCommandPatternBotCommandArgumentGroup].Value;

        }
        #endregion
    }
}
