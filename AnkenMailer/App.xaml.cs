using Microsoft.Data.Sqlite;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace AnkenMailer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public SqliteConnection Connection { get; private set; }   

        public string DatabaseFilePath
        {
            get
            {
                return Path.Combine(
                           Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                           "AnkenMailer",
                           "data.db"
                        );
            }
        }

        public static App CurrentApp
        {
            get { return (App)App.Current; }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            //■データファイルのコピー
            if (!File.Exists(this.DatabaseFilePath))
            {
                string sourcePath = Path.Combine(AppContext.BaseDirectory, "data.db");
                File.Copy(sourcePath, this.DatabaseFilePath, true);
            }
            this.MainWindow = new MainWindow();
            //■DB接続
            try
            {
                this.OpenConnection();
            }
            catch (Exception ex)
            {
               
                string sourcePath = Path.Combine(AppContext.BaseDirectory, "data.db");
                File.Copy(sourcePath, this.DatabaseFilePath, true);
                this.OpenConnection();
                this.MainWindow.Loaded += (object sender, RoutedEventArgs e) =>
                {
                   
                    MessageBox.Show("データベースが壊れています。データベースを初期化しました。バックアップがある場合は、データベースをインポートしてください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                };
            }
 

            this.MainWindow.Show();

        }


        public void OpenConnection()
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = this.DatabaseFilePath
            };
            this.Connection = new SqliteConnection(connectionStringBuilder.ToString());
            this.Connection.Open();

            //■memdbにアタッチ
            using var command = this.Connection.CreateCommand();
            command.CommandText = "ATTACH DATABASE ':memory:' AS memdb;";
            command.ExecuteNonQuery();
        }
    }

}
