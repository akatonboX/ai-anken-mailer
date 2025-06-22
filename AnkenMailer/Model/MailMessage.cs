using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnkenMailer.Model
{
    public class MailMessage
    {
        public MailMessage(string? body)
        {
            Body = body;
        }

        public string? Body
        {
            private set;
            get;
        }
    }
}
