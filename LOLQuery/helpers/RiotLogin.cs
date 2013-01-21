using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace helpers
{
    public class RiotLogin
    {
        public string Username;
        public string Password;
        public bool inUse;

        public RiotLogin(string user, string pass)
        {
            Username = user;
            Password = pass;
            inUse = false;
        }
    }
}
