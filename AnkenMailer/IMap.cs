using MailKit;
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

        public static IMailFolder GetTrash(IMailFolder parentFolder)
        {
            if (!parentFolder.IsOpen)
                parentFolder.Open(FolderAccess.ReadWrite);


            // サブフォルダを一覧取得
            var subfolders = parentFolder.GetSubfolders(false);

            // 名前一致するサブフォルダを探す
            foreach (var folder in subfolders)
            {
                if (folder.Name.Equals("removed", StringComparison.OrdinalIgnoreCase))
                {
                    return folder; // 既に存在
                }
            }

            // 存在しないので作成
            var newFolder = parentFolder.Create("removed", true);
            return newFolder;
        }

    }
}
