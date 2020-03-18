using Chat.Models;
using Chat.View;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Chat
{
    class Data
    {
        public enum States { NONE, R_USERNAME_LEN, R_USERNAME, R_MES_LEN, R_MES, R_DATE_LEN, R_DATE, WRITE_FULL_MES };


        public readonly List<MessageModel> Messages = new List<MessageModel>();

        private States StateOfRead;

        private readonly int BufferSize = 4096;
        private readonly int AmountBytesInMessageLen = 4;


        private readonly string MainMessagesFileName = "messages.dat";
        private readonly string BufferMessagesFileName = "messages(buffer).dat";
        private readonly string BackupMessagesFileName = "messages(backup).dat";
        private readonly string ActionsFileName = "actions.dat";


        public void AddNewMessage(MessageModel message)
        {

            Messages.Add(message);
            WriteMessageInFile(message, MainMessagesFileName, FileMode.Append);
        }

        public void WriteMessageInFile(MessageModel message, string InWhatFileWritingNow, FileMode fileMode)
        {
            
            byte[] UserNameBytes = Encoding.UTF8.GetBytes(message.UserName);
            byte[] MessageBytes = Encoding.UTF8.GetBytes(message.TextOfMessage);
            byte[] MessageLen = BitConverter.GetBytes(MessageBytes.Length);
            byte[] DateOfTicksBytes = BitConverter.GetBytes(message.TimeOfMessage.Ticks);
            if (MessageLen.Length > AmountBytesInMessageLen)
            {
                ConsoleView.Write("Слишком большое сообщение. Пожалуйста, напишите сократите сообщение или разбейте его на несколько сообщений");
                return;
            }
            using (var FileStream = File.Open(InWhatFileWritingNow, fileMode))
            {
                FileStream.WriteByte((byte)UserNameBytes.Length);
                FileStream.Write(UserNameBytes, 0, UserNameBytes.Length);
                FileStream.Write(MessageLen, 0, AmountBytesInMessageLen);
                FileStream.Write(MessageBytes, 0, MessageBytes.Length);
                FileStream.WriteByte((byte)DateOfTicksBytes.Length);
                FileStream.Write(DateOfTicksBytes, 0, DateOfTicksBytes.Length);
            }
        }


        public void ReadFromFileAllMessages()
        {
            using (var readFile = File.Open(MainMessagesFileName, FileMode.OpenOrCreate))
            {
                if (readFile.Length == 0)
                {
                    return;
                }
                readFile.Seek(0, SeekOrigin.Begin);

                MemoryStream memStream = new MemoryStream(); //записывает данные в оперативку (используется как временное хранилище)
                StreamReader streamReader = new StreamReader(memStream); //чтение всех данных, которые находятся в memStream;
                byte[] messagesBuffer = new byte[BufferSize]; //байты из файла
                byte[] ticks = new byte[8]; //дата отправки сообщения
                byte[] lenOfMessageTextBytes = new byte[4]; //длина сообщения

                int readedBytes = -1; //сколько байтов считано по факту
                int positionInMessagesBuffer = 0; //текущая позиция в массиве messagesBuffer
                bool needRead = true; //указывает нужно ли прочитать новую порцию данных из файла

                long dateInTicks = 0; //дата в тиках

                int leftToWriteInLenOfMessage = AmountBytesInMessageLen; //сколько осталось записать байт в массив lenOfMessageTextBytes

                int posInTicks = 0; //текущая позиция в массиве ticks
                int posInLenOfMessage = 0; //текущая позиция в массиве lenOfMessageTextBytes

                int usernameLen = 0; //длина имени пользователя
                int mesLen = 0; //длина текста сообщения
                int dateLen = 0; //длина даты

                string UserName = "", TextOfMessage = ""; 

                StateOfRead = States.R_USERNAME_LEN; //устанавливаем начальное состояние чтение

                while (readedBytes != 0)
                {
                    if (needRead)
                    {
                        readedBytes = readFile.Read(messagesBuffer, 0, BufferSize);
                        needRead = false;
                    }

                    switch (StateOfRead)
                    {
                        case States.R_USERNAME_LEN:
                            usernameLen = messagesBuffer[positionInMessagesBuffer++];
                            StateOfRead = States.R_USERNAME;
                            break;
                        case States.R_USERNAME:
                            WriteInMemory(messagesBuffer, ref positionInMessagesBuffer, memStream, readedBytes, ref usernameLen);
                            if (usernameLen == 0)
                            {
                                memStream.Seek(0, SeekOrigin.Begin);
                                UserName = streamReader.ReadToEnd();
                                memStream.SetLength(0);
                                StateOfRead = States.R_MES_LEN;
                            }
                            break;
                        case States.R_MES_LEN:
                            WriteInMemory(messagesBuffer, ref positionInMessagesBuffer, lenOfMessageTextBytes, ref posInLenOfMessage, readedBytes, ref leftToWriteInLenOfMessage);
                            if (leftToWriteInLenOfMessage == 0)
                            {
                                mesLen = BitConverter.ToInt32(lenOfMessageTextBytes);
                                posInLenOfMessage = 0;
                                leftToWriteInLenOfMessage = 4;
                                StateOfRead = States.R_MES;
                                break;
                            }
                            break;
                        case States.R_MES:
                            WriteInMemory(messagesBuffer, ref positionInMessagesBuffer, memStream, readedBytes, ref mesLen);
                            if (mesLen == 0)
                            {
                                memStream.Seek(0, SeekOrigin.Begin);
                                TextOfMessage = streamReader.ReadToEnd();
                                memStream.SetLength(0);
                                StateOfRead = States.R_DATE_LEN;
                            }
                            break;
                        case States.R_DATE_LEN:
                            dateLen = messagesBuffer[positionInMessagesBuffer++];
                            StateOfRead = States.R_DATE;
                            break;
                        case States.R_DATE:
                            WriteInMemory(messagesBuffer, ref positionInMessagesBuffer, ticks, ref posInTicks, readedBytes, ref dateLen);
                            if (dateLen == 0)
                            {
                                dateInTicks = BitConverter.ToInt64(ticks);
                                posInTicks = 0;
                                dateLen = 8;
                                StateOfRead = States.WRITE_FULL_MES;
                                break;
                            }
                            break;
                        case States.WRITE_FULL_MES:
                            Messages.Add(new MessageModel { UserName = UserName, TextOfMessage = TextOfMessage, TimeOfMessage = DateTime.FromBinary(dateInTicks) });
                            StateOfRead = States.R_USERNAME_LEN;
                            memStream.SetLength(0);
                            break;
                    }

                    if (positionInMessagesBuffer > readedBytes - 1)
                    {
                        positionInMessagesBuffer = 0;
                        needRead = true;
                    }

                }
                streamReader.Dispose();
                memStream.Dispose();
            }

        }


        private static void WriteInMemory(byte[] sourceBuffer, ref int positionInSource, MemoryStream destinationBuffer, int readedByte, ref int len)
        {
            int howMuchCanWrite;
            if (len < readedByte - positionInSource)
            {
                howMuchCanWrite = len;
            }
            else
            {
                howMuchCanWrite = readedByte - positionInSource;
            }
            destinationBuffer.Write(sourceBuffer, positionInSource, howMuchCanWrite);
            positionInSource += howMuchCanWrite;
            len -= howMuchCanWrite;
        }

        private static void WriteInMemory(byte[] sourceBuffer, ref int positionInSource, byte[] destinationBuffer, ref int positionInDestination, int readedByte, ref int len)
        {
            int howMuchCanWrite;
            if (len < readedByte - positionInSource)
            {
                howMuchCanWrite = len;
            }
            else
            {
                howMuchCanWrite = readedByte - positionInSource;
            }
            Array.Copy(sourceBuffer, positionInSource, destinationBuffer, positionInDestination, howMuchCanWrite);
            positionInSource += howMuchCanWrite;
            len -= howMuchCanWrite;
            positionInDestination += howMuchCanWrite;
        }

        public void WriteChatAction(string Action)
        {
            byte[] RecordableStringBytesArray = Encoding.UTF8.GetBytes(Action);

            using (var FileStream = File.Open(ActionsFileName, FileMode.Append))
            {
                FileStream.Write(RecordableStringBytesArray, 0, RecordableStringBytesArray.Length);
            }
        }

        public void DeleteMessage(int MessageId)
        {
            if (Messages.Count > 0 && MessageId <= Messages.Count)
            {
                MessageModel deletedMessage = Messages[MessageId - 1];
                Messages.Remove(deletedMessage);
                foreach (var mes in Messages)
                {
                    WriteMessageInFile(mes, BufferMessagesFileName, FileMode.OpenOrCreate);
                }
                File.Copy(BufferMessagesFileName, MainMessagesFileName, true);
            }
            else if (MessageId > Messages.Count)
            {
                ConsoleView.Write("Введённый вами идентификатор, превышает количество сообщений, имеющихся в чате");
            }
            else 
            {
                ConsoleView.Write("Вы не можете удалить сообщения, так как чат пуст");
            }
            
        }
    }
}
