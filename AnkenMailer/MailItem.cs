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

namespace AnkenMailer
{
    public class MailItem : ObservableObject
    {
        private long? id;
        private string folderPath;
        private MailKit.UniqueId uid;
        
        private MailKit.Envelope envelope;
        private MailMessage? message = null;
        private Anken? anken = null;

        public long? Id
        {
            get => this.id;
            set => this.SetProperty(ref this.id, value);
        }

        public MailItem(string folderPath, MailKit.UniqueId uid, MailKit.Envelope envelope)
        {
            this.folderPath = folderPath;
            this.uid = uid;
            this.envelope = envelope;


        }


        public string FolderPath
        {
            get => this.folderPath;
            set => this.SetProperty(ref this.folderPath, value);
        }
        public MailKit.UniqueId UId
        {
            get => this.uid;
            set => this.SetProperty(ref this.uid, value);
        }

        public MailKit.Envelope Envelope
        {
            get => this.envelope;
            set{
                this.SetProperty(ref this.envelope, value);
                this.OnPropertyChanged(nameof(Subject));
                this.OnPropertyChanged(nameof(Sender));
                this.OnPropertyChanged(nameof(Date));
            }
        }

        public MailMessage? Message
        {
            get => this.message;
            set => this.SetProperty(ref this.message, value);
        }
        public Anken? Anken
        {
            get => this.anken;
            set
            {
                this.SetProperty(ref this.anken, value);
                this.OnPropertyChanged(nameof(AnkenName));
                this.OnPropertyChanged(nameof(MainSkill));
                this.OnPropertyChanged(nameof(StartYearMonth));
                this.OnPropertyChanged(nameof(Place));
                this.OnPropertyChanged(nameof(MaxUnitPrice));
                this.OnPropertyChanged(nameof(MinUnitPrice));
                this.OnPropertyChanged(nameof(Remarks));
                this.OnPropertyChanged(nameof(Details));
                this.OnPropertyChanged(nameof(RequiredSkills));
                this.OnPropertyChanged(nameof(DesirableSkills));
                this.OnPropertyChanged(nameof(Start));
                this.OnPropertyChanged(nameof(End));
            }
        }

        //ここからViewプロパティ。DataGridのため
        public string? Subject => this.Envelope.Subject;
        public string Sender => this.Envelope.Sender.ToString();
        public string? Date => this.Envelope.Date?.ToString("yyyy/MM/dd HH:mm:ss");
        public string? AnkenName => this.Anken?.Name;
        public string? MainSkill => this.Anken?.MainSkill;
        public string? StartYearMonth => this.Anken?.StartYearMonth;
        public string? Place => this.Anken?.Place;
        public int? MaxUnitPrice => this.Anken?.MaxUnitPrice;
        public int? MinUnitPrice => this.Anken?.MinUnitPrice;
        public string? Remarks => this.Anken?.Remarks;
        public string? Details => this.Anken?.Details;
        public string? RequiredSkills => this.Anken == null ? null : string.Join(",", this.Anken.RequiredSkills);
        public string? DesirableSkills => this.Anken == null ? null : string.Join(",", this.Anken.DesirableSkills);
        public string? Start => this.Anken?.Start;
        public string? End => this.Anken?.End;

    }

    public class MailMessage
    {
        public MailMessage(string? body) {
            this.Body = body;
        }

        public string? Body
        {
            private set;
            get;
        }
    }

    public class MailContent
    {
        private long id;
        private string? name;
        private string? start;
        private string? end;
        private string? namstartYearMonthe;
        private string? place;
        private string? details;
        private string mainSkill;
        private IList<string> requiredSkills = new List<string>();
        private IList<string> desirableSkills = new List<string>();
        private string? description;
        private string? maxUnitPrice;
        private string? minUnitPrice;
        private string? remarks;
        public MailContent(
            long id,
            string? name,
            string? start,
            string? end,
            string? namstartYearMonthe,
            string? place,
            string? details,
            string? mainSkill,
            string? desirableSkills,
            string? description,
            string? maxUnitPrice,
            string? minUnitPrice,
            string? remarks
        )
        {
            this.id = id;
            this.name = name;
            this.start = start;
            this.end = end;
            this.namstartYearMonthe = namstartYearMonthe;
            this.place = place;
            this.details = details;
            this.mainSkill = mainSkill;
            //this.desirableSkills = desirableSkills;
            this.description = description;
            this.maxUnitPrice = maxUnitPrice;
            this.minUnitPrice = minUnitPrice;
            this.remarks = remarks;

            

        }

        public string Body
        {
            private set;
            get;
        }
    }
    
}
