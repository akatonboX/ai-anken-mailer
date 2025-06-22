using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnkenMailer.Model
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
            get => index;
            set => SetProperty(ref index, value);
        }

        public string? Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }
        public string? Start
        {
            get => start;
            set => SetProperty(ref start, value);
        }
        public string? StartYearMonth
        {
            get => startYearMonth;
            set => SetProperty(ref startYearMonth, value);
        }
        public string? End
        {
            get => end;
            set => SetProperty(ref end, value);
        }
        public string? Place
        {
            get => place;
            set => SetProperty(ref place, value);
        }
        public string? Details
        {
            get => details;
            set => SetProperty(ref details, value);
        }
        public string? MainSkill
        {
            get => mainSkill;
            set => SetProperty(ref mainSkill, value);
        }
        public string[] RequiredSkills
        {
            get => requiredSkills;
            set => SetProperty(ref requiredSkills, value);
        }
        public string[] DesirableSkills
        {
            get => desirableSkills;
            set => SetProperty(ref desirableSkills, value);
        }
        public int? MaxUnitPrice
        {
            get => maxUnitPrice;
            set => SetProperty(ref maxUnitPrice, value);
        }

        public int? MinUnitPrice
        {
            get => minUnitPrice;
            set => SetProperty(ref minUnitPrice, value);
        }
      
        public string? Remarks
        {
            get => remarks;
            set => SetProperty(ref remarks, value);
        }


    }
}
