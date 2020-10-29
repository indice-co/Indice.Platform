using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.Hosting.Tasks
{
    public enum QMessageStatus
    {
        New = 0,
        Dequeued = 1
    }

    public enum QTaskStatus
    {
        New = 0,
        Running = 1,
        Completed= 1
    }
}
