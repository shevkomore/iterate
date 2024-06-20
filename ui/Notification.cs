using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iterate.ui
{
    public class Notification
    {
        public string message { get; set; }
        public bool loading { get; set; }
        public Notification(string message, bool loading = false) { this.message = message; this.loading = loading; }
    }
}
