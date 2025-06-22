using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
