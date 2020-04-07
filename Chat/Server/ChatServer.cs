using System;
using System.Collections.Generic;
using System.Text;
using Chat.Commands;
using System.Text.RegularExpressions;
using Chat.Bots.Contract;
using Chat.Data.Contract;
using Chat.Data.Models;
using Chat.View.Contract;


namespace Chat.Server
{
    public struct BotConfig
    {
        public string Discription { get; set; }
        public Regex[] Triggers { get; set; }
        public IBot Bot { get; set; }
    }

    internal class ChatServer
    {
        private const char BotNamePrefix = '@';
        private const char BotCommandPrefix = '/';

        private const char UserNamePrefix = '@';

        private const string ChatInfoBase = "Добро пожаловать в чат \n" +
                                            "Список команд: \n" +
                                            "1. start-chat \n" +
                                            "2. signin @username \n" +
                                            "3. logout @username \n" +
                                            "4. add-mes @username | message \n" +
                                            "5. del-mes messageId \n" +
                                            "6. bot @username @botname bot-command [command-argument (optional)]\n" +
                                            "7. stop-chat \n" +
                                            "8. mes-hist \n" +
                                            "9. help \n \n" +
                                            "Доступные боты: \n";

        private bool IsWorking;

        private readonly Dictionary<string, Client> Clients;
        private CommandsForChat ChatCmd { get; set; }

        protected readonly IChatView View;
        private readonly IChatStorage ChatStorage;

        private readonly IDictionary<string, BotConfig> Bots;

        private const string BotCommandPatternBotNameGroup = "botName";
        private const string BotCommandPatternBotCommandGroup = "botCommandName";
        private const string BotCommandPatternBotCommandArgumentGroup = "botCommandArgument";

        protected readonly Regex BotCommandPattern =
            new Regex($@"{BotNamePrefix}(?<{BotCommandPatternBotNameGroup}>\w+) \{BotCommandPrefix}(?<{BotCommandPatternBotCommandGroup}>\w+) ?(?<{BotCommandPatternBotCommandArgumentGroup}>.*)");

        private const string UserMessagePatternUserNameGroup = "userName";
        private const string UserMessagePatternUserMessageGroup = "userMessage";

        private readonly Regex UserNameMessagePattern =
            new Regex($@"(?<chatCommand>){UserNamePrefix}(?<{UserMessagePatternUserNameGroup}>\w+) ?(?<{UserMessagePatternUserMessageGroup}>.*)");

        private readonly Regex UserNamePattern = new Regex($@"{UserNamePrefix}(?<{UserMessagePatternUserNameGroup}>\w+)");

        private readonly Regex MessageIdPattern = new Regex(@"\d+");

        private readonly string ChatInfo;

        #region Constructors

        public ChatServer(IChatView view, IChatStorage store, IDictionary<string, BotConfig> bots)
        {
            View = view ?? throw new ArgumentNullException(nameof(view));
            ChatStorage = store ?? throw new ArgumentNullException(nameof(bots));
            Bots = bots ?? throw new ArgumentNullException(nameof(bots));


            Clients = new Dictionary<string, Client>();

            var chatInfoBuilder = new StringBuilder(ChatInfoBase);
            foreach (var (botName, botConfig) in Bots)
            {
                if (botConfig.Bot == null)
                    throw new ArgumentNullException(nameof(bots));

                chatInfoBuilder.AppendLine(BotNamePrefix + botName + $" - {botConfig.Discription}");
            }

            ChatInfo = chatInfoBuilder.ToString();

            ChatCmd = new CommandsForChat();
        }

        #endregion

        public virtual void StartServer()
        {
            
            WorkingServer();
        }

        protected virtual void WorkingServer()
        {
            View.Write("Введите начальную команду для старта сервера");
            do
            {
                string message = View.Read();
                View.Clear();
                MessageProcessing(message);
            } while (IsWorking);
        }


