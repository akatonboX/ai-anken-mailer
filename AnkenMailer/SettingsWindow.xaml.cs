using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
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
    /// SettingsWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            var viewModel = new SettinsgWindowViewModel();
            viewModel.ImapServer = Properties.Settings.Default.ImapServer;
            viewModel.ImapPort = Properties.Settings.Default.ImapPort;
            viewModel.ImapUser = Properties.Settings.Default.ImapUser;
            viewModel.ImapPassword = Properties.Settings.Default.ImapPassword;

            viewModel.Endpoint = Properties.Settings.Default.Endpoint;
            viewModel.DeploymentName = Properties.Settings.Default.DeploymentName;
            viewModel.ApiKey = Properties.Settings.Default.ApiKey;
            viewModel.WebMailPath = Properties.Settings.Default.WebMailPath;
   
            this.DataContext = viewModel;
        }

        private SettinsgWindowViewModel ViewModel
        {
            get => (SettinsgWindowViewModel)this.DataContext;
            set
            {
                this.DataContext = value;
            }
        }

        private void acceptButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = (SettinsgWindowViewModel)this.DataContext;

            Properties.Settings.Default.ImapServer = viewModel.ImapServer;
            Properties.Settings.Default.ImapPort = viewModel.ImapPort;
            Properties.Settings.Default.ImapUser = viewModel.ImapUser;
            Properties.Settings.Default.ImapPassword = viewModel.ImapPassword;

            Properties.Settings.Default.Endpoint = viewModel.Endpoint;
            Properties.Settings.Default.DeploymentName = viewModel.DeploymentName;
            Properties.Settings.Default.ApiKey = viewModel.ApiKey;
            Properties.Settings.Default.WebMailPath = viewModel.WebMailPath;
            Properties.Settings.Default.Save();
            this.DialogResult = true;
            this.Close();

        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void copyButton_Click(object sender, RoutedEventArgs e)
        {
            var text = $"""
                ImapServer: {this.ViewModel.ImapServer}
                ImapPort: {this.ViewModel.ImapPort}
                ImapUser: {this.ViewModel.ImapUser}
                ImapPassword: {this.ViewModel.ImapPassword}
                Endpoint: {this.ViewModel.Endpoint}
                DeploymentName: {this.ViewModel.DeploymentName}
                ApiKey: {this.ViewModel.ApiKey}
                WebMailPath: {this.ViewModel.WebMailPath}
                """;
            Clipboard.SetText(text);
        }
    }
    public class SettinsgWindowViewModel : ObservableObject
    {
        private string imapServer = "";
        private int imapPort = 993;
        private string imapUser = "";
        private string imapPassword = "";
        private string endpoint = "";
        private string deploymentName = "";
        private string apiKey = "";
        private string webMailPath = "";

        public string ImapServer
        {
            get => this.imapServer;
            set => this.SetProperty(ref this.imapServer, value);
        }

        public int ImapPort
        {
            get => this.imapPort;
            set => this.SetProperty(ref this.imapPort, value);
        }
        public string ImapUser
        {
            get => this.imapUser;
            set => this.SetProperty(ref this.imapUser, value);
        }
        public string ImapPassword
        {
            get => this.imapPassword;
            set => this.SetProperty(ref this.imapPassword, value);
        }

        public string Endpoint
        {
            get => this.endpoint;
            set => this.SetProperty(ref this.endpoint, value);
        }

        public string DeploymentName
        {
            get => this.deploymentName;
            set => this.SetProperty(ref this.deploymentName, value);
        }
        public string ApiKey
        {
            get => this.apiKey;
            set => this.SetProperty(ref this.apiKey, value);
        }
        public string WebMailPath
        {
            get => this.webMailPath;
            set => this.SetProperty(ref this.webMailPath, value);
        }

    }


}
