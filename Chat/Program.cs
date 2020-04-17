using System.Collections.Generic;
using System.Text.RegularExpressions;
using Chat.Bots;
using Chat.Bots.Implementation;
using Chat.Data.Implementation;
using Chat.Handlers;
using Chat.View.Contract;
using Chat.View.Implementation;
using Chat.View.Implementation.HTML;

namespace Chat
{
    class Program
    {
        private static readonly RegexOptions IgnoreCaseCultureInvariant =
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

        static void Main(string[] args)
        {
            new ChatHtmlView(
                new SqlStorage(),
                new Dictionary<string, BotConfig>
                {
                    {
                        "joke",
                        new BotConfig
                        {
                            Discription = "кладезь юмора.",
                            Triggers = new[]
                            {
                                new Regex("скучно", IgnoreCaseCultureInvariant),
                                new Regex("грустно", IgnoreCaseCultureInvariant),
                                new Regex("хочу анекдот", IgnoreCaseCultureInvariant)
                            },
                            Bot = new JokeBot()
                        }
                    },
                    {
                        "time",
                        new BotConfig
                        {
                            Discription = "подсказывает точное время.",
                            Triggers = new[]
                            {
                                new Regex("через",IgnoreCaseCultureInvariant)
                            },
                            Bot = new TimeBot()
                        }
                    },
                    {
                        "downloader",
                        new BotConfig
                        {
                            Discription = "реагирует на ссылки в сообщении и загружает заголовки страниц.",
                            Triggers =  new[]
                            {
                                new Regex("http://", IgnoreCaseCultureInvariant),
                                new Regex("https://",  IgnoreCaseCultureInvariant)
                            },
                            Bot = new DownloadBot()
                        }
                    }
                }
                ).Start();
        }
    }
}
