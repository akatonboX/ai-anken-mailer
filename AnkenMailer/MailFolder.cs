using CommunityToolkit.Mvvm.ComponentModel;
using MailKit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnkenMailer
{
    public class MailFolder : ObservableObject
    {
        private string fullName;
        private string name;
        private bool isExpanded = true;
        public MailFolder(IMailFolder folder)
        {
            this.fullName = folder.FullName;
            this.name = folder.Name;
            Children = new ObservableCollection<MailFolder>(from item in folder.GetSubfolders(false) select new MailFolder(item));
        }

        public string FullName
        {
            get => this.fullName;
            set => this.SetProperty(ref this.fullName, value);
        }

        public string Name
        {
            get => this.name;
            set => this.SetProperty(ref this.name, value);
        }

        public bool IsExpanded
        {
            get => this.isExpanded;
            set => this.SetProperty(ref this.isExpanded, value);
        }

        public ObservableCollection<MailFolder> Children { get; }

    }
}
