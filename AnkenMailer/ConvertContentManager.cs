using Azure;
using Azure.AI.OpenAI;
using HtmlAgilityPack;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.Data.Sqlite;
using MimeKit;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AnkenMailer
{
    class ConvertContentManager
    {
        private CancellationTokenSource? cts = null;

        public event EventHandler<Progress>? Progress;

        public async Task Cnacel()
        {

            //■キャンセル対象が有るかどうか
            if (this.cts != null)
            {
                //■キャンセルする
                this.cts.Cancel();

                //■キャンセルしたTask終わるまで待つ
                await Task.Run(async () =>
                {
                    while (this.cts != null)
                    {
                        await Task.Delay(100); // 100ms ごとにチェック
                    }
                });
                this.cts = null;
            }
        }


        public async Task Convert(IList<MailItem> mailItems)
        {
            IProgress<Progress> progress = new Progress<Progress>(progress =>
            {
                if (this.Progress != null)
                {
                    this.Progress(this, progress);
                }
            });

        
            await this.Cnacel();
            this.cts = new CancellationTokenSource();

#pragma warning disable CS4014 // この呼び出しは待機されなかったため、現在のメソッドの実行は呼び出しの完了を待たずに続行されます
            var cancelToken = this.cts.Token;
            Task.Run(async () =>
            {
                progress.Report(new Progress(0, "処理中..."));
                using (var client = new ImapClient())
                {
                    // ■IMAPに接続
                    client.Connect(Properties.Settings.Default.ImapServer, Properties.Settings.Default.ImapPort, SecureSocketOptions.SslOnConnect);
                    client.Authenticate(Properties.Settings.Default.ImapUser, Properties.Settings.Default.ImapPassword);

                    //■AzureOpenAIに接続

                    var chatClient = new Func<ChatClient>(() =>
                    {
                        try
                        {
                            var endpoint = new Uri(Properties.Settings.Default.Endpoint);
                            var deploymentName = Properties.Settings.Default.DeploymentName;
                            var apiKey = Properties.Settings.Default.ApiKey;

                            AzureOpenAIClient azureClient = new(
                                endpoint,
                                new AzureKeyCredential(apiKey));
                            return azureClient.GetChatClient(deploymentName);
                        }

                        catch (Exception exception)
                        {
                            throw new ApplicationException("azure AIの接続に失敗しました。", exception);
                        }
                    })();

                    var index = 0;
                    foreach (var mailItem in mailItems)
                    {
                        cancelToken.ThrowIfCancellationRequested();

                        //■Envelopeの保存
                        {
                            //■存在確認とIdの取得
                            {
                                using var command = App.CurrentApp.Connection.CreateCommand();
                                command.CommandText = "select [EnvelopeId] from [Envelope] where [MessageId]=@messageId and [From] = @from;";
                                command.Parameters.AddWithValue("@messageId", mailItem.Envelope.MessageId);
                                command.Parameters.AddWithValue("@from", mailItem.Envelope.From.ToString());
                                mailItem.Id = command.ExecuteScalar() as long?;
                            }

                            //■DBになかったらinsert
                            if (mailItem.Id == null)
                            {
                                using var command = App.CurrentApp.Connection.CreateCommand();
                                command.CommandText = """
                                INSERT INTO [Envelope] (
                                    "MessageId"
                                    , "Date"
                                    , "From"
                                    , "Bcc"
                                    , "Cc"
                                    , "InReplyTo"
                                    , "ReplyTo"
                                    , "Sender"
                                    , "Subject"
                                    , "To"
                                ) 
                                VALUES ( 
                                    @messageId
                                    , @date
                                    , @from
                                    , @bcc
                                    , @cc
                                    , @inReplyTo
                                    , @replyTo
                                    , @sender
                                    , @subject
                                    , @to
                                )
                                RETURNING EnvelopeId;
                                """;
                                command.Parameters.AddWithValue("@messageId", mailItem.Envelope.MessageId);
                                command.Parameters.AddWithValue("@date", mailItem.Envelope.Date == null ? DBNull.Value : mailItem.Envelope.Date?.ToString("o"));
                                command.Parameters.AddWithValue("@from", mailItem.Envelope.From.Count == 0 ? DBNull.Value : mailItem.Envelope.From.ToString());
                                command.Parameters.AddWithValue("@bcc", mailItem.Envelope.Bcc.Count == 0 ? DBNull.Value : mailItem.Envelope.Bcc.ToString());
                                command.Parameters.AddWithValue("@cc", mailItem.Envelope.Cc.Count == 0 ? DBNull.Value : mailItem.Envelope.Cc.ToString());
                                command.Parameters.AddWithValue("@inReplyTo", mailItem.Envelope.InReplyTo == null ? DBNull.Value : mailItem.Envelope.InReplyTo);
                                command.Parameters.AddWithValue("@replyTo", mailItem.Envelope.ReplyTo == null ? DBNull.Value : mailItem.Envelope.ReplyTo.ToString());
                                command.Parameters.AddWithValue("@sender", mailItem.Envelope.Sender == null ? DBNull.Value : mailItem.Envelope.Sender.ToString());
                                command.Parameters.AddWithValue("@subject", mailItem.Envelope.Subject);
                                command.Parameters.AddWithValue("@to", mailItem.Envelope.To.ToString());
                                mailItem.Id = command.ExecuteScalar() as long?;
                            }
                        }
                        //■メッセージの保存
                        if (mailItem.Id != null)
                        {
                            //■存在確認
                            var mailMessage = new Func<MailMessage?>(() =>
                            {
                                using var command = App.CurrentApp.Connection.CreateCommand();
                                command.CommandText = "select * from [Message] where [EnvelopeId]=@envelopeId";
                                command.Parameters.AddWithValue("@envelopeId", mailItem.Id);
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
                                
                            })();

                            if (mailMessage == null)
                            {
                                var folder = client.GetFolder(mailItem.FolderPath);
                                folder.Open(FolderAccess.ReadOnly);
                                if (folder != null)
                                {
                                    var message = folder.GetMessage(mailItem.UId);
                                    mailMessage = new Func<MailMessage>(() =>
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
                                }
                            }
                            mailItem.Message = mailMessage;

                            //■JSONにコンバート
                            {
                                //■存在確認
                                var ankens = new Func<IList<Anken>?>(() =>
                                {

                                    //■Ankenの取得
                                    var list = new List<Anken>();
                                    {
                                        using var command = App.CurrentApp.Connection.CreateCommand();
                                        command.CommandText = "select * from [Anken] where [EnvelopeId]=@envelopeId";
                                        command.Parameters.AddWithValue("@envelopeId", mailItem.Id);
                                        using (var reader = command.ExecuteReader())
                                        {
                                            while (reader.Read())
                                            {
                                                Anken temp = new Anken();
                                                temp.Index = reader.GetInt32("Index");
                                                temp.Name = reader.IsDBNull("Name") ? null : reader.GetString("Name");
                                                temp.Start = reader.IsDBNull("Start") ? null : reader.GetString("Start");
                                                temp.End = reader.IsDBNull("End") ? null : reader.GetString("End");
                                                temp.StartYearMonth = reader.IsDBNull("StartYearMonth") ? null : reader.GetString("StartYearMonth");
                                                temp.Place = reader.IsDBNull("Place") ? null : reader.GetString("Place");
                                                temp.Details = reader.IsDBNull("Details") ? null : reader.GetString("Details");
                                                temp.MainSkill = reader.IsDBNull("MainSkill") ? null : reader.GetString("MainSkill");
                                                temp.RequiredSkills = reader.IsDBNull("RequiredSkills") ? [] : reader.GetString("RequiredSkills").Split(",");
                                                temp.DesirableSkills = reader.IsDBNull("DesirableSkills") ? [] : reader.GetString("DesirableSkills").Split(",");
                                                temp.MaxUnitPrice = reader.IsDBNull("MaxUnitPrice") ? null : reader.GetInt32("MaxUnitPrice");
                                                temp.MinUnitPrice = reader.IsDBNull("MinUnitPrice") ? null : reader.GetInt32("MinUnitPrice");
                                                temp.Remarks = reader.IsDBNull("Remarks") ? null : reader.GetString("Remarks");
                                                list.Add(temp);
                                            }
                                        }
                                    }
                                    if(list.Count == 0)
                                    {
                                        //■ヘッダの確認(すでに解析されていることの証左
                                        using var command = App.CurrentApp.Connection.CreateCommand();
                                        command.CommandText = "select count(*) from [AnkenHeader] where [EnvelopeId]=@envelopeId";
                                        command.Parameters.AddWithValue("@envelopeId", mailItem.Id);
                                        var count = (long?)command.ExecuteScalar();
                                        if (count == null)
                                        {
                                            throw new Exception("select count(*) from [AnkenHeader] where [EnvelopeId]=@envelopeId が想定外のnullを返却");
                                        }

                                        return count == 0L ? null : list;
                                    }
                                    else
                                    {
                                        return list;
                                    }
                                })();

                                if (ankens == null)
                                {
                                    if (mailMessage != null && mailMessage.Body != null)
                                    {
                                        List<ChatMessage> messages = new List<ChatMessage>()
                                        {
                                            new SystemChatMessage($$"""
                                                - あなたはプロのデータ抽出エージェントです。
                                                - 日本語の自然文から構造化データを抽出し、次の形式でJSONとして出力してください。これは、案件の情報です。
                                                [
                                                    {
                                                        name: string; //案件の名前
                                                        start: string; //案件の開始時期。表記内容を自然文でそのまま抽出してください。
                                                        end: string; //案件の終了時期。表記内容を自然文でそのまま抽出してください。
                                                        startYearMonth: string; //案件の開始時期。内容を解釈し、YYYY-MM形式で出力してください。
                                                        place: string;//作業場所
                                                        details: string;//作業内容。複数存在する場合は、連結してひとつの文字列にしてください。
                                                        mainSkill: string;//主な開発言語として、"JAVA",".NET", "iOS", "Android", "Pytion", "Ruby","それ以外"のどれかを選択してください。
                                                        requiredSkills: string[];//必須の技術スタックの一覧。desirableSkillsと区別がつかない場合は、requiredSkillsに格納してください。
                                                        desirableSkills: string[];//あると有利な技術スタックの一覧。
                                                        maxUnitPrice: number;//単価の最大。「万円」の単位にして下さい。30万円を下回ることはないですし、200万円を上回ることはないので、推測してください。ただし、記載がなければ、nullにして下さい。
                                                        minUnitPrice: number;//単価の最小。「万円」の単位にして下さい。30万円を下回ることはないですし、200万円を上回ることはないので、推測してください。ただし、記載がなければ、nullにして下さい。
                                                        remarks: string;//備考
                                                    }
                                                ]
                                                - javascriptではなく、正式なJSON形式にしてください。プロパティ名は""で囲う必要があります。
                                                - 結果は必ず配列にしてください。入力に案件の情報が見つからないとき、"[]"が結果となります。
                                                - 余計な説明や文章は出力せず、JSONのみを返してください。"```json"と"```"で囲う
                                                必要もありません。
                                                - 日本語の自然文に複数の案件情報が記載されることがあります。
                                                - 今日は、{{mailItem.Envelope.Date:yyyy-MM-dd}}です。自然文に記載される日付は、常に未来です。
                                                - 決して、"..."等で、データを省略しないでください。
                                                """),
                                            new UserChatMessage(mailMessage.Body),
                                        };

                                        var response = chatClient.CompleteChat(messages);

                                        //JSONの形式チェック
                                        var json = response.Value.Content[0].Text;
                                        try
                                        {
                                            var converted = JsonSerializer.Deserialize<Anken[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                            if (converted != null) 
                                                ankens = new List<Anken>(converted);
                                            else
                                                ankens = new List<Anken>();

                                        }
                                        catch (JsonException exception)
                                        {
                                            throw new Exception("Jsonの形式が不正です");
                                        }
                                    }

                                    //DBに登録
                                    if (ankens != null)
                                    {
                                        //■AnkenHeader
                                        {
                                            using var command = App.CurrentApp.Connection.CreateCommand();
                                            command.CommandText = """
                                            INSERT INTO [AnkenHeader]([EnvelopeId])
                                            VALUES (@EnvelopeId);
                                            """;
                                            command.Parameters.AddWithValue("@EnvelopeId", mailItem.Id);
                                            command.ExecuteNonQuery();
                                        }
                                        //■[Anken]
                                        for(var i = 0; i < ankens.Count; i++)
                                        {
                                            var anken = ankens[i];
                                            anken.Index = i;
                                            using var command = App.CurrentApp.Connection.CreateCommand();
                                            command.CommandText = """
                                            INSERT INTO [Anken]
                                                  ([EnvelopeId]
                                                  ,[Index]
                                                  ,[Name]
                                                  ,[Start]
                                                  ,[End]
                                                  ,[StartYearMonth]
                                                  ,[Place]
                                                  ,[Details]
                                                  ,[MainSkill]
                                                  ,[RequiredSkills]
                                                  ,[DesirableSkills]
                                                  ,[MaxUnitPrice]
                                                  ,[MinUnitPrice]
                                                  ,[Remarks])
                                            VALUES (
                                                   @EnvelopeId
                                                  ,@Index
                                                  ,@Name
                                                  ,@Start
                                                  ,@End
                                                  ,@StartYearMonth
                                                  ,@Place
                                                  ,@Details
                                                  ,@MainSkill
                                                  ,@RequiredSkills
                                                  ,@DesirableSkills
                                                  ,@MaxUnitPrice
                                                  ,@MinUnitPrice
                                                  ,@Remarks);
                                            """;

                                            command.Parameters.AddWithValue("@EnvelopeId", mailItem.Id);
                                            command.Parameters.AddWithValue("@Index", anken.Index);
                                            command.Parameters.AddWithValue("@Name", anken.Name != null ? anken.Name : DBNull.Value);
                                            command.Parameters.AddWithValue("@Start", anken.Start != null ? anken.Start : DBNull.Value);
                                            command.Parameters.AddWithValue("@End", anken.End != null ? anken.End : DBNull.Value);
                                            command.Parameters.AddWithValue("@StartYearMonth", anken.StartYearMonth != null ? anken.StartYearMonth : DBNull.Value);
                                            command.Parameters.AddWithValue("@Place", anken.Place != null ? anken.Place : DBNull.Value);
                                            command.Parameters.AddWithValue("@Details", anken.Details != null ? anken.Details : DBNull.Value);
                                            command.Parameters.AddWithValue("@MainSkill", anken.MainSkill != null ? anken.MainSkill : DBNull.Value);
                                            command.Parameters.AddWithValue("@RequiredSkills", anken.RequiredSkills != null ? string.Join(",", anken.RequiredSkills) : DBNull.Value);
                                            command.Parameters.AddWithValue("@DesirableSkills", anken.DesirableSkills != null ? string.Join(",", anken.DesirableSkills) : DBNull.Value);
                                            command.Parameters.AddWithValue("@MaxUnitPrice", anken.MaxUnitPrice != null ? anken.MaxUnitPrice : DBNull.Value);
                                            command.Parameters.AddWithValue("@MinUnitPrice", anken.MinUnitPrice != null ? anken.MinUnitPrice : DBNull.Value);
                                            command.Parameters.AddWithValue("@Remarks", anken.Remarks != null ? anken.Remarks : DBNull.Value);
                                            command.ExecuteNonQuery();
                                        }
                                    }
                                }
                                mailItem.Ankens = ankens;

                                //■進捗の通知
                                progress.Report(new Progress((int)Math.Floor(index * (100d / mailItems.Count)), "処理中..."));
                                index++;
                            }
                        }
                    }
                }

                
            }).ContinueWith(t =>
            {

                if (t.IsCanceled)
                {
                    progress.Report(new Progress("キャンセルしました"));
                }
                else if (t.IsFaulted)
                {
                    var excecption = t.Exception.InnerException != null ? t.Exception.InnerException : t.Exception;
                    progress.Report(new Progress("エラーが発生しました。" + excecption.Message, excecption.StackTrace));
                }
                else
                {
                    progress.Report(new Progress(100, "完了"));
                }
                this.cts = null;
            }, TaskScheduler.FromCurrentSynchronizationContext());
#pragma warning restore CS4014 // この呼び出しは待機されなかったため、現在のメソッドの実行は呼び出しの完了を待たずに続行されます
        }

        public async Task ReConvert(IList<MailItem> mailItems)
        {
            //■現在の解析をストップ
            await this.Cnacel();

            //■データベースの削除
            using var command = App.CurrentApp.Connection.CreateCommand();
            command.CommandText = "delete from [AnkenHeader] where [EnvelopeId] = @envelopeId";
            command.Parameters.Add("@envelopeId", SqliteType.Integer);
            foreach (var mailItem in mailItems)
            {
                command.Parameters[0].Value = ((MailItem)mailItem).Id;
                command.ExecuteNonQuery();
            }

            //■再解析
            await this.Convert(mailItems);
        }
        

        public string? GetBodyText(MailItem mailItem)
        {
            using var command = App.CurrentApp.Connection.CreateCommand();
            command.CommandText = "select [Body] from [Message]  where [EnvelopeId]=@envelopeId";
            command.Parameters.AddWithValue("@envelopeId", mailItem.Id);
            return command.ExecuteScalar() as string;
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


        public void GeContent(MailItem mailItem)
        {
            using var command = App.CurrentApp.Connection.CreateCommand();
            command.CommandText = """
                select * from [Content] 
                where [EnvelopeId]=@envelopeId
            """;
            command.Parameters.AddWithValue("@envelopeId", mailItem.Id);
            var reader = command.ExecuteReader();
            if (reader.Read())
            {
                int id = reader.GetInt32(0);         // カラムのインデックスで取得
                string name = reader.GetString(1);
                int age = reader.GetInt32(reader.GetOrdinal("age")); // カラム名でも取得可能

                Console.WriteLine($"ID: {id}, Name: {name}, Age: {age}");
            }
            else
            {

            }
        }
    }
}
