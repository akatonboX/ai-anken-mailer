using CommunityToolkit.Mvvm.ComponentModel;
using MailKit;
using MimeKit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AnkenMailer.Model
{
    public class MailItem : ObservableObject
    {
        private long? id;
        private string folderPath;
        private UniqueId uid;
        
        private Envelope envelope;
        private MailMessage? message = null;
        private AnkenHeader? ankenHeader = null;
        private IList<Anken>? ankens = null;
       

        public long? Id
        {
            get => id;
            set => SetProperty(ref id, value);
        }

        public MailItem(string folderPath, UniqueId uid, Envelope envelope)
        {
            this.folderPath = folderPath;
            this.uid = uid;
            this.envelope = envelope;


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
        public int? AnkenCount => Ankens?.Count;
        public string? AnkenName => TopAnken?.Name;
        public string? MainSkill => TopAnken?.MainSkill;
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

        public bool? HasError => AnkenHeader?.HasError;



        public void RefreshView()
        {
            OnPropertyChanged(nameof(AnkenCount));
            OnPropertyChanged(nameof(AnkenName));
            OnPropertyChanged(nameof(MainSkill));
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
            OnPropertyChanged(nameof(HasError));

        }
    }


    
}
