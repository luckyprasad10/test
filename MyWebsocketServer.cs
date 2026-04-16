

using Newtonsoft.Json.Linq;
using ServiceMonitor;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Timers;
using WebSocketSharp;
using WebSocketSharp.Server;

public delegate void cbFncAcceptWebSocketHandler(WebsocketHandlerService clientHandler);
public delegate byte[] cbFncclientDataReceive(WebsocketHandlerService clientHandler);

namespace ServiceMonitor
{
    public class MyWebsocketServer : WebSocketBehavior
    {
        public bool IsErrorModeEnabled;
        cbFncAcceptWebSocketHandler acceptClientHandler;
        string url;
        WebSocketServer ws_server;
        readonly IPAddress WS_Server_ip;
        readonly int WS_server_Port;
        public MyWebsocketServer() { }

        public MyWebsocketServer(string server_url, cbFncAcceptWebSocketHandler socketAcceptFxn)
        {
            url = server_url;
            acceptClientHandler = socketAcceptFxn;
        }
        public MyWebsocketServer(IPAddress server_ip, int server_port, cbFncAcceptWebSocketHandler socketAcceptFxn)
        {
            WS_Server_ip = server_ip;
            acceptClientHandler = socketAcceptFxn;
            WS_server_Port = server_port;
        }
        public MyWebsocketServer(IPAddress server_ip, int server_port)
        {
            WS_Server_ip = server_ip;
           // acceptClientHandler = socketAcceptFxn;
            WS_server_Port = server_port;
        }

        //start websocket server 
        public void Start()
        {
            try
            {
                ws_server = new WebSocketServer(WS_Server_ip, WS_server_Port);
                //ws_server.AddWebSocketService<WebsocketHandler>("/", () => new WebsocketHandler(acceptClientHandler, Receive)); // add uri 's 
                ws_server.AddWebSocketService<WebsocketHandlerService>("/get-service-info", () => new WebsocketHandlerService()); // add uri 's 
                ws_server.Start();// start the websocket server 
            }
            catch (Exception ex)
            {
                if (IsErrorModeEnabled)
                    Console.WriteLine("ERROR :: Message => {0} , StackTrace => {1}", ex.Message, ex.StackTrace);
            }
        }

        //stop websocket server
        public void Stop()
        {
            try
            {
                ws_server.Stop();
            }
            catch (Exception ex)
            {
                if (IsErrorModeEnabled)
                    Console.WriteLine("ERROR :: Message => {0} , StackTrace => {1}", ex.Message, ex.StackTrace);
            }
        }

        //receive data from the client
        //public byte[] Receive(WebsocketHandler socketHandler)
        //{
        //    try
        //    {
        //        //ClientAppProtocol cl = AppProtocol.clientAppList.Find(item => (item.bench_type == "websocket" && item.socketHandler.ID == socketHandler.ID));
        //        //if (cl != null)
        //        //{
        //        //    if (cl.receiveQ.Count > 0)
        //        //    {
        //        //        return cl.receiveQ.Dequeue();
        //        //    }
        //        //    else
        //        //    {
        //        //        return null;
        //        //    }
        //        //}
        //        //return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        if (IsErrorModeEnabled)
        //            Console.WriteLine("ERROR :: Message => {0} , StackTrace => {1}", ex.Message, ex.StackTrace);
        //        return null;
        //    }

        //}

        //Send data to client 
        //public bool SendData(WebsocketHandler socketHandler, byte[] data)
        //{
        //    try
        //    {

        //        if (GlobalVar.IsDebugModeEnabled)
        //            Console.WriteLine("Sent data to client :{0}", ASCIIEncoding.ASCII.GetString(data));
        //        IWebSocketSession session;
        //        socketHandler.wbSessionManager.TryGetSession(socketHandler.ID, out session);
        //        if (session != null)
        //        {
        //            if (session.State == WebSocketState.Open)
        //            {
        //                string str = ASCIIEncoding.ASCII.GetString(data);
        //                //Console.WriteLine(str);
        //                socketHandler.wbSessionManager.SendTo(str, socketHandler.ID);
        //                return true;
        //            }
        //        }

        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        if (IsErrorModeEnabled)
        //            Console.WriteLine("ERROR :: Message => {0} , StackTrace => {1}", ex.Message, ex.StackTrace);
        //        return false;
        //    }
        //}

    } //class

    //public class WebsocketHandler : WebSocketBehavior
    //{
    //    public bool IsErrorModeEnabled;
    //    cbFncAcceptWebSocketHandler clientAcceptWebSocketHandler;
    //    cbFncclientDataReceive receiveData;
    //    public WebSocketSessionManager wbSessionManager;
    //    ServiceAPIs apis = new ServiceAPIs();
    //    public NameValueCollection QueryString;

    //    public WebsocketHandler(cbFncAcceptWebSocketHandler cl, cbFncclientDataReceive data)
    //    {
    //        clientAcceptWebSocketHandler = cl;
    //        receiveData = data;
    //    }

