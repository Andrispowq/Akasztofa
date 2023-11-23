using Akasztofa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HangmanServer
{
    internal class Session
    {
        private Guid connectionID;
        private Guid clientID;
        private RSA rsa;
        private double timeout;

        private Guid sessionID;
        private User? userData;

        private static double DefaultTimeout = Config.GetInstance().config.timeoutMinutes * 60.0; //5 minutes

        public Session(Guid clientID)
        {
            this.sessionID = Guid.Empty;
            this.connectionID = Guid.NewGuid();
            this.clientID = clientID;
            this.rsa = RSA.Create();
            this.timeout = DefaultTimeout;
        }

        public void LoginUser(User userData)
        {
            this.sessionID = Guid.NewGuid();
            this.userData = userData;
        }

        public void LogoutUser()
        {
            this.sessionID = Guid.Empty;
            this.userData = null;
        }

        public string Decrypt(string encrypted)
        {
            encrypted = encrypted.Replace(" ", "+");
            byte[] encrypted_bytes = Convert.FromBase64String(encrypted);
            byte[] bytes = rsa.Decrypt(encrypted_bytes, RSAEncryptionPadding.Pkcs1);
            return Encoding.Unicode.GetString(bytes);
        }

        public User? GetUserData()
        {
            return userData;
        }

        public Guid GetConnectionID()
        {
            return connectionID;
        }

        public Guid GetClientID()
        {
            return clientID;
        }

        public Guid GetSessionID()
        {
            return sessionID;
        }

        public (byte[] exponent, byte[] modulus) GetPublicKey()
        {
            RSAParameters parameters = rsa.ExportParameters(false);
            return (parameters.Exponent!, parameters.Modulus!);
        }

        public void RefreshSession()
        {
            timeout = DefaultTimeout;
        }

        public void TimoutSession()
        {
            timeout = -1.0;
        }

        public void Update(double seconds_passed)
        {
            timeout -= seconds_passed;
        }

        public bool IsTimedOut()
        {
            return timeout < 0.0;
        }
    }
}
