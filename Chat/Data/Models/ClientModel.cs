using System;
using System.Collections.Generic;
using System.Text;

namespace Chat.Data.Models
{
    public class ClientModel
    {
        public string UserName { get; }
        public DateTime LoginTime { get; }

        public ClientModel(string userName, DateTime loginTime)
        {
            UserName = userName;
            LoginTime = loginTime;
        }

    }
}
