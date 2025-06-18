using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AnkenMailer
{
    public class AnkenHeader : ObservableObject
    {
        private bool hasError = false;
        private string? errorMessage;
        private string? json;
        private string? ceateDateTime;

        public bool HasError
        {
            get => this.hasError;
            set => this.SetProperty(ref this.hasError, value);
        }

        public string? ErrorMessage
        {
            get => this.errorMessage;
            set => this.SetProperty(ref this.errorMessage, value);
        }
        public string? Json
        {
            get => this.json;
            set => this.SetProperty(ref this.json, value);
        }
        public string? CeateDateTime
        {
            get => this.ceateDateTime;
            set => this.SetProperty(ref this.ceateDateTime, value);
        }
    }
}
