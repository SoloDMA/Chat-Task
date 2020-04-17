using Chat.Data.Models;
using System.Collections.Generic;

namespace Chat.Data.Contract
{
    interface IChatStorage
    {

        /// <summary>
        ///     Возвращает список сообщений
        /// </summary>
        /// <returns>
        ///     Список сообщений чата
        /// </returns>
        List<MessageModel> GetAllMessages();

        /// <summary>
        ///     Запись события чата в хранилище.
        /// </summary>
        /// <param name="action">
        ///     Описание события.
        /// </param>
        /// <param name="error">
        ///     Сообщение об ошибке.
        ///     error = null, если метод отработал успешно
        /// </param>
        /// <returns>
        ///     true  - если событие успешно записано в хранилище.
        ///     false - если произошла ошибка во время записи.
        /// </returns>
        bool AddAction(string action, out string error);

        /// <summary>
        ///     Запись сообщения в хранилище.
        /// </summary>
        /// <param name="messageModel">
        ///     Объект, представляющий модель записываемого сообщения.
        /// </param>
        /// <param name="error">
        ///     Сообщение об ошибке.
        ///     error = null, если метод отработал успешно.
        /// </param>
        /// <returns>
        ///     true  - если событие успешно записано в хранилище.
        ///     false - если произошла ошибка во время записи.
        /// </returns>
        bool AddMessage(MessageModel messageModel, out string error);

        /// <summary>
        ///     Удаление сообщения из хранилища.
        /// </summary>
        /// <param name="messageId">
        ///     Идентификатор удаляемого сообщения.
        /// </param>
        /// <param name="error">
        ///     Сообщение об ошибке.
        ///     error = null, если метод отработал успешно.
        /// </param>
        /// <returns>
        ///     true  - если событие успешно записано в хранилище.    
        ///     false - если произошла ошибка во время записи.
        /// </returns>
        bool TryDeleteMessage(in int messageId, out string error);
    }
}
