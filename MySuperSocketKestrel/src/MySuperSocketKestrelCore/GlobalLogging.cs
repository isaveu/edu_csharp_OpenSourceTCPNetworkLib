using System;
using System.Collections.Generic;
using System.Text;

namespace MySuperSocketKestrelCore
{
    static class GLogging
    {
        static Microsoft.Extensions.Logging.ILogger _Logger;

        public static Microsoft.Extensions.Logging.ILogger Logger( ) { return _Logger;  }
        
        public static void SetLogger(Microsoft.Extensions.Logging.ILogger logger)
        {
            _Logger = logger;
        }
    }
}
