using Chat.Data.Models;
using System.Collections.Generic;

namespace Chat.View.Contract
{
    interface IChatView
    {
        void Write(string text);
        void WriteMessages(List<MessageModel> messages);
        string Read();
        void Clear();
    }
}
