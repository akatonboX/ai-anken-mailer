using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AnkenMailer.ColumnFilterWindow;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AnkenMailer.Model
{
    public class AnkenHeader : ObservableObject
    {
        private bool hasError = false;
        private string? errorMessage;
        private string? json;
        private string? ceateDateTime;

        public bool HasError
        {
            get => hasError;
            set => SetProperty(ref hasError, value);
        }

        public string? ErrorMessage
        {
            get => errorMessage;
            set => SetProperty(ref errorMessage, value);
        }
        public string? Json
        {
            get => json;
            set => SetProperty(ref json, value);
        }
        public string? CeateDateTime
        {
            get => ceateDateTime;
            set => SetProperty(ref ceateDateTime, value);
        }

        public static AnkenHeader? Load(long id)
        {

            //■AnkenHeader取得
            using var command = App.CurrentApp.Connection.CreateCommand();
            command.CommandText = "select * from [AnkenHeader] where [EnvelopeId]=@envelopeId";
            command.Parameters.AddWithValue("@envelopeId", id);
            var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var temp = new AnkenHeader();
                temp.HasError = reader.GetBoolean("HasError");
                temp.ErrorMessage = reader.IsDBNull("ErrorMessage") ? null : reader.GetString("ErrorMessage");
                temp.Json = reader.IsDBNull("JSON") ? null : reader.GetString("JSON");
                temp.CeateDateTime = reader.IsDBNull("CeateDateTime") ? null : reader.GetString("CeateDateTime");
                return temp;
            }
            else
            {
                return null;
            }
        }
    }
}
