using AnkenMailer.Model;
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
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using static System.Runtime.InteropServices.JavaScript.JSType;
using MailMessage = AnkenMailer.Model.MailMessage;

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
            DateTime now = DateTime.Now;

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
                    foreach (var mailItem in mailItems)//■パラレル実行しても、imapサーバやAzure OpenAIがエラーを起こすのでシングル実行
                    {
                        cancelToken.ThrowIfCancellationRequested();


                        //■Messageの取得と保存
                        if (mailItem.Message == null)
                        {
                            mailItem.Message = MailMessage.Load(mailItem.Id);
                            if (mailItem.Message == null)
                            {
                                mailItem.Message = MailMessage.LoadFromImap(mailItem, client);
                            }
                        }


                        //■案件情報の取得と保存
                        {

                            //■AnkenHeader取得
                            if (mailItem.AnkenHeader == null)
                            {
                                mailItem.AnkenHeader = AnkenHeader.Load(mailItem.Id);
                                if (mailItem.AnkenHeader == null)
                                {
                                    mailItem.Ankens = Anken.Load(mailItem.Id);
                                }
                            }
                            if (mailItem.AnkenHeader == null)
                            {
                                //■AIによるデータ化
                                var (newAnkenHeader, newAnkens) = new Func<(AnkenHeader, IList<Anken>)>(() =>
                                {
                                    var newAnkenHeader = new AnkenHeader();
                                    newAnkenHeader.HasError = false;
                                    newAnkenHeader.CeateDateTime = now.ToString("yyyy/MM/dd HH:mm:ss");

                                    if (mailItem.Message != null && mailItem.Message.Body != null)
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
                                                        skills: string[];//その案件で用いるプログラム言語、データベース、フレームワーク、ライブラリなどの技術スタックの名前を、必ず全て列挙して下さい。mainSkillとは無関係に考えてください。記載がない場合は空の配列です。"その他"など推測は必要ありません。また求められている能力であっても、「コミュニケーション力」のような技術スタックの名前以外のものは含めません。
                                                        mainSkill: string;//作業内容の主な開発言語として、"JAVA","C#", "Swift", "Python", "Ruby","Go"のどれかを選択してください。どれにも当たらない場合は、"その他"にして下さい。
                                                        requiredSkills: string[];//必須の技術スタックの一覧。desirableSkillsと区別がつかない場合は、requiredSkillsに格納してください。
                                                        desirableSkills: string[];//あると有利な技術スタックの一覧。
                                                        maxUnitPrice: number;//単価の最大。「万円」の単位にして下さい。30万円を下回ることはないですし、200万円を上回ることはないので、推測してください。ただし、記載がなければ、nullにして下さい。さらに小数部は四捨五入で丸めてください。
                                                        minUnitPrice: number;//単価の最小。「万円」の単位にして下さい。30万円を下回ることはないですし、200万円を上回ることはないので、推測してください。ただし、記載がなければ、nullにして下さい。さらに小数部は四捨五入で丸めてください。
                                                        remarks: string;//備考
                                                    }
                                                ]
                                                - javascriptではなく、正式なJSON形式にしてください。プロパティ名は""で囲う必要があります。
                                                - 結果は必ず配列にしてください。入力に案件の情報が見つからないとき、"[]"が結果となります。
                                                - 余計な説明や文章は出力せず、JSONのみを返してください。"```json"と"```"で囲う必要もありません。
                                                - 日本語の自然文に複数の案件情報が記載されることがあります。
                                                - 今日は、{{mailItem.Envelope.Date:yyyy-MM-dd}}です。自然文に記載される日付は、常に未来です。
                                                - 決して、"..."等で、データを省略しないでください。
                                                """),
                                            new UserChatMessage(mailItem.Message.Body),
                                        };

                                        var response = chatClient.CompleteChat(messages);

                                        //■LLM使用料の記録
                                        {
                                            using var command = App.CurrentApp.Connection.CreateCommand();
                                            command.CommandText = """
                                                INSERT INTO [LlmUsage](
                                                    [Date]
                                                    ,[InputTokenCount]
                                                    ,[OutputTokenCount]
                                                )
                                                VALUES (
                                                    @Date
                                                    , @InputTokenCount
                                                    , @OutputTokenCount
                                                );
                                                """;
                                            command.Parameters.AddWithValue("@Date", DateTimeOffset.Now.ToString("o"));
                                            command.Parameters.AddWithValue("@InputTokenCount", response.Value.Usage.InputTokenCount);
                                            command.Parameters.AddWithValue("@OutputTokenCount", response.Value.Usage.OutputTokenCount);
                                            command.ExecuteNonQuery();
                                            
                                        }
                                       
                                        //■JSONの形式チェック
                                        var json = response.Value.Content[0].Text;
                                        try
                                        {
                                            var converted = JsonSerializer.Deserialize<Anken[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                            return (newAnkenHeader, converted != null ? new List<Anken>(converted) : new List<Anken>());
                                        }
                                        catch (JsonException exception)
                                        {
                                            //エラーを記録して続行
                                            newAnkenHeader.HasError = true;
                                            newAnkenHeader.ErrorMessage = exception.Message;
                                            newAnkenHeader.Json = json;
                                            return (newAnkenHeader, new List<Anken>());
                                        }
                                    }
                                    else
                                    {
                                        return (newAnkenHeader, new List<Anken>());
                                    }
                                })();

                                //■DBに登録
                                {
                                    //■AnkenHeader
                                    {
                                        using var command = App.CurrentApp.Connection.CreateCommand();
                                        command.CommandText = """
                                        INSERT INTO [AnkenHeader]
                                                ([EnvelopeId]
                                                ,[HasError]
                                                ,[ErrorMessage]
                                                ,[JSON]
                                                ,[CeateDateTime])
                                        VALUES (
                                                @EnvelopeId
                                                ,@HasError
                                                ,@ErrorMessage
                                                ,@JSON
                                                ,@CeateDateTime
                                        );
                                        """;
                                        command.Parameters.AddWithValue("@EnvelopeId", mailItem.Id);
                                        command.Parameters.AddWithValue("@HasError", newAnkenHeader.HasError);
                                        command.Parameters.AddWithValue("@ErrorMessage", newAnkenHeader.ErrorMessage == null ? DBNull.Value : newAnkenHeader.ErrorMessage);
                                        command.Parameters.AddWithValue("@JSON", newAnkenHeader.Json == null ? DBNull.Value : newAnkenHeader.Json);
                                        command.Parameters.AddWithValue("@CeateDateTime", newAnkenHeader.CeateDateTime);
                                        command.ExecuteNonQuery();
                                    }
                                    //■[Anken]
                                    for (var i = 0; i < newAnkens.Count; i++)
                                    {
                                        var anken = newAnkens[i];
                                        anken.Index = i;

                                        using (var command = App.CurrentApp.Connection.CreateCommand())
                                        {
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

                                        //■Skill
                                        using (var command = App.CurrentApp.Connection.CreateCommand())
                                        {
                                            command.CommandText = """
                                                INSERT INTO [Skill]
                                                        ([EnvelopeId]
                                                        ,[Index]
                                                        ,[SkillName])
                                                VALUES (
                                                        @EnvelopeId
                                                        ,@Index
                                                        ,@SkillName);
                                                """;

                                            command.Parameters.AddWithValue("@EnvelopeId", mailItem.Id);
                                            command.Parameters.AddWithValue("@Index", anken.Index);
                                            var skillNameParameter = command.Parameters.Add("@SkillName", SqliteType.Text);
                                            foreach(var skillName in anken.Skills)
                                            {
                                                skillNameParameter.Value = skillName;
                                                command.ExecuteNonQuery();
                                            }
                                        }
                                    }                                  
                                }

                                //■メールアイテムに格納
                                mailItem.AnkenHeader = newAnkenHeader;
                                mailItem.Ankens = newAnkens;
                            }



                            //■進捗の通知
                            progress.Report(new Progress((int)Math.Floor(index * (100d / mailItems.Count)), "処理中..."));
                            index++;
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

            //■プロパティとデータベースの削除
            using var command = App.CurrentApp.Connection.CreateCommand();
            command.CommandText = "delete from [AnkenHeader] where [EnvelopeId] = @envelopeId";
            command.Parameters.Add("@envelopeId", SqliteType.Integer);
            foreach (var mailItem in mailItems)
            {
                //■プロパティの削除
                mailItem.AnkenHeader = null;
                mailItem.Ankens = null;

                //■DBの削除
                command.Parameters[0].Value = ((MailItem)mailItem).Id;
                command.ExecuteNonQuery();
            }

            //■再解析
            await this.Convert(mailItems);
        }

    }
}
