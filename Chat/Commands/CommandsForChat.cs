using System.Text.RegularExpressions;

namespace Chat.Commands
{
    class CommandsForChat : CommandAndEventType
    {
        private readonly Regex GetMessagePattern = new Regex(@"^\S+");
        public CommandTypes GetCommandType(string message)
        {
            return GetMessagePattern.Match(message).Value switch
            {
                "start-chat" => CommandTypes.START_CHAT,
                "signin" => CommandTypes.SIGNIN,
                "logout" => CommandTypes.LOGOUT,
                "add-mes" => CommandTypes.ADD_MESSAGE,
                "del-mes" => CommandTypes.DELETE_MESSAGE,
                "bot" => CommandTypes.BOT,
                "stop-chat" => CommandTypes.STOP_CHAT,
                "help" => CommandTypes.HELP,
                _ => CommandTypes.ERROR
            };
        }
    }
}
