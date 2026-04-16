using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using WebSocketSharp;
namespace ServiceMonitor
{

    public class WebsocketClient : ClientInterface<ServiceInfo>
    {
        string connect_api = "/authMgr/connect/";
        public WebSocket websocket;
        ServiceAPIs apis = new ServiceAPIs();

        CancellationTokenSource cancel_source_reconnect = new CancellationTokenSource();
        Task reconnectTask;
        public WebsocketClient(ServiceInfo serviceInfo)
        {
            try
            {


                websocket = new WebSocket("ws://" + serviceInfo.health_check_ip + ":" + serviceInfo.health_check_port + connect_api);
                websocket.OnOpen += (sender, e1) => OnOpen(sender, e1, serviceInfo);
                websocket.OnClose += (sender, e1) => OnClose(sender, e1, serviceInfo);
                websocket.OnMessage += (sender, e1) => OnMessage(sender, e1, serviceInfo);
                websocket.OnError += (sender, e1) => OnError(sender, e1, serviceInfo);

                this.service = serviceInfo;
                reconnectTask = Task.Factory.StartNew(() => Reconnect(), cancel_source_reconnect.Token);
                serviceInfo.hbWsClient = this;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// On open of connection start the hb timer which continuous send hb 
        /// for checking health status of app 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnOpen(object sender, EventArgs e, ServiceInfo serviceInfo)
        {
            try
            {

                ServiceInfo serviceDetails = new ServiceInfo();
                JObject payload = new JObject();
                payload["app_id"] = serviceInfo.name;
                payload["auth_token"] = serviceInfo.name;
                payload["request_api"] = serviceInfo.health_check_api;
                serviceInfo.hbWsClient.websocket.Send(payload.ToString());
              
                serviceInfo.hbWsClient.reconnect = false;

                serviceDetails.name = serviceInfo.name;
                serviceDetails.exeName = serviceInfo.exeName;

                serviceDetails.startTime = DateTime.Now;
                serviceDetails.stopTime = null;
                serviceDetails.stopReason = string.Empty;
                serviceDetails.status = 1;

                serviceDetails.health_check_timeout = serviceInfo.health_check_timeout;
                serviceInfo.startTime = serviceDetails.startTime;
                serviceInfo.stopTime = serviceDetails.stopTime;
                serviceInfo.stopReason = serviceDetails.stopReason;
                serviceInfo.status = serviceDetails.status;
                serviceInfo.SkipQueue.Enqueue(serviceDetails);
                apis.UpdateDBandSendUI(serviceInfo);
               
                serviceInfo.hbSchedule = new System.Timers.Timer(serviceInfo.health_check_timeout);
                serviceInfo.hbSchedule.Elapsed += (s1, e2) => apis.HbEventFunc(s1, e2, serviceInfo);
                serviceInfo.hbSchedule.Start();
                int index = ServiceAPIs.serviceList.FindIndex(m => m.name == service.name);
                ServiceAPIs.serviceList.RemoveAt(index);
                ServiceAPIs.serviceList.Add(service);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        ///  On close of connection , hb timer 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnClose(object sender, CloseEventArgs e, ServiceInfo serviceInfo)
        {
            try
            {
               // Console.WriteLine("Connection Close ..");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// On receiving message from server , checking if it is having hb message 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="serviceInfo"></param>
        public void OnMessage(object sender, MessageEventArgs e, ServiceInfo serviceInfo)
        {
            try
            {
                JObject json = JObject.Parse(e.Data);
                if (json.ContainsKey("login") && json.ContainsKey("request_api"))
                {
                    ResetConnectUri(serviceInfo);

                }
                else if (e.Data.Contains("HB"))
                {
                    serviceInfo.hbCount++;


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        /// <summary>
        /// On error in connection 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="serviceInfo"></param>
        public void OnError(object sender, ErrorEventArgs e, ServiceInfo serviceInfo)
        {
            //try
            //{
            //    reconnect = true;
            //    //Disconnect(serviceInfo);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //}
        }


        /// <summary>
        /// Websocket client connect of specific service 
        /// </summary>
        /// <param name="serviceInfo"></param>
        public override void Connect(ServiceInfo serviceInfo)
        {

            try
            {
                //serviceInfo.hbWsClient.websocket = new WebSocket("ws://" + serviceInfo.health_check_ip + ":" + serviceInfo.health_check_port + connect_api);
                serviceInfo.hbWsClient.websocket.Connect();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);


            }

        }


        /// <summary>
        /// Disconnect from websocket client of specific service 
        /// </summary>
        /// <param name="serviceInfo"></param>
        public override void Disconnect(ServiceInfo serviceInfo)
        {
            try
            {
                if (serviceInfo.hbSchedule != null)
                {
                    serviceInfo.hbSchedule.Stop();
                    serviceInfo.hbSchedule.Close();
                    serviceInfo.hbSchedule.Dispose();

                    serviceInfo.hbSchedule = null;

                }
                if (serviceInfo.hbWsClient.reconnectCount == 1)
                {
                    if (serviceInfo.stopTime == null)
                    {
                        serviceInfo.stopTime = DateTime.Now;
                    }
                    if (serviceInfo.stopReason == string.Empty)
                    {
                        serviceInfo.stopReason = "HB missed";
                    }
                    serviceInfo.status = 0;
                    apis.UpdateDBandSendUI(serviceInfo);
                    apis.RestartService(serviceInfo.name);
                }
                //serviceInfo.hbCount = 1;

                if (serviceInfo.hbWsClient != null)
                {
                    serviceInfo.hbWsClient.websocket.Close();
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        public bool ResetConnectUri(ServiceInfo serviceInfo)
        {
            try
            {
                serviceInfo.hbWsClient.websocket = new WebSocket("ws://" + serviceInfo.health_check_ip + ":" + serviceInfo.health_check_port + connect_api);
                serviceInfo.hbWsClient.websocket.OnOpen += (sender, e1) => OnOpen(sender, e1, serviceInfo);
                serviceInfo.hbWsClient.websocket.OnClose += (sender, e1) => OnClose(sender, e1, serviceInfo);
                serviceInfo.hbWsClient.websocket.OnMessage += (sender, e1) => OnMessage(sender, e1, serviceInfo);
                serviceInfo.hbWsClient.websocket.OnError += (sender, e1) => OnError(sender, e1, serviceInfo);
                serviceInfo.hbWsClient.reconnect = true;
                int index = ServiceAPIs.serviceList.FindIndex(m => m.name == service.name);
                ServiceAPIs.serviceList.RemoveAt(index);
                ServiceAPIs.serviceList.Add(service);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }

    }


    //public class WebsocketClient
    //{
    //    int reconnectCount = 1;
    //    string connect_api = "/authMgr/connect/";
    //    public WebSocket websocket;
    //    ServiceAPIs apis = new ServiceAPIs();

    //    //CancellationTokenSource cancel_source_reconnect = new CancellationTokenSource();
    //    //Task reconnectTask;
    //    public WebsocketClient(ServiceInfo serviceInfo)
    //    {
    //        try
    //        {


    //            websocket = new WebSocket("ws://" + serviceInfo.health_check_ip + ":" + serviceInfo.health_check_port + connect_api);
    //            websocket.OnOpen += (sender, e1) => OnOpen(sender, e1, serviceInfo);
    //            websocket.OnClose += (sender, e1) => OnClose(sender, e1, serviceInfo);
    //            websocket.OnMessage += (sender, e1) => OnMessage(sender, e1, serviceInfo);
    //            websocket.OnError += (sender, e1) => OnError(sender, e1, serviceInfo);

    //            serviceInfo.hbSchedule = new System.Timers.Timer(serviceInfo.health_check_timeout);
    //            serviceInfo.hbSchedule.Elapsed += (s1, e2) => apis.HbEventFunc(s1, e2, serviceInfo);
    //            serviceInfo.hbSchedule.Start();
    //            serviceInfo.hbWsClient = this;
    //           // Disconnect(serviceInfo);
               
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine(ex.Message);
    //        }
    //    }

    //    /// <summary>
    //    /// On open of connection start the hb timer which continuous send hb 
    //    /// for checking health status of app 
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    public void OnOpen(object sender, EventArgs e, ServiceInfo serviceInfo)
    //    {
    //        try
    //        {
    //            if (serviceInfo.hbSchedule != null)
    //            {
    //                try
    //                {
    //                    serviceInfo.hbSchedule.Stop();
    //                    serviceInfo.hbSchedule.Close();
    //                    serviceInfo.hbSchedule.Dispose();
    //                }
    //                catch (Exception ex)
    //                {
    //                    Console.WriteLine(ex.Message);
    //                }


    //                //serviceInfo.hbSchedule = null;

    //            }

    //            ServiceInfo serviceDetails = new ServiceInfo();
                
    //            JObject payload = new JObject();
    //            payload["app_id"] = serviceInfo.name;
    //            payload["auth_token"] = serviceInfo.name;
    //            payload["request_api"] = serviceInfo.health_check_api;
    //            serviceInfo.hbWsClient.websocket.Send(payload.ToString());
    //            Console.WriteLine("Connection Opened ..");
                

               
    //            serviceDetails.name = serviceInfo.name;
    //            serviceDetails.exeName = serviceInfo.exeName;
              
    //            serviceDetails.startTime = DateTime.Now;
    //            serviceDetails.stopTime = null;
    //            serviceDetails.stopReason = string.Empty;
    //            serviceDetails.status = 1;
               
    //            serviceDetails.health_check_timeout = serviceInfo.health_check_timeout;
    //            serviceInfo.startTime = serviceDetails.startTime;
    //            serviceInfo.stopTime = serviceDetails.stopTime;
    //            serviceInfo.stopReason = serviceDetails.stopReason;
    //            serviceInfo.status = serviceDetails.status;
    //            serviceInfo.SkipQueue.Enqueue(serviceDetails);
    //            apis.UpdateDBandSendUI(serviceInfo);
    //            serviceInfo.hbSchedule = new System.Timers.Timer(serviceInfo.health_check_timeout);
    //            serviceInfo.hbSchedule.Elapsed += (s1, e2) => apis.HbEventFunc(s1, e2, serviceInfo);
    //            serviceInfo.hbSchedule.Start();
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine(ex.Message);
    //        }
    //    }

    //    /// <summary>
    //    ///  On close of connection , hb timer 
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    public void OnClose(object sender, CloseEventArgs e, ServiceInfo serviceInfo)
    //    {
    //        try
    //        {
    //            Console.WriteLine("Connection Close ..");
    //            //if (e.Code == 10061)
    //            //{
    //            //    Console.WriteLine("Server closed");
    //            //    Disconnect(serviceInfo);

    //            //}
                
    //            //    serviceInfo.hbWsClient.reconnectCount++;
    //           //Disconnect(serviceInfo);
    //            //ResetConnectUri(serviceInfo);
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine(ex.Message);
    //        }
    //    }

    //    /// <summary>
    //    /// On receiving message from server , checking if it is having hb message 
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    /// <param name="serviceInfo"></param>
    //    public void OnMessage(object sender, MessageEventArgs e, ServiceInfo serviceInfo)
    //    {
    //        try
    //        {
    //            JObject json = JObject.Parse(e.Data);
    //            if (json.ContainsKey("login") && json.ContainsKey("request_api"))
    //            {
    //                ResetConnectUri(serviceInfo);

    //            }
    //            else if (e.Data.Contains("HB"))
    //            {
    //                serviceInfo.hbCount++;


    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine(ex.Message);
    //        }
    //    }


    //    /// <summary>
    //    /// On error in connection 
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    /// <param name="serviceInfo"></param>
    //    public void OnError(object sender, ErrorEventArgs e, ServiceInfo serviceInfo)
    //    {
    //        try
    //        {
               
              
    //          // if( e.Exception.Data["ErrorCode"] )
    //            {
    //              //  Console.WriteLine("Server not running");
    //               // Disconnect(serviceInfo);
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine(ex.Message);
    //        }
    //    }


    //    /// <summary>
    //    /// Websocket client connect of specific service 
    //    /// </summary>
    //    /// <param name="serviceInfo"></param>
    //    public void Connect(ServiceInfo serviceInfo)
    //    {

    //        try
    //        {
    //            // serviceInfo.hbWsClient.reconnectCount++;
    //            //serviceInfo.hbWsClient.websocket = new WebSocket("ws://" + serviceInfo.health_check_ip + ":" + serviceInfo.health_check_port + connect_api);
    //            serviceInfo.hbWsClient.websocket.Connect();

    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine(ex.Message);


    //        }

    //    }


    //    /// <summary>
    //    /// Disconnect from websocket client of specific service 
    //    /// </summary>
    //    /// <param name="serviceInfo"></param>
    //    public bool Disconnect(ServiceInfo serviceInfo)
    //    {
    //        try
    //        {
    //            if (serviceInfo.hbSchedule != null)
    //            {
    //                try
    //                {
    //                    serviceInfo.hbSchedule.Stop();
    //                    serviceInfo.hbSchedule.Close();
    //                    serviceInfo.hbSchedule.Dispose();
    //                }
    //                catch (Exception ex)
    //                {
    //                    Console.WriteLine(ex.Message);
    //                }
                    

    //                //serviceInfo.hbSchedule = null;

    //            }


    //            if (serviceInfo.stopTime == null)
    //            {
    //                serviceInfo.stopTime = DateTime.Now;
    //            }
    //            if (serviceInfo.stopReason == string.Empty)
    //            {
    //                serviceInfo.stopReason = "HB missed";
    //            }
    //            serviceInfo.status = 0;
    //            apis.UpdateDBandSendUI(serviceInfo);
    //            return apis.RestartService(serviceInfo.name); //serviceInfo.hbWsClient.reconnectCount = 0;




    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine(ex.Message);
    //        }
    //        return false;
    //    }

    //    public void ResetConnectUri(ServiceInfo serviceInfo)
    //    {
    //        try
    //        {
    //            Console.WriteLine("reset connect uri ");
    //            serviceInfo.hbWsClient.websocket = new WebSocket("ws://" + serviceInfo.health_check_ip + ":" + serviceInfo.health_check_port + connect_api);
    //            serviceInfo.hbWsClient.websocket.OnOpen += (sender, e1) => OnOpen(sender, e1, serviceInfo);
    //            serviceInfo.hbWsClient.websocket.OnClose += (sender, e1) => OnClose(sender, e1, serviceInfo);
    //            serviceInfo.hbWsClient.websocket.OnMessage += (sender, e1) => OnMessage(sender, e1, serviceInfo);
    //            serviceInfo.hbWsClient.websocket.OnError += (sender, e1) => OnError(sender, e1, serviceInfo);
    //            serviceInfo.hbSchedule = new System.Timers.Timer(serviceInfo.health_check_timeout);
    //            serviceInfo.hbSchedule.Elapsed += (s1, e2) => apis.HbEventFunc(s1, e2, serviceInfo);
    //            serviceInfo.hbSchedule.Start();
    //            Connect(serviceInfo);
                
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine(ex.Message);
    //        }
    //    }

    //}
}
