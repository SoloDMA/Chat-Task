using Chat.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chat.Bots.Contract
{
    public interface IBot
    {
        /// <summary>
        ///     Реакция бота на события чата.
        /// </summary>
        /// <param name="ChatEvent">
        ///     Тип события чата.
        /// </param>
        /// <returns>
        ///     Реакция на событие чата.
        /// </returns>
        string ReactionOnEvent(CommandAndEventType.EventType ChatEvent);

        /// <summary>
        ///     Бот получает сообщение от клиента и реагириует на тригеры, если они есть.
        /// </summary>
        /// <param name="message">
        ///     Текст сообщения
        /// </param>
        /// <returns>
        ///     Возвращает строку - реакцию на триггер, присутствующий в сообщении.
        ///     Если триггер не найден, то возвращает null
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Если <paramref name="message"/> пустая строка или null
        /// </exception>
        string ReactionOnMessage(string message);

        /// <summary>
        ///     Выполнение команды бота.
        /// </summary>
        /// <param name="command">
        ///     Команда бота
        /// </param>
        /// <param name="argement">
        ///     Аргумент команды.
        ///     Этот параметр может быть null, так как не у всех ботов команды содержат аргумент.
        /// </param>
        /// <returns>
        ///     Строка, содержащая результат выполнения команды.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Если <paramref name="command"/> пустая строка или null
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Есил произошла внутрення ошибка метода, связанная с определением типа команды бота
        /// </exception>
        string ExecuteCommand(string command, string argement); 
    }
}
