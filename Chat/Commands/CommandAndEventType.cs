using System;
using System.Collections.Generic;
using System.Text;

namespace Chat.Commands
{
    public class CommandAndEventType
    {
        public enum CommandTypes
        {
            START_CHAT,
            SIGNIN,
            LOGOUT,
            ADD_MESSAGE,
            DELETE_MESSAGE,
            BOT,
            STOP_CHAT,
            ERROR,
            HELP,
            HISTORY
        }

        public enum EventType
        {
            SIGNIN,
            LOGOUT
        }
    }
}
