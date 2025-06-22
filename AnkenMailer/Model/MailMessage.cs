using HtmlAgilityPack;
using MailKit;
using MailKit.Net.Imap;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using static AnkenMailer.ColumnFilterWindow;

namespace AnkenMailer.Model
{
    public class MailMessage
    {
        public MailMessage(string? body)
        {
            Body = body;
        }

        public string? Body
        {
            private set;
            get;
        }

        public static MailMessage? Load(long id)
        {
            using var command = App.CurrentApp.Connection.CreateCommand();
            command.CommandText = "select * from [Message] where [EnvelopeId]=@envelopeId";
            command.Parameters.AddWithValue("@envelopeId", id);
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new MailMessage(reader.GetString("Body"));
                }
                else
                {
                    return null;
                }
            }
        }

        public static MailMessage? LoadFromImap(MailItem mailItem, ImapClient client)
        {
            var folder = client.GetFolder(mailItem.FolderPath);
            if (folder != null)
            {
                folder.Open(FolderAccess.ReadOnly);
                var message = folder.GetMessage(mailItem.UId);
                var mailMessage = new Func<MailMessage>(() =>
                {
                    // プレーンテキスト部分がある場合はそれを使う
                    if (!string.IsNullOrWhiteSpace(message.TextBody))
                    {
                        return new MailMessage(message.TextBody);
                    }

                    //  HTMLだけある場合は、それをプレーンテキストに変換
                    if (!string.IsNullOrWhiteSpace(message.HtmlBody))
                    {
                        return new MailMessage(HtmlToPlainText(message.HtmlBody));

                    }

                    // Multipart構造の場合、ボディパートを明示的に探す
                    if (message.Body is Multipart multipart)
                    {
                        foreach (var part in multipart)
                        {
                            if (part is TextPart textPart)
                            {
                                if (textPart.IsHtml)
                                    return new MailMessage(HtmlToPlainText(textPart.Text));
                                if (textPart.IsPlain)
                                    return new MailMessage(textPart.Text);
                            }
                        }
                    }
                    return new MailMessage(null);
                })();

                using var command = App.CurrentApp.Connection.CreateCommand();
                command.CommandText = """
                                    INSERT INTO [Message] ("EnvelopeId", "Body")
                                    VALUES ( 
                                        @envelopeId
                                        , @body
                                    )
                                    """;
                command.Parameters.AddWithValue("@envelopeId", mailItem.Id);
                command.Parameters.AddWithValue("@body", mailMessage.Body);
                command.ExecuteNonQuery();

                return mailMessage;
            }
            else
            {
                return null;
            }
        }

        static string HtmlToPlainText(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // script, style, head などノイズ要素を削除
            var noisyNodes = doc.DocumentNode.SelectNodes("//script|//style|//head");
            if (noisyNodes != null)
            {
                foreach (var node in noisyNodes)
                {
                    node.Remove();
                }
            }

            // <br> を明示的な改行に置換
            var brs = doc.DocumentNode.SelectNodes("//br");
            if (brs != null)
            {
                foreach (var br in brs)
                {
                    br.ParentNode.ReplaceChild(doc.CreateTextNode("\n"), br);
                }
            }

            // <p>, <div> にも改行を追加
            var blocks = doc.DocumentNode.SelectNodes("//p|//div");
            if (blocks != null)
            {
                foreach (var block in blocks)
                {
                    block.AppendChild(doc.CreateTextNode("\n"));
                }
            }

            // 全体テキストを取得・整形
            var text = doc.DocumentNode.InnerText;

            return System.Text.RegularExpressions.Regex
                .Replace(text, @"\s+", " ")  // 連続する空白や改行を1つに
                .Trim();
        }

    }
}
