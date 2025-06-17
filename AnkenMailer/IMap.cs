using MailKit.Net.Imap;
using MailKit.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AnkenMailer
{
    public class IMap
    {
        public static ImapClient Open()
        {
            var client = new ImapClient();
            client.Connect(Properties.Settings.Default.ImapServer, Properties.Settings.Default.ImapPort, SecureSocketOptions.SslOnConnect);
            client.Authenticate(Properties.Settings.Default.ImapUser, Properties.Settings.Default.ImapPassword);
            return client;
        }

    }
}
