using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnkenMailer
{
    public class Anken : ObservableObject
    {
        private int? index;
        private string? name;
        private string? start;
        private string? startYearMonth;
        private string? end;
        private string? place;
        private string? details;
        private string? mainSkill;
        private string[] requiredSkills = [];
        private string[] desirableSkills = [];
        private int? maxUnitPrice;
        private int? minUnitPrice;
        private string? remarks;
        public int? Index
        {
            get => this.index;
            set => this.SetProperty(ref this.index, value);
        }

        public string? Name
        {
            get => this.name;
            set => this.SetProperty(ref this.name, value);
        }
        public string? Start
        {
            get => this.start;
            set => this.SetProperty(ref this.start, value);
        }
        public string? StartYearMonth
        {
            get => this.startYearMonth;
            set => this.SetProperty(ref this.startYearMonth, value);
        }
        public string? End
        {
            get => this.end;
            set => this.SetProperty(ref this.end, value);
        }
        public string? Place
        {
            get => this.place;
            set => this.SetProperty(ref this.place, value);
        }
        public string? Details
        {
            get => this.details;
            set => this.SetProperty(ref this.details, value);
        }
        public string? MainSkill
        {
            get => this.mainSkill;
            set => this.SetProperty(ref this.mainSkill, value);
        }
        public string[] RequiredSkills
        {
            get => this.requiredSkills;
            set => this.SetProperty(ref this.requiredSkills, value);
        }
        public string[] DesirableSkills
        {
            get => this.desirableSkills;
            set => this.SetProperty(ref this.desirableSkills, value);
        }
        public int? MaxUnitPrice
        {
            get => this.maxUnitPrice;
            set => this.SetProperty(ref this.maxUnitPrice, value);
        }

        public int? MinUnitPrice
        {
            get => this.minUnitPrice;
            set => this.SetProperty(ref this.minUnitPrice, value);
        }
      
        public string? Remarks
        {
            get => this.remarks;
            set => this.SetProperty(ref this.remarks, value);
        }


    }
}
