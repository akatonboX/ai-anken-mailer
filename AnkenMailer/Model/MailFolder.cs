using CommunityToolkit.Mvvm.ComponentModel;
using MailKit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnkenMailer.Model
{
    public class MailFolder : ObservableObject
    {
        private string fullName;
        private string name;
        private bool isExpanded = true;
        public MailFolder(IMailFolder folder)
        {
            fullName = folder.FullName;
            name = folder.Name;
            Children = new ObservableCollection<MailFolder>(from item in folder.GetSubfolders(false) select new MailFolder(item));
        }

        public string FullName
        {
            get => fullName;
            set => SetProperty(ref fullName, value);
        }

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set => SetProperty(ref isExpanded, value);
        }

        public ObservableCollection<MailFolder> Children { get; }

    }
}
