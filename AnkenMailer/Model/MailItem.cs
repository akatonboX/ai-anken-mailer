using CommunityToolkit.Mvvm.ComponentModel;
using MailKit;
using MimeKit;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using static AnkenMailer.ColumnFilterWindow;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AnkenMailer.Model
{
    public class MailItem : ObservableObject
    {
        private long id;
        private string folderPath;
        private UniqueId uid;
        
        private Envelope envelope;
        private MailMessage? message = null;
        private AnkenHeader? ankenHeader = null;
        private IList<Anken>? ankens = null;


        public MailItem(string folderPath, UniqueId uid, Envelope envelope)
        {
            this.folderPath = folderPath;
            this.uid = uid;
            this.envelope = envelope;

            //■Envelopeの保存
            {
                //■存在確認とIdの取得
                var id = new Func<long?>(() =>
                {
                    using var command = App.CurrentApp.Connection.CreateCommand();
                    command.CommandText = "select [EnvelopeId] from [Envelope] where [MessageId]=@messageId and [From] = @from;";
                    command.Parameters.AddWithValue("@messageId", this.Envelope.MessageId);
                    command.Parameters.AddWithValue("@from", this.Envelope.From.ToString());
                    return command.ExecuteScalar() as long?;

                })();
                if (id == null) //■DB無ければインサート
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
                    command.Parameters.AddWithValue("@messageId", this.Envelope.MessageId);
                    command.Parameters.AddWithValue("@date", this.Envelope.Date == null ? DBNull.Value : this.Envelope.Date?.ToString("o"));
                    command.Parameters.AddWithValue("@from", this.Envelope.From.Count == 0 ? DBNull.Value : this.Envelope.From.ToString());
                    command.Parameters.AddWithValue("@bcc", this.Envelope.Bcc.Count == 0 ? DBNull.Value : this.Envelope.Bcc.ToString());
                    command.Parameters.AddWithValue("@cc", this.Envelope.Cc.Count == 0 ? DBNull.Value : this.Envelope.Cc.ToString());
                    command.Parameters.AddWithValue("@inReplyTo", this.Envelope.InReplyTo == null ? DBNull.Value : this.Envelope.InReplyTo);
                    command.Parameters.AddWithValue("@replyTo", this.Envelope.ReplyTo == null ? DBNull.Value : this.Envelope.ReplyTo.ToString());
                    command.Parameters.AddWithValue("@sender", this.Envelope.Sender == null ? DBNull.Value : this.Envelope.Sender.ToString());
                    command.Parameters.AddWithValue("@subject", this.Envelope.Subject);
                    command.Parameters.AddWithValue("@to", this.Envelope.To.ToString());
                    var newId = command.ExecuteScalar() as long?;
                    if (newId == null) throw new Exception("Envelopeのinsertで、予期しないnullが帰りました。");
                    this.id = (long)newId;


                }
                else //■DBあれば、その他の情報を読みこむ
                {
                    this.id = (long)id;
                    this.message = MailMessage.Load(this.Id);
                    if (this.message != null)
                    { 
                        this.ankenHeader = AnkenHeader.Load(this.Id);
                        
                        if (ankenHeader != null && !this.ankenHeader.HasError)
                        {
                            this.ankens = Anken.Load(this.Id);
                        }
                    }
                }
            }
        }



        public long Id
        {
            get => id;
            set => SetProperty(ref id, value);
        }


        public string FolderPath
        {
            get => folderPath;
            set => SetProperty(ref folderPath, value);
        }
        public UniqueId UId
        {
            get => uid;
            set => SetProperty(ref uid, value);
        }

        public Envelope Envelope
        {
            get => envelope;
            set{
                SetProperty(ref envelope, value);
                OnPropertyChanged(nameof(Subject));
                OnPropertyChanged(nameof(Sender));
                OnPropertyChanged(nameof(Date));
            }
        }

        public MailMessage? Message
        {
            get => message;
            set => SetProperty(ref message, value);
        }
        public AnkenHeader? AnkenHeader
        {
            get => ankenHeader;
            set
            {
                SetProperty(ref ankenHeader, value);
                RefreshView();
            }
        }
        public IList<Anken>? Ankens
        {
            get => ankens;
            set
            {
                SetProperty(ref ankens, value);
                RefreshView();
            }
        }

       

        //ここからViewプロパティ。DataGridのため
        private Anken? TopAnken
        {
            get
            {
                return Ankens == null || Ankens.Count == 0 ? null : Ankens[0];
            }
        }
        public string? Subject => Envelope.Subject;
        public string Sender => Envelope.Sender.ToString();
        public string? Date => Envelope.Date?.ToString("yyyy/MM/dd HH:mm:ss");

        public string AnalysisState => this.AnkenHeader == null ? "未" : !this.AnkenHeader.HasError ? "済" : "エラー";

        public int? AnkenCount => Ankens?.Count;
        public string? AnkenName => TopAnken?.Name;
        public string? MainSkill => TopAnken?.MainSkill;
        public string? Skills => TopAnken == null ? null : string.Join(",", TopAnken.Skills);

        public string? StartYearMonth => TopAnken?.StartYearMonth;
        public string? Place => TopAnken?.Place;
        public int? MaxUnitPrice => TopAnken?.MaxUnitPrice;
        public int? MinUnitPrice => TopAnken?.MinUnitPrice;
        public string? Remarks => TopAnken?.Remarks;
        public string? Details => TopAnken?.Details;
        public string? RequiredSkills => TopAnken == null ? null : string.Join(",", TopAnken.RequiredSkills);
        public string? DesirableSkills => TopAnken == null ? null : string.Join(",", TopAnken.DesirableSkills);
        public string? Start => TopAnken?.Start;
        public string? End => TopAnken?.End;
        public bool? HasError => this.AnkenHeader?.HasError;



        public void RefreshView()
        {
            OnPropertyChanged(nameof(AnkenCount));
            OnPropertyChanged(nameof(AnkenName));
            OnPropertyChanged(nameof(MainSkill));
            OnPropertyChanged(nameof(Skills));
            OnPropertyChanged(nameof(StartYearMonth));
            OnPropertyChanged(nameof(Place));
            OnPropertyChanged(nameof(MaxUnitPrice));
            OnPropertyChanged(nameof(MinUnitPrice));
            OnPropertyChanged(nameof(Remarks));
            OnPropertyChanged(nameof(Details));
            OnPropertyChanged(nameof(RequiredSkills));
            OnPropertyChanged(nameof(DesirableSkills));
            OnPropertyChanged(nameof(Start));
            OnPropertyChanged(nameof(End));
            OnPropertyChanged(nameof(AnalysisState));
            OnPropertyChanged(nameof(HasError));

        }
    }


    
}