    //    //------S: When the client connects to the ws server -------//
    //    protected override void OnOpen()
    //    {
    //        try
    //        {
    //            QueryString = Context.QueryString;
    //            wbSessionManager = this.Sessions;
    //            clientAcceptWebSocketHandler(this);
    //        }
    //        catch (Exception ex)
    //        {
    //            if (IsErrorModeEnabled)
    //                Console.WriteLine("ERROR :: Message => {0} , StackTrace => {1}", ex.Message, ex.StackTrace);
    //        }
    //    }

    //    // event is triggered when client closes the connection
    //    protected override void OnClose(CloseEventArgs e)
    //    {

    //        try
    //        {

    //            ClientAppProtocol cl = AppProtocol.clientAppList.Find(item => (item.bench_type == "websocket" && item.socketHandler == this));
    //            if (cl != null)
    //            {
    //                cl.Disconnect();
    //            }


    //        }
    //        catch (Exception ex)
    //        {
    //            if (IsErrorModeEnabled)
    //                Console.WriteLine("ERROR :: Message => {0} , StackTrace => {1}", ex.Message, ex.StackTrace);
    //        }
    //    }

    //    // event is triggered when client sends message
    //    protected override void OnMessage(MessageEventArgs e)
    //    {
    //        try
    //        {

    //            ClientAppProtocol cl = AppProtocol.clientAppList.Find(item => (item.bench_type == "websocket" && item.socketHandler.ID == this.ID));
    //            if (cl != null)
    //            {
    //                if (GlobalVar.IsDebugModeEnabled)
    //                    Console.WriteLine("INFO received message from client {0}", e.Data);
    //                cl.receiveQ.Enqueue(ASCIIEncoding.ASCII.GetBytes(e.Data));
    //            }
    //            else
    //            {
    //                this.Context.WebSocket.Close();
    //            }

    //        }
    //        catch (Exception ex)
    //        {
    //            if (IsErrorModeEnabled)
    //                Console.WriteLine("ERROR :: Message => {0} , StackTrace => {1}", ex.Message, ex.StackTrace);
    //        }
    //    }
    //}

    public class WebsocketHandlerService : WebSocketBehavior
    {
        public bool IsErrorModeEnabled;
        cbFncAcceptWebSocketHandler clientAcceptWebSocketHandler;
        cbFncclientDataReceive receiveData;
        public WebSocketSessionManager wbSessionManager;

          SchedulerS scheduler = new SchedulerS();
         ServiceAPIs apis = new ServiceAPIs();

        public NameValueCollection QueryString;

        public WebsocketHandlerService()
        {
        }
        public WebsocketHandlerService(cbFncAcceptWebSocketHandler cl, cbFncclientDataReceive data)
        {
            clientAcceptWebSocketHandler = cl;
            receiveData = data;
        }

        //------S: When the client connects to the ws server -------//
        protected override void OnOpen()
        {
            try
            {
                ServiceAPIs.UI_handler = this;

                // send the latest update of service 

                //List<JObject> lists = apis();
                //this.Send(ASCIIEncoding.ASCII.GetBytes(lists.ToString()));
            }
            catch (Exception ex)
            {
                if (IsErrorModeEnabled)
                    Console.WriteLine("ERROR :: Message => {0} , StackTrace => {1}", ex.Message, ex.StackTrace);
            }
        }

        // event is triggered when client closes the connection
        protected override void OnClose(CloseEventArgs e)
        {

            try
            {

                foreach (ServiceInfo service in ServiceAPIs.serviceList)
                {
                    service.hbCount = 1;
                    service.hbSchedule.Stop();
                    service.hbSchedule.Dispose();
                }
            }
            catch (Exception ex)
            {
                if (IsErrorModeEnabled)
                    Console.WriteLine("ERROR :: Message => {0} , StackTrace => {1}", ex.Message, ex.StackTrace);
            }
        }

        // event is triggered when client sends message
        protected override void OnMessage(MessageEventArgs e)
        {
            try
            {
                Console.WriteLine("Message received " + e.Data);
                JObject message = JObject.Parse(e.Data);
                //get service from message
                //ServiceInfo s = ServiceAPIs.serviceList.Find(x => x.name == message["name"].ToString());
                //if (s != null)
                //{
                //    switch (message["requestType"].ToString()) {
                //        case "HB":
                //            {
                //                 s.hbCount++;
                //            }break;
                //        case "CONNECT":  
                //            {
                //                DateTime timer = DateTime.Now;
                //                s.hbWsClient = this.Context.WebSocket;
                //                s.hbSchedule = new Timer(10000);
                              
                //                s.hbSchedule.Elapsed += (s1, e2) => apis.HbEventFunc(s1, e2, s);
                //                s.hbSchedule.Start();


                //            }
                //            break;
                //     }
                //}
                    
                
            }
            catch (Exception ex)
            {
                if (IsErrorModeEnabled)
                    Console.WriteLine("ERROR :: Message => {0} , StackTrace => {1}", ex.Message, ex.StackTrace);
            }
        }
    }
}
