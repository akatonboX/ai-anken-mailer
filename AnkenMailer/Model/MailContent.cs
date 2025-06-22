using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnkenMailer.Model
{


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
