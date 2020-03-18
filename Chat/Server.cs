using Chat.Bots;
using Chat.View;
using System;
using System.Collections.Generic;
using System.Text;
using Chat.Commands;
using System.Text.RegularExpressions;
using Chat.Models;

namespace Chat
{
    class Server
    {
        private bool isWorking { get; set; }
        private List<IBot> Bots { get; set; }
        private List<Client> Clients { get; set; }
        private CommandsForChat ChatCmd { get; set; }
        private Data ChatData { get; set; }
        private readonly Regex UserOrBotNamePattern = new Regex(@"@\w+");
        private readonly Regex BotCommandPattern = new Regex(@"/\w+");
        private readonly Regex MessageIdPettern = new Regex(@"\d+");


        public void StartServer()
        {
            ChatCmd = new CommandsForChat();
            Clients = new List<Client>();
            ChatData = new Data();
            Bots = new List<IBot>
            {
                new JokeBot("@joke_bot", "/joke" ,"скучно", "грустно", "хочу анекдот"),
                new TimeBot("@time_bot", "/current", "через"),
                new DownloadBot("@downloader_bot", "/site" , "http://", "https://")
            };
            WorkingServer();
        }

        private void WorkingServer()
        {
            ConsoleView.Write("Введите начальную команду для старта сервера");
            do
            {
                string message = ConsoleView.Read();
                ConsoleView.Clear();
                MessageProcessing(message);
            } while (isWorking);
        }


        private void MessageProcessing(string Message)
        {

            switch (ChatCmd.GetCommandType(Message))
            {
                case CommandAndEventType.CommandTypes.STARTCHAT:
                    ChatData.ReadFromFileAllMessages();
                    isWorking = true;
                    WriteChatInfo(); 
                    break;
                case CommandAndEventType.CommandTypes.SIGNIN:
                    SignInChat(Message); 
                    break;
                case CommandAndEventType.CommandTypes.LOGOUT:
                    LogOutChat(Message);
                    break;
                case CommandAndEventType.CommandTypes.ADDMESSAGE:
                    AddMessageChat(Message);
                    break;
                case CommandAndEventType.CommandTypes.DELETEMESSAGE:
                    DeleteMessageChat(Message);
                    break;
                case CommandAndEventType.CommandTypes.BOT:
                    BotChat(Message);
                    break;
                case CommandAndEventType.CommandTypes.STOPCHAT:
                    isWorking = false;
                    break;
                case CommandAndEventType.CommandTypes.HELP:
                    WriteChatInfo();
                    break;
                case CommandAndEventType.CommandTypes.HISTORY:
                    WriteChatHistory();
                    break;
                case CommandAndEventType.CommandTypes.ERROR:
                    ConsoleView.Write("Введена неверная команда");
                    break;
            }
        }

        private void WriteChatHistory()
        {
            ConsoleView.WriteMessages(ChatData.Messages);
        }

        private Client GetUser(string UserName)
        {
            return Clients.Find(Client => Client.UserName == UserName);
        }

        private void WriteChatInfo()
        {
            string ChatInfog =
                "Добро пожаловать в чат \n" +
                "Список команд: \n" +
                "1. start-chat \n" +
                "2. signin @username \n" +
                "3. logout @username \n" +
                "4. add-mes @username | message \n" +
                "5. del-mes messageId \n" +
                "6. bot @botname @username bot-command \n" +
                "7. stop-chat \n" +
                "8. mes-hist \n" +
                "9. help \n \n" +
                "Доступные боты: @time_bot, @joke_bot, @downloader_bot \n";
            ConsoleView.Write(ChatInfog);
        }


