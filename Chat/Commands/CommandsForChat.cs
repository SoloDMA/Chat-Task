using System.Text.RegularExpressions;

namespace Chat.Commands
{
    class CommandsForChat : CommandAndEventType
    {
        private readonly Regex GetMessagePattern = new Regex(@"^\S+");
        public CommandTypes GetCommandType(string Message)
        {
            
            switch (GetMessagePattern.Match(Message).Value)
            {
                case "start-chat":
                    return CommandTypes.STARTCHAT;
                case "signin":
                    return CommandTypes.SIGNIN;
                case "logout":
                    return CommandTypes.LOGOUT;
                case "add-mes":
                    return CommandTypes.ADDMESSAGE;
                case "del-mes":
                    return CommandTypes.DELETEMESSAGE;
                case "bot":
                    return CommandTypes.BOT;
                case "stop-chat":
                    return CommandTypes.STOPCHAT;
                case "help":
                    return CommandTypes.HELP;
                case "mes-hist":
                    return CommandTypes.HISTORY;
                default:
                    return CommandTypes.ERROR;
            }
        }
    }
}
