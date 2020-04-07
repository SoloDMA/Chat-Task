using Chat.Data.Contract;
using Chat.Data.Models;
using Chat.View;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Chat.Data.Implementation
{
    class FileStorage : IChatStorage
    {
        #region Fields

        private enum States
        {
            NONE,
            R_USERNAME_LEN,
            R_USERNAME,
            R_MES_LEN,
            R_MES,
            R_DATE_LEN,
            R_DATE,
            WRITE_FULL_MES
        };


        private List<MessageModel> messageCache;
        private List<MessageModel> MessagesCache
        {
            get
            {
                if (messageCache != null)
                    return messageCache;

                return messageCache = ReadMessages(MainMessagesFileName);
            }
        }

        private static int lastMessageID;
        public int LastMessageID
        {
            get
            {
                if (lastMessageID >= 1)
                    return lastMessageID;

                return 1;
            }

            set
            {
                lastMessageID = value;
            }
        }

        private const int BufferSize = 4096;
        private const int AmountBytesInMessageLen = 4;


        private readonly string MainMessagesFileName = "messages.dat";
        private readonly string BufferMessagesFileName = "messages(buffer).dat";
        private readonly string ActionsFileName = "actions.dat";


        #endregion

        #region Private Methods

        private bool TryWriteMessage(MessageModel message, string inWhatFileWritingNow, FileMode fileMode,
            out string error)
        {
            var userNameBytes = Encoding.UTF8.GetBytes(message.UserName);
            var messageBytes = Encoding.UTF8.GetBytes(message.TextOfMessage);
            var messageLen = BitConverter.GetBytes(messageBytes.Length);
            var dateOfTicksBytes = BitConverter.GetBytes(message.TimeOfMessage.Ticks);

            if (messageLen.Length > AmountBytesInMessageLen)
            {
                error = "Слишком большое сообщение. Пожалуйста, напишите сократите сообщение или разбейте его на несколько сообщений";
                return false;
            }

            using var fileStream = File.Open(inWhatFileWritingNow, fileMode);

            fileStream.WriteByte((byte)userNameBytes.Length);
            fileStream.Write(userNameBytes, 0, userNameBytes.Length);
            fileStream.Write(messageLen, 0, AmountBytesInMessageLen);
            fileStream.Write(messageBytes, 0, messageBytes.Length);
            fileStream.WriteByte((byte)dateOfTicksBytes.Length);
            fileStream.Write(dateOfTicksBytes, 0, dateOfTicksBytes.Length);

            error = null;
            return true;
        }

        private static List<MessageModel> ReadMessages(string storeFilePath)
        {
            using var readFile = File.Open(storeFilePath, FileMode.OpenOrCreate);

            if (readFile.Length == 0)
                return new List<MessageModel>();

            var messages = new List<MessageModel>();

            // Переиспользуемый поток, для сбора строковых значений.
            using var memStream = new MemoryStream();

            using var streamReader = new StreamReader(memStream); //чтение всех данных, которые находятся в memStream;

            var buffer = new byte[BufferSize]; //байты из файла
            var ticks = new byte[8]; //дата отправки сообщения
            var lenOfMessageTextBytes = new byte[4]; //длина сообщения

            var readBytes = -1; //сколько байтов считано по факту
            var bufferPosition = 0; //текущая позиция в массиве messagesBuffer
            var needRead = true; //указывает нужно ли прочитать новую порцию данных из файла

            var dateInTicks = 0l; //дата в тиках

            var leftToWriteInLenOfMessage =
                AmountBytesInMessageLen; //сколько осталось записать байт в массив lenOfMessageTextBytes

            var posInTicks = 0; //текущая позиция в массиве ticks
            var posInLenOfMessage = 0; //текущая позиция в массиве lenOfMessageTextBytes

            var userNameLength = 0; //длина имени пользователя
            var mesLen = 0; //длина текста сообщения
            var dateLen = 0; //длина даты

            string userName = string.Empty,
                textOfMessage = string.Empty;

            var readState = States.R_USERNAME_LEN; //устанавливаем начальное состояние чтение

            lastMessageID = 1;

            while (readBytes != 0)
            {
                if (needRead)
                {
                    readBytes = readFile.Read(buffer, 0, BufferSize);
                    needRead = false;
                }

                switch (readState)
                {
                    case States.R_USERNAME_LEN:

                        userNameLength = buffer[bufferPosition++];
                        readState = States.R_USERNAME;

                        break;
                    case States.R_USERNAME:

                        var writeBytes = WriteInMemory(memStream, buffer, readBytes, bufferPosition, userNameLength);

                        userNameLength -= writeBytes;
                        bufferPosition += writeBytes;

                        if (userNameLength == 0)
                        {
                            memStream.Position = 0;
                            userName = streamReader.ReadToEnd();
                            memStream.SetLength(0);
                            readState = States.R_MES_LEN;
                        }

                        break;
                    case States.R_MES_LEN:

                        var writeBytesMassage = WriteInMemory(buffer, bufferPosition, lenOfMessageTextBytes, posInLenOfMessage, readBytes, leftToWriteInLenOfMessage);

                        bufferPosition += writeBytesMassage;
                        leftToWriteInLenOfMessage -= writeBytesMassage;
                        posInLenOfMessage += writeBytesMassage;

                        if (leftToWriteInLenOfMessage == 0)
                        {
                            mesLen = BitConverter.ToInt32(lenOfMessageTextBytes);
                            posInLenOfMessage = 0;
                            leftToWriteInLenOfMessage = 4;
                            readState = States.R_MES;
                        }

                        break;
                    case States.R_MES:

                        var writeMessageBytes = WriteInMemory(memStream, buffer, readBytes, bufferPosition, mesLen);

                        mesLen -= writeMessageBytes;
                        bufferPosition += writeMessageBytes;

                        if (mesLen == 0)
                        {
                            memStream.Position = 0;
                            textOfMessage = streamReader.ReadToEnd();
                            memStream.SetLength(0);
                            readState = States.R_DATE_LEN;
                        }

                        break;
                    case States.R_DATE_LEN:

                        dateLen = buffer[bufferPosition++];
                        readState = States.R_DATE;

                        break;
                    case States.R_DATE:

                        var writeBytesDate = WriteInMemory(buffer, bufferPosition, ticks, posInTicks,
                             readBytes, dateLen);

                        bufferPosition += writeBytesDate;
                        dateLen -= writeBytesDate;
                        posInTicks += writeBytesDate;

                        if (dateLen == 0)
                        {
                            dateInTicks = BitConverter.ToInt64(ticks);
                            posInTicks = 0;
                            dateLen = 8;
                            readState = States.WRITE_FULL_MES;
                        }

                        break;
                    case States.WRITE_FULL_MES:

                        messages.Add(new MessageModel
                        {
                            MessageID = (lastMessageID++).ToString(),
                            UserName = userName,
                            TextOfMessage = textOfMessage,
                            TimeOfMessage = DateTime.FromBinary(dateInTicks)
                        });

                        readState = States.R_USERNAME_LEN;
                        memStream.SetLength(0);

                        break;
                }

                if (bufferPosition <= readBytes - 1)
                    continue;

                bufferPosition = 0;
                needRead = true;
            }

            return messages;
        }
        private static int WriteInMemory(MemoryStream destinationBuffer, byte[] sourceBuffer, int readByte, int positionInSource, int needWrite)
        {
            int howMuchCanWrite;

            if (needWrite < readByte - positionInSource)
            {
                howMuchCanWrite = needWrite;
            }
            else
            {
                howMuchCanWrite = readByte - positionInSource;
            }

            destinationBuffer.Write(sourceBuffer, positionInSource, howMuchCanWrite);

            return howMuchCanWrite;
        }

        private static int WriteInMemory(byte[] sourceBuffer, int positionInSource, byte[] destinationBuffer, int positionInDestination, int readByte, int needWrite)
        {
            int howMuchCanWrite;
            if (needWrite < readByte - positionInSource)
            {
                howMuchCanWrite = needWrite;
            }
            else
            {
                howMuchCanWrite = readByte - positionInSource;
            }

            Array.Copy(sourceBuffer, positionInSource, destinationBuffer, positionInDestination, howMuchCanWrite);

            return howMuchCanWrite;
        }

        #endregion

        #region Public Methods

        public bool AddMessage(MessageModel message, out string error)
        {
            MessagesCache.Add(message);
            return TryWriteMessage(message, MainMessagesFileName, FileMode.Append, out error);
        }

        public List<MessageModel> GetAllMessages()
        {
            return MessagesCache;
        }

        public bool AddAction(string action, out string error)
        {
            var recordableStringBytesArray = Encoding.UTF8.GetBytes(action);

            using var fileStream = File.Open(ActionsFileName, FileMode.Append);

            fileStream.Write(recordableStringBytesArray, 0, recordableStringBytesArray.Length);

            error = null;
            return true;
        }

        public bool TryDeleteMessage(in int messageId, out string error)
        {
            if (MessagesCache.Count == 0)
            {
                error = "Вы не можете удалить сообщения, так как чат пуст";
                return false;
            }

            if (messageId > MessagesCache.Count)
            {
                error = "Введённый вами идентификатор, превышает количество сообщений, имеющихся в чате";
                return false;
            }

            var deletedMessage = MessagesCache[messageId - 1];
            MessagesCache.Remove(deletedMessage);

            foreach (var mes in MessagesCache) 
            {
                var oldId = int.Parse(mes.MessageID);
                if (oldId > messageId)
                {
                    mes.MessageID = (oldId-1).ToString();
                }
            }

            lastMessageID--;

            foreach (var mes in MessagesCache)
            {
                if (!TryWriteMessage(mes, BufferMessagesFileName, FileMode.OpenOrCreate, out error))
                    return false;

            }

            File.Copy(BufferMessagesFileName, MainMessagesFileName, true);

            error = null;
            return true;
        }

        #endregion

    }
}

