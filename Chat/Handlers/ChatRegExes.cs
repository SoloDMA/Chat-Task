using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Chat.Handlers
{
    class ChatRegExes
    {
        #region Prefixes
        private const char BotNamePrefix = '@';
        private const char BotCommandPrefix = '/';

        private const char UserNamePrefix = '@';
        #endregion

        #region Group Names
        public static readonly string BotCommandPatternBotNameGroup = "botName";
        public static readonly string BotCommandPatternBotCommandGroup = "botCommandName";
        public static readonly string BotCommandPatternBotCommandArgumentGroup = "botCommandArgument";

        public static readonly string UserMessagePatternUserNameGroup = "userName";
        public static readonly string UserMessagePatternUserMessageGroup = "userMessage";
        #endregion

        #region RegExes
        public static readonly Regex BotCommandPattern =
            new Regex($@"{BotNamePrefix}(?<{BotCommandPatternBotNameGroup}>\w+) \{BotCommandPrefix}(?<{BotCommandPatternBotCommandGroup}>\w+) ?(?<{BotCommandPatternBotCommandArgumentGroup}>.*)");

        public static readonly Regex UserNameMessagePattern =
            new Regex($@"(?<chatCommand>){UserNamePrefix}(?<{UserMessagePatternUserNameGroup}>\w+) ?(?<{UserMessagePatternUserMessageGroup}>.*)");

        public static readonly Regex UserNamePattern = new Regex($@"{UserNamePrefix}(?<{UserMessagePatternUserNameGroup}>\w+)");

        public static readonly Regex MessageIdPattern = new Regex(@"\d+");
        #endregion
    }
}
