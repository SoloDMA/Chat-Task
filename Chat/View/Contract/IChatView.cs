using Chat.Data.Models;

using System.Collections.Generic;

namespace Chat.View.Contract
{
    enum Pages
    {
        MAIN,
        LOGIN
    }
    interface IChatView
    {
        void Write(string text);
        void WriteMessages(List<MessageModel> messages);
        string Read();
        string ReadHtml(Pages pageType);
        void Clear();
    }
}
