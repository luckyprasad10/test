using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HTTPModule
{
    interface HTTPInterface
    {
         void GET(HttpListenerContext context);
         void POST(HttpListenerContext context);
         void PUT(HttpListenerContext context);
         void DELETE(HttpListenerContext context);

    }
}