        protected void MessageProcessing(string message)
        {
            switch (ChatCmd.GetCommandType(message))
            {
                case CommandAndEventType.CommandTypes.START_CHAT:
                    IsWorking = true;
                    View.Write(GetChatInfo());
                    break;
                case CommandAndEventType.CommandTypes.SIGNIN:
                    View.Write(GetChatInfo());
                    View.Write(SignInChat(GetClientName(message)));
                    View.WriteMessages(ChatStorage.GetAllMessages());
                    break;
                case CommandAndEventType.CommandTypes.LOGOUT:
                    View.Write(LogOutChat(GetUserFromMessage(message)));
                    View.Write(GetChatInfo());
                    break;
                case CommandAndEventType.CommandTypes.ADD_MESSAGE:
                    View.Write(GetChatInfo());
                    if (!AddMessageChat(GetUserFromMessage(message, out var userMessage), userMessage, out var error))
                        View.Write(error);
                    View.WriteMessages(ChatStorage.GetAllMessages());
                    break;
                case CommandAndEventType.CommandTypes.DELETE_MESSAGE:
                    View.Write(GetChatInfo());
                    View.Write(DeleteMessageChat(message));
                    View.WriteMessages(ChatStorage.GetAllMessages());
                    break;
                case CommandAndEventType.CommandTypes.BOT:
                    View.Write(GetChatInfo());
                    View.Write(BotMessageProcessing(GetUserFromMessage(message, out userMessage), userMessage));
                    View.WriteMessages(ChatStorage.GetAllMessages());
                    break;
                case CommandAndEventType.CommandTypes.STOP_CHAT:
                    View.Write(GetChatInfo());
                    IsWorking = false;
                    break;
                case CommandAndEventType.CommandTypes.HELP:
                    View.Write(GetChatInfo());
                    break;
                case CommandAndEventType.CommandTypes.HISTORY:
                    View.Write(GetChatInfo());
                    View.WriteMessages(ChatStorage.GetAllMessages());
                    break;
                case CommandAndEventType.CommandTypes.ERROR:
                    View.Write("Введена неверная команда");
                    break;
            }
        }

        private string GetClientName(string message)
        {
            var clientNameMatch = UserNamePattern.Match(message);
            if (!clientNameMatch.Success)
                throw new Exception();

            return clientNameMatch.Groups[UserMessagePatternUserNameGroup].Value;
        }


        private Client GetUserFromMessage(string message, out string userMessage)
        {
            var userMassageMatch = UserNameMessagePattern.Match(message);

            if (!userMassageMatch.Success)
                throw new Exception($"Не верный формат \"{message}\".");

            var userName = userMassageMatch.Groups[UserMessagePatternUserNameGroup].Value;

            if (!Clients.TryGetValue(userName, out var client))
                throw new Exception($"Не нашли пользователя по имени \"{userName}\".");

            userMessage = userMassageMatch.Groups[UserMessagePatternUserMessageGroup].Value;
            return client;
        }

        private Client GetUserFromMessage(string message)
        {
            var userMassageMatch = UserNameMessagePattern.Match(message);

            if (!userMassageMatch.Success)
                throw new Exception($"Не верный формат \"{message}\".");

            var userName = userMassageMatch.Groups[UserMessagePatternUserNameGroup].Value;

            if (!Clients.TryGetValue(userName, out var client))
                throw new Exception($"Не нашли пользователя по имени \"{userName}\".");

            return client;
        }

        private string GetChatInfo()
        {
            return ChatInfo;
        }

        private string BotMessageProcessing(Client user, string message)
        {
            if (user == null)
                throw new Exception();

            var botCommandMatch = BotCommandPattern.Match(message);

            if (!botCommandMatch.Success)
                return "Не корректный формат обращения к боту.";


            var botName = botCommandMatch.Groups[BotCommandPatternBotNameGroup].Value;
            var botCommand = botCommandMatch.Groups[BotCommandPatternBotCommandGroup].Value;
            var botCommandArgument = botCommandMatch.Groups[BotCommandPatternBotCommandArgumentGroup].Value;


            if (!ChatStorage.AddAction($"Сall {botName} from {user.UserName}", out var error))
                return error;

            if (!Bots.TryGetValue(botName, out var botConfig))
                return $"Не нашли бота по имени {botName}";

            var botResultMessage = botConfig.Bot.ExecuteCommand(botCommand, botCommandArgument);

            if (!ChatStorage.AddMessage(
                new MessageModel {MessageID = (ChatStorage.LastMessageID++).ToString(), UserName = botName, TextOfMessage = botResultMessage, TimeOfMessage = DateTime.Now },
                out error))
                return error;

            return botResultMessage;
        }

