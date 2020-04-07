using Chat.Data.Models;
using Chat.View.Contract;
using System;
using System.Collections.Generic;

namespace Chat.View.Implementation
{
    class ChatConsoleView : IChatView
    {
        public void Write(string anyString) => Console.WriteLine(anyString);

        public void WriteMessages(List<MessageModel> messages)
        {
            foreach (var mes in messages)
            {
                Console.WriteLine("{0} : {1} [{2}]", mes.UserName, mes.TextOfMessage, mes.TimeOfMessage);
            }
        }

        public string Read() =>  Console.ReadLine();


        public void Clear() => Console.Clear();

        public string ReadHtml(Pages pageType)
        {
            throw new NotImplementedException();
        }
    }
}
