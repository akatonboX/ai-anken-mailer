using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnkenMailer
{
    public class Progress
    {

        public Progress(int? value) { this.Value = value; }

        public Progress(string? label) { this.Label = label; }

        public Progress(string? label, string? detail) { this.Label = label; this.Detail = detail; }

        public Progress(int value, string? label) { this.Value = value; this.Label = label; }
        public int? Value {  get; private set; }
        public string? Label { get; private set; }

        public string? Detail{get; private set; }
    }
}
