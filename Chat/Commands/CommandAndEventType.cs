using System;
using System.Collections.Generic;
using System.Text;

namespace Chat.Commands
{
    class CommandAndEventType
    {
        public enum CommandTypes
        {
            STARTCHAT,
            SIGNIN,
            LOGOUT,
            ADDMESSAGE,
            DELETEMESSAGE,
            BOT,
            STOPCHAT,
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