        private void BotChat(string Message)
        {
            MatchCollection UserAndBotNames = UserOrBotNamePattern.Matches(Message);
            string BotName = UserAndBotNames[0].Value;
            string InvokerUser = UserAndBotNames[1].Value;
            string BotCommand = BotCommandPattern.Match(Message).Value;
            if(GetUser(InvokerUser) != null)
            {
                ChatData.WriteChatAction($"call {BotName} from {InvokerUser}");
                string BotMessage = Bots.Find(bot => bot.BotName == BotName).Execute(BotCommand);
                ChatData.AddNewMessage(new MessageModel { UserName = BotName, TextOfMessage = BotMessage, TimeOfMessage = DateTime.Now });
                ConsoleView.WriteMessages(ChatData.Messages);
            }
            else
            {
                ConsoleView.Write("Вы не можете вызвать ботов в чате, так как не зарегистрированы. Пожалуйста, воспользутесь командой signin @username для регистрации");
            }
            
        }

        private void DeleteMessageChat(string Message)
        {
            int MessageId = int.Parse(MessageIdPettern.Match(Message).Value);
            ChatData.DeleteMessage(MessageId);
            ConsoleView.WriteMessages(ChatData.Messages);
        }


        private void SignInChat(string Message)
        {

            string UserName = UserOrBotNamePattern.Match(Message).Value;
            if (Encoding.UTF8.GetBytes(UserName).Length > byte.MaxValue)
            {
                ConsoleView.Write("Слишком длинное имя пользователя. Повторите регистрацию снова.");
                return;
            }
            Client NewClient = new Client(UserName);
            Clients.Add(NewClient);
            ChatData.WriteChatAction($"add {UserName}");
            ChatData.AddNewMessage(new MessageModel { UserName = "@CHAT", TextOfMessage = $"Привет, {UserName}!", TimeOfMessage = DateTime.Now });
            foreach (var bot in Bots)
            {
                string BotMessage = bot.ReactionOnEvent(CommandAndEventType.EventType.SIGNIN);
                ChatData.AddNewMessage(new MessageModel { UserName = bot.BotName, TextOfMessage = BotMessage, TimeOfMessage = DateTime.Now });
            }
            ConsoleView.WriteMessages(ChatData.Messages);
        }

        private void LogOutChat(string Message)
        {
            string UserName = UserOrBotNamePattern.Match(Message).Value;
            Client DeletingClient = GetUser(UserName);
            if (DeletingClient != null)
            {
                Clients.Remove(DeletingClient);
                ChatData.WriteChatAction($"delete {UserName}");
                ChatData.AddNewMessage(new MessageModel { UserName = "@CHAT", TextOfMessage = $"Пока, {UserName}!", TimeOfMessage = DateTime.Now });
                foreach (var bot in Bots)
                {
                    string BotMessage = bot.ReactionOnEvent(CommandAndEventType.EventType.LOGOUT);
                    ChatData.AddNewMessage(new MessageModel { UserName = bot.BotName, TextOfMessage = BotMessage, TimeOfMessage = DateTime.Now });
                }
                ConsoleView.WriteMessages(ChatData.Messages);
            }
            else
            {
                ConsoleView.Write("Вы не можете выйти из чата, не зайдя в него. Пожалуйста, воспользутесь командой signin @username для регистрации");
            }
            
        }

        private void AddMessageChat(string Message)
        {
            string UserName = UserOrBotNamePattern.Match(Message).Value;
            string UserMessage = Message.Split("| ")[1]; 
            if (GetUser(UserName) != null)
            {
                ChatData.AddNewMessage(new MessageModel { UserName = UserName, TextOfMessage = UserMessage, TimeOfMessage = DateTime.Now});
                foreach (var bot in Bots)
                {
                    string BotMessage = bot.ReciveNewMessageFromClient(UserMessage);
                    if (BotMessage != null) ChatData.AddNewMessage(new MessageModel { UserName = bot.BotName, TextOfMessage = BotMessage, TimeOfMessage = DateTime.Now });

                }
                ConsoleView.WriteMessages(ChatData.Messages);
            }
            else
            {
                ConsoleView.Write("Вы не зарегистрированы в чате. Пожалуйста, воспользутесь командой signin @username для регистрации");
            }

        }

    }
}
