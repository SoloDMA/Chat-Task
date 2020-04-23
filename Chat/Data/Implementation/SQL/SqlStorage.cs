using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using Chat.Data.Contract;
using Chat.Data.Models;

namespace Chat.Data.Implementation
{
    class SqlStorage : IChatStorage
    {
        private object lockObj = new object();

        private SqlConnection SqlConnection;

        private const string ConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=D:\.Net\Chat — новая архитектура\Chat\Data\Implementation\SQL\ChatDB.mdf;Integrated Security=True";

        private Dictionary<int, string> OrderNumber_MessageSqlID;

        private List<MessageModel> messageCache;
        private List<MessageModel> MessagesCache
        {
            get
            {
                if (messageCache != null)
                    return messageCache;

                return messageCache = ReadMessages();
            }
        }

        private static int currenOrderNumber;
        private int CurrentOrderNumber
        {
            get
            {
                if (currenOrderNumber >= 1)
                    return currenOrderNumber;

                return 1;
            }

            set
            {
                currenOrderNumber = value;
            }
        }

        public SqlStorage()
        {
            SqlConnection = new SqlConnection(ConnectionString);
            OrderNumber_MessageSqlID = new Dictionary<int, string>();
        }

        private List<MessageModel> ReadMessages()
        {
            SqlConnection.Open();
            var sqlCommand = new SqlCommand("SELECT * FROM [Messages] ORDER BY TimeOfMessage", SqlConnection);
            using var sqlReader = sqlCommand.ExecuteReader();

            var messages = new List<MessageModel>();

            var ChatId = 1;
            while (sqlReader.Read())
            {

                var message = new MessageModel
                {
                    MessageID = ChatId.ToString(),
                    UserName = (string)sqlReader["Username"],
                    TextOfMessage = (string)sqlReader["TextOfMessage"],
                    TimeOfMessage = DateTime.Parse((string)sqlReader["TimeOfMessage"])
                };
                messages.Add(message);
                OrderNumber_MessageSqlID.Add(ChatId++, (string)sqlReader["Id"]);
            }

            SqlConnection.Close();

            currenOrderNumber = messages.Count + 1;

            return messages;
        }

        public bool AddAction(string action, out string error)
        {
            lock (lockObj)
            {
                SqlConnection.Open();

                var sqlCommand = new SqlCommand("INSERT INTO [Actions] (ActionDescription, ActionTime) VALUES(@ActionDescription, @ActionTime)",
                    SqlConnection);

                sqlCommand.Parameters.AddWithValue("ActionDescription", action);
                sqlCommand.Parameters.AddWithValue("ActionTime", DateTime.Now);

                sqlCommand.ExecuteNonQuery();

                SqlConnection.Close();

                error = null;
                return true;
            }
        }

        public bool AddMessage(MessageModel messageModel, out string error)
        {


            if (messageModel.TextOfMessage == null || string.IsNullOrWhiteSpace(messageModel.TextOfMessage))
            {
                error = "Сообщение пусто. Введите сообщение заново.";
                return false;
            }

            lock (lockObj)
            {

                messageModel.MessageID = CurrentOrderNumber.ToString();
                var MessageIdInDB = Guid.NewGuid().ToString();
                OrderNumber_MessageSqlID.Add(CurrentOrderNumber++, MessageIdInDB);


                MessagesCache.Add(messageModel);

                SqlConnection.Open();
                var sqlCommand = new SqlCommand("INSERT INTO [Messages] (Id,Username, TextOfMessage, TimeOfMessage) " +
                                                "VALUES(@Id ,@Username, @TextOfMessage, @TimeOfMessage)",
                    SqlConnection);

                sqlCommand.Parameters.AddWithValue("Id", MessageIdInDB);
                sqlCommand.Parameters.AddWithValue("Username", messageModel.UserName);
                sqlCommand.Parameters.AddWithValue("TextOfMessage", messageModel.TextOfMessage);
                sqlCommand.Parameters.AddWithValue("TimeOfMessage", messageModel.TimeOfMessage.ToString("dd.MM.yyyy HH:mm:ss.ffff"));
                sqlCommand.ExecuteNonQuery();
                SqlConnection.Close();

                error = null;
                return true;
            }

        }

        public List<MessageModel> GetAllMessages()
        {
            lock (lockObj)
                return MessagesCache;
        }



        public bool TryDeleteMessage(in int Id, out string error)
        {
            if (MessagesCache.Count == 0)
            {
                error = "Вы не можете удалить сообщения, так как чат пуст";
                return false;
            }

            if (Id > CurrentOrderNumber || Id < 1)
            {
                error = "Введён некорректный идентификатор";
                return false;
            }

            var deletedMessage = MessagesCache[Id - 1];
            lock (lockObj)
            {

                MessagesCache.Remove(deletedMessage);

                SqlConnection.Open();

                var MessageIdInDB = OrderNumber_MessageSqlID[Id];
                OrderNumber_MessageSqlID.Remove(Id);

                var sqlCommand = new SqlCommand("DELETE FROM [Messages] WHERE [Id]=@Id", SqlConnection);

                sqlCommand.Parameters.AddWithValue("Id", MessageIdInDB);
                sqlCommand.ExecuteNonQuery();

                SqlConnection.Close();

                foreach (var mes in MessagesCache)
                {
                    var oldId = int.Parse(mes.MessageID);
                    if (oldId > Id)
                    {
                        mes.MessageID = (oldId - 1).ToString();
                    }
                }

                var newDict = new Dictionary<int, string>();
                foreach (var (key, value) in OrderNumber_MessageSqlID)
                {
                    var newKey = key;
                    if (newKey > Id)
                    {
                        newKey--;
                    }
                    newDict.Add(newKey, value);
                }

                OrderNumber_MessageSqlID = newDict;

                currenOrderNumber--;

                error = null;
                return true;
            }
        }
    }
}
