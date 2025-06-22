using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static AnkenMailer.ColumnFilterWindow;

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


        public static IList<Anken> Load(long id)
        {
            var list = new List<Anken>();
            using (var command = App.CurrentApp.Connection.CreateCommand())
            {
                command.CommandText = "select * from [Anken] where [EnvelopeId]=@envelopeId";
                command.Parameters.AddWithValue("@envelopeId", id);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Anken temp = new Anken();
                        temp.Index = reader.GetInt32("Index");
                        temp.Name = reader.IsDBNull("Name") ? null : reader.GetString("Name");
                        temp.Start = reader.IsDBNull("Start") ? null : reader.GetString("Start");
                        temp.End = reader.IsDBNull("End") ? null : reader.GetString("End");
                        temp.StartYearMonth = reader.IsDBNull("StartYearMonth") ? null : reader.GetString("StartYearMonth");
                        temp.Place = reader.IsDBNull("Place") ? null : reader.GetString("Place");
                        temp.Details = reader.IsDBNull("Details") ? null : reader.GetString("Details");
                        temp.MainSkill = reader.IsDBNull("MainSkill") ? null : reader.GetString("MainSkill");
                        temp.RequiredSkills = reader.IsDBNull("RequiredSkills") ? [] : reader.GetString("RequiredSkills").Split(",");
                        temp.DesirableSkills = reader.IsDBNull("DesirableSkills") ? [] : reader.GetString("DesirableSkills").Split(",");
                        temp.MaxUnitPrice = reader.IsDBNull("MaxUnitPrice") ? null : reader.GetInt32("MaxUnitPrice");
                        temp.MinUnitPrice = reader.IsDBNull("MinUnitPrice") ? null : reader.GetInt32("MinUnitPrice");
                        temp.Remarks = reader.IsDBNull("Remarks") ? null : reader.GetString("Remarks");
                        list.Add(temp);
                    }
                }
            }
            return list;
        }

    }
}