        private string DeleteMessageChat(string message)
        {
            var messageId = int.Parse(MessageIdPattern.Match(message).Value);

            if (!ChatStorage.TryDeleteMessage(messageId, out var error))
                return error;

            return $"Сообщение ({messageId}) удалено.";
        }


        private string SignInChat(string clientName)
        {

            if (Encoding.UTF8.GetBytes(clientName).Length > byte.MaxValue)
            {
                return "Слишком длинное имя пользователя. Повторите регистрацию снова.";
            }

            Clients.Add(clientName, new Client(clientName));
            if (!ChatStorage.AddAction($"add {clientName}", out var error))
                return error;

            if (!ChatStorage.AddMessage(
                new MessageModel
                { MessageID = (ChatStorage.LastMessageID++).ToString(), UserName = "@CHAT", TextOfMessage = $"Привет, {clientName}!", TimeOfMessage = DateTime.Now },
                out error))
                return error;

            foreach (var (botName, botConfig) in Bots)
            {
                var botMessage = botConfig.Bot.ReactionOnEvent(CommandAndEventType.EventType.SIGNIN);

                if (string.IsNullOrWhiteSpace(botMessage))
                    continue;

                if (!ChatStorage.AddMessage(
                    new MessageModel { MessageID = (ChatStorage.LastMessageID++).ToString(), UserName = botName, TextOfMessage = botMessage, TimeOfMessage = DateTime.Now },
                    out error))
                    return error;
            }

            return $"Удачный вход пользователя {clientName}.";
        }

        private string LogOutChat(Client client)
        {
            Clients.Remove(client.UserName);

            if (!ChatStorage.AddAction($"delete {client.UserName}", out var error))
                return error;

            if (!ChatStorage.AddMessage(
                new MessageModel
                { MessageID = (ChatStorage.LastMessageID++).ToString(), UserName = "@CHAT", TextOfMessage = $"Пока, {client.UserName}!", TimeOfMessage = DateTime.Now },
                out error))
                return error;

            foreach (var (botName, botConfig) in Bots)
            {
                var botMessage = botConfig.Bot.ReactionOnEvent(CommandAndEventType.EventType.LOGOUT);
                if (string.IsNullOrWhiteSpace(botMessage))
                    continue;

                if (!ChatStorage.AddMessage(
                    new MessageModel { MessageID = (ChatStorage.LastMessageID++).ToString(), UserName = botName, TextOfMessage = botMessage, TimeOfMessage = DateTime.Now },
                    out error))
                    return error;
            }

            return $"Пользователь {client.UserName} вышел.";
        }

        private bool AddMessageChat(Client user, string userMessage, out string error)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                throw new ArgumentNullException(nameof(userMessage));


            if (!ChatStorage.AddMessage(new MessageModel { MessageID = (ChatStorage.LastMessageID++).ToString(), UserName = user.UserName, TextOfMessage = userMessage, TimeOfMessage = DateTime.Now }, out error))
                return false;

            foreach (var (botName, botConfig) in Bots)
            {

                foreach (var botTrigger in botConfig.Triggers)
                {
                    if (!botTrigger.IsMatch(userMessage))
                        continue;

                    var botTriggerMessage = botConfig.Bot.ReactionOnMessage(userMessage);

                    if (string.IsNullOrWhiteSpace(botTriggerMessage))
                        continue;

                    if (!ChatStorage.AddMessage(new MessageModel
                    { MessageID = (ChatStorage.LastMessageID++).ToString(), UserName = botName, TextOfMessage = botTriggerMessage, TimeOfMessage = DateTime.Now }, out error))
                        return false;
                }

            }

            error = null;
            return true;
        }
    }
}