using System;
using System.Collections.Generic;
using System.Text;

namespace Chat
{
    public class Client
    {
        public string UserName { get; set; }

        public Client(string UserName)
        {
            this.UserName = UserName;
        }

    }
}
