using Akasztofa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangmanServer
{
    internal class Session
    {
        private Guid sessionID;
        private User userData;
        private double timeout;

        private static double DefaultTimeout = Config.GetInstance().config.timeoutMinutes * 60.0; //5 minutes

        public Session(User userData)
        {
            timeout = DefaultTimeout;
            this.sessionID = Guid.NewGuid();
            this.userData = userData;
        }

        public void RefreshSession()
        {
            timeout = DefaultTimeout;
        }

        public void Update(double seconds_passed)
        { 
            timeout -= seconds_passed;
        }

        public bool IsTimedOut()
        {
            return timeout < 0.0;
        }

        public User GetUserData()
        {
            return userData;
        }

        public Guid GetSessionID()
        {
            return sessionID;
        }
    }
}
