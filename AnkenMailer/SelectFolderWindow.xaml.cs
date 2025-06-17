using CommunityToolkit.Mvvm.ComponentModel;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using System.Collections.ObjectModel;
using System.Windows;

namespace AnkenMailer
{
    /// <summary>
    /// MoveMailWindows.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectFolderWindow : Window
    {
        class MoveMailWindowViewModel : ObservableObject
        {
            private IList<FolderViewModel>? folders;



            public IList<FolderViewModel>? Folders
            {
                get => this.folders;
                set => this.SetProperty(ref this.folders, value);
            }


        }


        public class FolderViewModel
        {
            public FolderViewModel(IMailFolder folder)
            {
                Folder = folder;
                Name = folder.Name;
                Children = new ObservableCollection<FolderViewModel>(from item in folder.GetSubfolders(false) select new FolderViewModel(item));
            }

            public IMailFolder Folder { get; }

            public string Name { get; }

            public bool IsExpanded { get; set; } = true;
            public ObservableCollection<FolderViewModel> Children { get; }

        }

        public SelectFolderWindow(ImapClient client)
        {
            this.client = client;
            InitializeComponent();

            this.DataContext = new MoveMailWindowViewModel();

          
            // ルートフォルダから再帰的に一覧を取得
            var root = new FolderViewModel(client.GetFolder(client.PersonalNamespaces[0]));

            this.ViewModel.Folders = new ObservableCollection<FolderViewModel> { root };

        }


        private ImapClient client;
        public ImapClient Client
        {
            get => this.client;
        }

        private IMailFolder? folder = null;
        public IMailFolder? Folder
        {
            get => this.folder;
        }

        private MoveMailWindowViewModel ViewModel
        {
            get => this.DataContext as MoveMailWindowViewModel;
        }
        
        private void acceptButton_Click(object sender, RoutedEventArgs e)
        {
            var folder = (FolderViewModel)this.treeView.SelectedItem;
            if (folder == null) return;

            this.DialogResult = true;
            this.folder = folder.Folder;
            this.Close();

        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.folder = null;
            this.Close();
        }

        public void Dispose()
        {
            this.folder = null;
            this.client = null;
        }
    }
}
