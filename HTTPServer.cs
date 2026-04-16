using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;

public delegate void GET(HttpListenerContext context);
public delegate void POST(HttpListenerContext context);
public delegate void PUT(HttpListenerContext context);
public delegate void DELETE(HttpListenerContext context);
namespace HTTPModule
{
    class HTTPServer
    {
        Thread requestThread;
        HttpListener httpListener;
        GET cbFxnGetData = null;
        POST cbFxnPostData = null;
        PUT cbFxnPutData = null;
        DELETE cbFxnDeleteData = null;
        string url;
        List<string> urls;
        string IP;
        string PORT;
        public HTTPServer(GET getFun , POST postFun , PUT putFunc, DELETE delFun, string IP, string port, string uri)
        {
            cbFxnGetData = getFun;
            cbFxnPostData = postFun;
            cbFxnPutData = putFunc;
            cbFxnDeleteData = delFun;

            url = "http://" + IP + ":" + port + uri;
        }

        public HTTPServer(GET getFun, POST postFun, string IP, string port, string uri)
        {
            cbFxnGetData = getFun;
            cbFxnPostData = postFun;
            url = "http://" + IP + ":" + port + uri;
        }

        public HTTPServer(GET getFun, POST postFun, string IPAddress, string port, List<string> uri)
        {
            cbFxnGetData = getFun;
            cbFxnPostData = postFun;

            IP = IPAddress;
            PORT = port;
            // url = "http://" + IP + ":" + port + uri;
            urls = uri;
        }


        public HTTPServer(GET getFun, string IP, string port, string uri)
        {
            cbFxnGetData = getFun;
            url = "http://" + IP + ":" + port + uri;
           
        }

        public HTTPServer(POST postFun, string IP, string port, string uri)
        {
           
            cbFxnPostData = postFun;
            url = "http://" + IP + ":" + port + uri;
        }

        /// <summary>
        /// Start HTTPServer request 
        /// </summary>
        public void Start() {
            try
            {
                httpListener = new HttpListener();
                foreach (string uri in urls)
                {
                    if (uri == "/")
                    {
                        httpListener.Prefixes.Add("http://" + IP + ":" + PORT + uri );
                    }
                    else
                    {
                        httpListener.Prefixes.Add("http://" + IP + ":" + PORT + uri + "/");

                    }

                    Console.WriteLine("http://" + IP + ":" + PORT + uri);
                }
                httpListener.Start();
                requestThread = new Thread(WaitForRequest);
                requestThread.Start();
                Console.WriteLine("Http server started ");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Start Exception {0}", ex.Message);
                if(ex.Message.Contains("conflicts"))
                {
                    Environment.Exit(1);
                }
            }
        }

        /// <summary>
        /// Stop HTTPServer server 
        /// </summary>
        public void Stop()
        {
            try
            {
                httpListener.Stop();
                requestThread.Abort();
            }
            catch (ThreadAbortException ex)
            {

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Stop Server {0}", ex.Message);
            }
        }

        /// <summary>
        /// Wait for the client request 
        /// </summary>
        private async void WaitForRequest()
        {
            while (true)
            {
                try
                {
                    HttpListenerContext context =await  httpListener.GetContextAsync();

                    // Check the connection is not of websocket
                    //if (!context.Request.IsWebSocketRequest)
                    {
                        //Handling CRUD
                        switch (context.Request.HttpMethod)
                        {
                            case "GET":
                                //Perform GET Operation
                                cbFxnGetData(context);
                                break;
                            case "POST":
                                //Perform POST Operation
                                //CREATE(context);
                                cbFxnPostData(context);
                                break;
                            case "PUT":
                                //Perform UPDATE Operation
                                cbFxnPutData(context);
                                break;
                            case "DELETE":
                                //Perform DELETE Operation
                                cbFxnDeleteData(context);
                                break;
                            default:
                                break;
                        }

                    }
                }
                catch (Exception ex) {
                    Console.WriteLine("Strat "+ex.Message);
                }
            }// continuos loop
        }//method :WaitForRequest
    }//class
}//namespace
