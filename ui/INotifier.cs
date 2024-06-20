using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iterate.ui
{
    public interface INotifier
    {
        void Notify(Notification notification);
    }
}
