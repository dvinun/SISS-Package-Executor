using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SISSPackageExecutor
{
    public interface IOnScreenLogger
    {
       void Log(string message, LogType logType);
    }
}
