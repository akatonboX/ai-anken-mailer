using AnkenMailer.Model;
using MailKit;
using Microsoft.Data.Sqlite;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static AnkenMailer.ColumnFilterWindow;

namespace AnkenMailer
{
    public class TotalizationTargetTempTable : IDisposable
    {
        public SqliteConnection Connection { get; private set; }
        public string TempTableName { get; private set; }
        public TotalizationTargetTempTable(SqliteConnection connection, IList<IMailFolder> folders)
        {
            this.initialize(connection, folders);

        }

        private void initialize(SqliteConnection connection, IList<IMailFolder>? folders)
        {
            //■初期化
            this.Connection = connection;
            this.TempTableName = Guid.NewGuid().ToString();


            //■テンポラリテーブルの作成
            using (var command = this.Connection.CreateCommand())
            {
                command.CommandText = $"""
                    
                    CREATE TABLE memdb.[{this.TempTableName}] (
                        [EnvelopeId] INTEGER NOT NULL
                        , [Folder] TEXT NOT NULL
                        , CONSTRAINT [{this.TempTableName}] PRIMARY KEY ([EnvelopeId])
                    );
                    """;
                command.ExecuteNonQuery();                
            }

            //■データの投入

            if (folders != null)
            {
                //■SQLコマンドの初期化
                using (var command = this.Connection.CreateCommand())
                {
                    command.CommandText = $"""
                    INSERT INTO  memdb.[{this.TempTableName}] (
                        [EnvelopeId]
                        ,[Folder]
                    )
                    SELECT 
                        [EnvelopeId] 
                        ,@Folder 
                    from [Envelope] 
                    WHERE 
                        [MessageId] = @MessageId 
                        and [From] = @From
                        and not exists (select * from memdb.[{this.TempTableName}] where [EnvelopeId] = Envelope.EnvelopeId);
                    """;
                    command.Parameters.Add("@MessageId", SqliteType.Text);
                    command.Parameters.Add("@From", SqliteType.Text);
                    command.Parameters.Add("@Folder", SqliteType.Text);

                    foreach (var folder in folders)
                    {
                        folder.Open(FolderAccess.ReadOnly);
                        var mailItems = (from summary in folder.Fetch(0, -1, MessageSummaryItems.Envelope | MessageSummaryItems.UniqueId) select new MailItem(folder.FullName, summary.UniqueId, summary.Envelope)).ToList();
                        foreach (var mailItem in mailItems)
                        {
                            command.Parameters["@MessageId"].Value = mailItem.Envelope.MessageId;
                            command.Parameters["@From"].Value = mailItem.Envelope.From.ToString();
                            command.Parameters["@Folder"].Value = mailItem.FolderPath;
                            command.ExecuteNonQuery();
                        }

                        
                    }
                }
            }
           

        }

        public void Dispose()
        {
            using (var command = this.Connection.CreateCommand())
            {
                command.CommandText = $" DROP TABLE IF EXISTS [{this.TempTableName}];";
                command.ExecuteNonQuery();
            }
            this.Connection = null;
        }
    }
}
