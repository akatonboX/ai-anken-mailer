using CommunityToolkit.Mvvm.ComponentModel;
using MailKit;
using MailKit.Net.Imap;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AnkenMailer
{
    /// <summary>
    /// MultiSelectFolderWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MultiSelectFolderWindow : Window
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

            public bool Selected { get; set; } = false;

            public ObservableCollection<FolderViewModel> Children { get; }

        }

        public MultiSelectFolderWindow(ImapClient client)
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

        private List<IMailFolder>? folders = null;
        public List<IMailFolder>? Folders
        {
            get => this.folders;
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

            
            this.folders = new List<IMailFolder>();
            foreach(var child in this.ViewModel.Folders)
            {
                this.GetSelectedFolders(child, this.folders);
            }
            
            this.Close();

        }

        private void GetSelectedFolders(FolderViewModel folder, List<IMailFolder> list)
        {
            if (folder.Selected)
            {
                list.Add(folder.Folder);
            }

            foreach (var child in folder.Children)
            {
                this.GetSelectedFolders(child, list);
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.folders = null;
            this.Close();
        }

        public void Dispose()
        {
            this.folders = null;
            this.client = null;
        }
    }
}
