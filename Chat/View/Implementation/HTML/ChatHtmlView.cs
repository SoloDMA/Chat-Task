using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Chat.Data.Models;
using Chat.View.Contract;

namespace Chat.View.Implementation.HTML
{
    
    class ChatHtmlView : IChatView
    {
        private readonly string MainPagePath = "../../../View/Implementation/HTML/MainPage.html";
        private readonly string LoginPagePath = "../../../View/Implementation/HTML/LoginPage.html";

        private readonly string MessageFormat = "\t\t\t<div class=\"message\">\n" +
                                                "\t\t\t\t<div class=\"user-name\">{0}</div>\n" +
                                                "\t\t\t\t<div class=\"message-text\">{1}</div>\n" +
                                                "\t\t\t\t<div class=\"message-time\">{2}</div>\n" +
                                                "\t\t\t\t<div class=\"delete-message-btn\">\n" +
                                                "\t\t\t\t\t<a href=\"http://localhost:8080/deleteMessage/?messageID={3}\">Удалить</a>\n" +
                                                "\t\t\t\t</div>\n" +
                                                "\t\t\t</div>\n";

        private readonly string MessageLocator = "<!--Messages-->";

        private string mainPageStr;
        private string loginPageStr;

        private string MainPageStr { 
            get 
            { 
                if(mainPageStr != null)
                    return mainPageStr;

                return mainPageStr = GetPage(MainPagePath);
            } 
        }

        private string LoginPageStr
        {
            get
            {
                if (loginPageStr != null)
                {
                    return loginPageStr;
                }
                return loginPageStr = GetPage(LoginPagePath);
            }
        }

        public void WriteMessages(List<MessageModel> messages)
        {
            mainPageStr = GetPage(MainPagePath);

            foreach (var message in messages)
            {
                var LocatorPosition = mainPageStr.IndexOf(MessageLocator);
                var FormattedMessage = string.Format(MessageFormat, message.UserName, message.TextOfMessage, message.TimeOfMessage, message.MessageID);

                mainPageStr = mainPageStr.Insert(LocatorPosition, FormattedMessage);
            }
        }

        //private void UpdateHtmlPage(string path, string updatedPageStr)
        //{
        //    File.Delete(path);

        //    if (File.Exists(path))
        //        throw new Exception("Не удалось удалить файл");

        //    using var NewFile = File.Open(path, FileMode.Create, FileAccess.Write);
        //    var Buffer = Encoding.UTF8.GetBytes(updatedPageStr);
        //    NewFile.Write(Buffer, 0, Buffer.Length);
        //}

        private string GetPage(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Файл не найден!");

            using var FileStream = File.OpenRead(path);
            var Buffer = new byte[FileStream.Length];
            FileStream.Read(Buffer, 0, Buffer.Length);

            return Encoding.UTF8.GetString(Buffer);
        }

        public string ReadHtml(Pages pageType)
        {
            return pageType switch
            {
                Pages.LOGIN => LoginPageStr,
                Pages.MAIN => MainPageStr,
                _ => string.Empty
            };
        }

        #region NotImplementedMethods

        public void Write(string text)
        {
            
        }

        public void Clear()
        {
            
        }

        public string Read()
        {
            return string.Empty;
        }
        #endregion

    }

}
