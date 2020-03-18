using Chat.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chat.View
{
    class ConsoleView
    {
        public static void Write(string anyString) => Console.WriteLine(anyString);

        public static void WriteMessages(List<MessageModel> messages)
        {
            
            foreach (var mes in messages)
            {
                Console.WriteLine("{0} : {1} [{2}]", mes.UserName, mes.TextOfMessage, mes.TimeOfMessage);
            }
        }

        public static string Read() =>  Console.ReadLine();


        public static void Clear() => Console.Clear();
    }
}
