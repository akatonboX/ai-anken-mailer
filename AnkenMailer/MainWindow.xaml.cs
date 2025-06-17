using CommunityToolkit.Mvvm.ComponentModel;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using MimeKit;
using MimeKit.Utils;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Tls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using static AnkenMailer.ColumnFilterWindow;

namespace AnkenMailer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ConvertContentManager convertContentManager;
    
        public MainWindow()
        {
            this.convertContentManager = new ConvertContentManager();
            this.convertContentManager.Progress += ConvertContentManager_Progress;

            InitializeComponent();
            this.ViewModel = new MainWindowViewModel();

            //■解析の開始
            this.ViewModel.PropertyChanged += async (object? sender, PropertyChangedEventArgs e) =>
            {
                if(e.PropertyName == "MailItems")
                {
                    //■現在の解析をストップ
                    await this.convertContentManager.Cnacel();

                    //■新しく選択されたフォルダの解析をスタート
                    if(this.ViewModel.MailItems != null)
                        await this.convertContentManager.Convert(this.ViewModel.MailItems);
                }
            };

            this.LoadMailFolders();
        }



        private void ConvertContentManager_Progress(object? sender, Progress e)
        {
            if (e.Value != null)
                this.ViewModel.Progress = e.Value;
            this.ViewModel.StatusMessage = e.Label;
            this.ViewModel.StatusMessageDetail = e.Detail;
        }

        private MainWindowViewModel ViewModel
        {
            get => (MainWindowViewModel)this.DataContext;
            set => this.DataContext = value;
        }

        private void GeneralSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var window = new SettingsWindow();
            window.Owner = this;
            if (window.ShowDialog() == true)
            {
                this.LoadMailFolders();
            }
        }
        private void LoadMailFolders()
        {
            try
            {
                using (var client = new ImapClient())
                {
                    // ■接続（SSL有効）
                    client.Connect(Properties.Settings.Default.ImapServer, Properties.Settings.Default.ImapPort, SecureSocketOptions.SslOnConnect);
                    // ■認証
                    client.Authenticate(Properties.Settings.Default.ImapUser, Properties.Settings.Default.ImapPassword);

                    //■ルートフォルダから再帰的に一覧を取得
                    this.ViewModel.MailFolders = new ObservableCollection<MailFolder>(from item in client.PersonalNamespaces select new MailFolder(client.GetFolder(item)));
                }
            }
            catch(Exception exception)
            {
                MessageBox.Show($"エラーが発生しました。\r\n{exception.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            this.ViewModel.SelectedMailFolder = (MailFolder)e.NewValue;

        }



        private void detailButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this.ViewModel.StatusMessageDetail, "情報", MessageBoxButton.OK);
        }



        private void mailItemList_Sorting(object sender, DataGridSortingEventArgs e)
        {
            //■デフォルトのソート動作をキャンセル
            e.Handled = true;

            //■新しい該当のカラムのソート状態を決定
            var column = e.Column;
            ListSortDirection? newDirection = column.SortDirection == null ? ListSortDirection.Ascending : column.SortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : null;

            //■データグリッドにitemsがなければ終了
            var dataGrid = (DataGrid)sender;
            if (dataGrid.ItemsSource == null) return;


            //■CollectionViewSourceを取得
            var view = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);


            //■該当のカラムのソートがあれば、削除
            var existingSort = view.SortDescriptions.FirstOrDefault(item => item.PropertyName == column.SortMemberPath);
            if (existingSort != null)
            {
                view.SortDescriptions.Remove(existingSort);

            }

            //■ソートの追加
            if (newDirection != null)
            {
                view.SortDescriptions.Add(new SortDescription(column.SortMemberPath, (ListSortDirection)newDirection));
            }


            //■UIに反映
            foreach (var sortDirection in view.SortDescriptions)
            {
                var targetColumn = mailItemList.Columns.FirstOrDefault(item => item.SortMemberPath == sortDirection.PropertyName);
                if (targetColumn != null)
                {
                    targetColumn.SortDirection = sortDirection.Direction;
                }

            }
        }

        private void ColumnHeader_RightClick(object sender, MouseButtonEventArgs e)
        {
            //■データ収集
            var columnHeader = (DataGridColumnHeader)sender;
            var column = columnHeader.Column;
            var headerName = column.SortMemberPath;

            //■選択肢の生成
            var items = this.ViewModel.MailItems == null ? new List<object?>() 
                        : this.ViewModel.MailItems
                            .OfType<object>() // DataGridが内部で使うDataRowViewなどを避けるため
                            .Select(item =>
                            {
                                var prop = item.GetType().GetProperty(headerName);
                                return prop?.GetValue(item);
                            })
                            .OrderBy(v => v)
                            .Distinct()
                            .ToList();

            //■Windowの準備と表示
            var window = new ColumnFilterWindow(column.Header.ToString().Replace("*", ""), items, this.ViewModel.ColumnFilters[headerName]);
            if (window.ShowDialog() == true)
            {
                //■ViewModelに反映
                this.ViewModel.ColumnFilters[headerName] = window.GetResult();

                //■ヘッダの出力調整
                var header = column.Header.ToString();
                if (header.EndsWith("*") && this.ViewModel.ColumnFilters[headerName] == null)
                {
                    column.Header = header.Substring(0, header.Length - 1);
                }
                else if(!header.EndsWith("*") && this.ViewModel.ColumnFilters[headerName] != null)
                {
                    column.Header = header + "*";
                }


                //■フィルタの適用
                var viewSource = (CollectionViewSource)this.Resources["MailItemsCollectionViewSource"];
                viewSource.View.Refresh();
            }
        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            var item = e.Item as MailItem;
            if (item != null)
            {
                var clazz = item.GetType();



                foreach (var filterItem in this.ViewModel.ColumnFilters.Items)
                {
                    if(filterItem.Value != null)
                    {
                        var value = clazz.GetProperty(filterItem.Key)?.GetValue(item);
                        e.Accepted = filterItem.Value.Contains(value);

                    }
                    if(e.Accepted == false)
                    {
                        break;
                    }
                }
            }

        }


        private void SaveJsonButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var mailItem = (MailItem)button.DataContext;
            Anken? anken = null;
            try
            {
                anken = JsonSerializer.Deserialize<Anken>(this.JsonTextBox.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            }
            catch (JsonException exception)
            {
                MessageBox.Show("JSONの形式が不正です。→" + exception.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (anken == null)
            {
                MessageBox.Show("JSONの形式が不正です。変換結果がnullになりました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }




            //■DBに登録

            using var command = App.CurrentApp.Connection.CreateCommand();
            command.CommandText = """
                                    UPDATE [Anken]
                                    SET
                                        [Name]=@Name
                                        ,[Start]=@Start
                                        ,[End]=@End
                                        ,[StartYearMonth]=@StartYearMonth
                                        ,[Place]=@Place
                                        ,[Details]=@Details
                                        ,[MainSkill]=@MainSkill
                                        ,[RequiredSkills]=@RequiredSkills
                                        ,[DesirableSkills]=@DesirableSkills
                                        ,[MaxUnitPrice]=@MaxUnitPrice
                                        ,[MinUnitPrice]=@MinUnitPrice
                                        ,[Remarks]=@Remarks
                                    WHERE [EnvelopeId] = @EnvelopeId
                                    """;

            command.Parameters.AddWithValue("@EnvelopeId", mailItem.Id);
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
            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show("JSONの形式が不正です。登録できませんでした。→" + ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //■mailItemListに登録
           
            mailItem.Anken = anken;
        }

        private async void ReConvertButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var mailItem = (MailItem)button.DataContext;

            await this.convertContentManager.ReConvert(new List<MailItem>() { mailItem });
        }

        private void OpenWebMailButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var mailItem = (MailItem)button.DataContext;
            Process.Start(new ProcessStartInfo
            {
                FileName = $"{Properties.Settings.Default.WebMailPath}{this.ViewModel.SelectedMailFolder.FullName}/{mailItem.UId}",
                UseShellExecute = true
            });
        }

        private void DeleteMailButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var mailItem = (MailItem)button.DataContext;
            using (var client = IMap.Open())
            {

                var window = new SelectFolderWindow(client);
                if (window.ShowDialog() == true && window.Folder != null)
                {
                    if (window.Folder.FullName.Equals(mailItem.FolderPath))
                    {
                        MessageBox.Show("同じフォルダです。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    var srcFolder = client.GetFolder(mailItem.FolderPath);
                    srcFolder.Open(FolderAccess.ReadWrite);
                    srcFolder.MoveTo(mailItem.UId, window.Folder);
                    this.ViewModel.MailItems.Remove(mailItem);
                }
            }
        }

        private void MoveMailButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var mailItem = (MailItem)button.DataContext;
            if (MessageBox.Show("メールを削除しますか？", "質問", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                using (var client = IMap.Open())
                {

                    var destFolder = client.GetFolder("INBOX.Trash");
                    var srcFolder = client.GetFolder(mailItem.FolderPath);
                    srcFolder.Open(FolderAccess.ReadWrite);
                    srcFolder.MoveTo(mailItem.UId, destFolder);
                    this.ViewModel.MailItems.Remove(mailItem);
                }

            }
                
            
        }
        private void DeleteMailListButton_Click(object sender, RoutedEventArgs e)
        {
            var targets = this.mailItemList.Items.Cast<MailItem>().ToList();
            if (targets.Count == 0)
            {
                MessageBox.Show("対象がありません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("表示されているメールをすべて削除しますか？", "質問", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                using (var client = IMap.Open())
                {
                    var destFolder = client.GetFolder("INBOX.Trash");
                    var srcFolder = client.GetFolder(this.ViewModel.SelectedMailFolder.FullName);
                    srcFolder.Open(FolderAccess.ReadWrite);
                    foreach (var target in targets)
                    {
                        srcFolder.MoveTo(target.UId, destFolder);
                        this.ViewModel.MailItems.Remove(target);
                    }


                }
            }
          
        }
        private void MoveMailListButton_Click(object sender, RoutedEventArgs e)
        {

            var targets = this.mailItemList.Items.Cast<MailItem>().ToList();
            if(targets.Count == 0)
            {
                MessageBox.Show("対象がありません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var client = IMap.Open())
            {

                var window = new SelectFolderWindow(client);
                if (window.ShowDialog() == true && window.Folder != null)
                {
                    if (window.Folder.FullName.Equals(this.ViewModel.SelectedMailFolder.FullName))
                    {
                        MessageBox.Show("同じフォルダです。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    var srcFolder = client.GetFolder(this.ViewModel.SelectedMailFolder.FullName);
                    srcFolder.Open(FolderAccess.ReadWrite);
                    foreach (var target in targets)
                    {
                        srcFolder.MoveTo(target.UId, window.Folder);
                        this.ViewModel.MailItems.Remove(target);
                    }
                    
                    
                }
            }
        }

        private async void ReConvertListButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("表示しているすべてのメールの本文をすべて解析し直しますか？", "情報", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

            await this.convertContentManager.ReConvert(this.mailItemList.Items.Cast<MailItem>().ToList());
        }

        private void ExportDatabaseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "データベースをエクスポート",
                Filter = "SQLiteデータファイル (*.db)|*.db|すべてのファイル (*.*)|*.*",
                DefaultExt = ".db",
                FileName = "data.db"
            };
            if (dialog.ShowDialog() == true)
            {
                //■DBを切断
                App.CurrentApp.Connection.Close();
                try
                {
                    File.Copy(App.CurrentApp.DatabaseFilePath, dialog.FileName, true);
                }
                finally
                {
                    //■接続の復帰
                    App.CurrentApp.OpenConnection();
                }
                MessageBox.Show("データベースをエクスポートしました。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);

            }
            
        }

        private void ImportDatabaseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "データベースをエクスポート",
                Filter = "SQLiteデータファイル (*.db)|*.db|すべてのファイル (*.*)|*.*",
                DefaultExt = ".db",
                FileName = "data.db"
            };
            if (dialog.ShowDialog() == true)
            {
                //■テンポラリにバックアップ
                string tempPath = System.IO.Path.GetTempFileName();
                File.Copy(App.CurrentApp.DatabaseFilePath, tempPath, true);
                //■DBを切断
                App.CurrentApp.Connection.Close();
                try
                {
                    File.Copy(dialog.FileName, App.CurrentApp.DatabaseFilePath, true);
                }
                finally
                {
                    //■接続の復帰
                    try
                    {
                        App.CurrentApp.OpenConnection();
                        this.ViewModel.Reflesh();
                        MessageBox.Show("データベースをインポートしました。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        //■バックアップの復旧
                        File.Copy(tempPath, App.CurrentApp.DatabaseFilePath, true);
                        App.CurrentApp.OpenConnection();
                        MessageBox.Show("データベースをインポートできませんでした。→" + ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        File.Delete(tempPath);
                    }
                }

            }
        }

        private void CopyListButton_Click(object sender, RoutedEventArgs e)
        {
            this.mailItemList.SelectAllCells();
            ApplicationCommands.Copy.Execute(null, this.mailItemList);
            this.mailItemList.UnselectAllCells();

        }

        private void ClearFilterAndSortButton_Click(object sender, RoutedEventArgs e)
        {
            foreach(var column in this.mailItemList.Columns)
            {
                column.SortDirection = null;
                column.Header = column.Header.ToString().Replace("*", "");
            }
            this.ViewModel.ColumnFilters.Clear();
            //■フィルタの適用
            var viewSource = (CollectionViewSource)this.Resources["MailItemsCollectionViewSource"];
            viewSource.View.Refresh();
        }

        private void Totalization01MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var client = IMap.Open();
            var dialog = new MultiSelectFolderWindow(client);
            dialog.Owner = this;
            if(dialog.ShowDialog() == true)
            {
                var folders = dialog.Folders;
                var result = new List<Totalization01Data>();
                foreach(var folder in folders)
                {
                    folder.Open(FolderAccess.ReadOnly);
                    var mailItems = (from summary in folder.Fetch(0, -1, MessageSummaryItems.Envelope | MessageSummaryItems.UniqueId) select new MailItem(folder.FullName, summary.UniqueId, summary.Envelope)).ToList();
                    //this.convertContentManager.Convert()

                    foreach(var mailItem in mailItems)
                    {
                        //■Idの取得
                        {
                            using var command = App.CurrentApp.Connection.CreateCommand();
                            command.CommandText = "select [EnvelopeId] from [Envelope] where [MessageId]=@messageId and [From] = @from;";
                            command.Parameters.AddWithValue("@messageId", mailItem.Envelope.MessageId);
                            command.Parameters.AddWithValue("@from", mailItem.Envelope.From.ToString());
                            mailItem.Id = command.ExecuteScalar() as long?;
                        }

                        if(mailItem.Id != null)//DBになければ無視する
                        {

                        }
                    }

                }
            }
        }
        private class Totalization01Data
        {
            public string MainSkill { get; set; }
            public string Name { get; set; }
            public int Price { get; set; }

            public int Count { get; set; }
        }
    }

    public class MainWindowViewModel : ObservableObject
    {
        private ObservableCollection<MailFolder> mailFolders = new ObservableCollection<MailFolder>();
        private MailFolder? selectedMailFolder = null;
        private ObservableCollection<MailItem>? mailItems;
        private int? progress = null;
        private string? statusMessage = null;
        private string? statusMessageDetail = null;
        private ColumnFilters columnFilters = new ColumnFilters();

        public ObservableCollection<MailFolder> MailFolders
        {
            get => this.mailFolders;
            set
            {
                this.SetProperty(ref this.mailFolders, value);
            }
        }

        public MailFolder? SelectedMailFolder
        {
            get => this.selectedMailFolder;
            set
            {
                this.SetProperty(ref this.selectedMailFolder, value);
                if (value == null)
                {
                   this.mailItems = null;
                }

                try
                {
                    using (var client = new ImapClient())
                    {
                        // ■接続（SSL有効）
                        client.Connect(Properties.Settings.Default.ImapServer, Properties.Settings.Default.ImapPort, SecureSocketOptions.SslOnConnect);
                        // ■認証
                        client.Authenticate(Properties.Settings.Default.ImapUser, Properties.Settings.Default.ImapPassword);

                        //■ルートフォルダから再帰的に一覧を取得
                        var folder = client.GetFolder(((MailFolder)value).FullName);
                        folder.Open(FolderAccess.ReadOnly);
                        var result = (from summary in folder.Fetch(0, -1, MessageSummaryItems.Envelope | MessageSummaryItems.UniqueId) select new MailItem(folder.FullName, summary.UniqueId, summary.Envelope)).ToList();


                        this.mailItems = new ObservableCollection<MailItem>(result);
                    }
                }
                catch (Exception)
                {
                    this.mailItems = null;
                }
                this.OnPropertyChanged(nameof(MailItems));
            }
        }
        public IList<MailItem>? MailItems
        {
            get => this.mailItems;
        }

        public int? Progress
        {
            get => this.progress;
            set => this.SetProperty(ref this.progress, value);
        }
        public string? StatusMessage
        {
            get => this.statusMessage;
            set => this.SetProperty(ref this.statusMessage, value);
        }
        public string? StatusMessageDetail
        {
            get => this.statusMessageDetail;
            set => this.SetProperty(ref this.statusMessageDetail, value);
        }

        public ColumnFilters ColumnFilters
        {
            get => this.columnFilters;
            set => this.SetProperty(ref this.columnFilters, value);
        }

        public void Reflesh()
        {
            this.OnPropertyChanged(nameof(MailItems));
        }
    }


    public class MailItemToBodyConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var mailItem = value as MailItem;
            if (mailItem == null || mailItem.Message == null)
            {
                return "";
            }
            return mailItem.Message.Body;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
    public class AnkenToJsonConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var anken = value as Anken;
            if (anken == null)
            {
                return null;
            }


            var json = JsonSerializer.Serialize(anken, new JsonSerializerOptions
            {
                WriteIndented = true, // 整形出力
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) // Unicodeエンコードを防止（日本語などをそのまま出力）
            });
            return json;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }


}