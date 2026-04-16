using Network;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceMonitor
{
    public class TCPClient : ClientInterface<ServiceInfo>
    {
        string  SERVER_TERMINATOR = "\r\n";
        public Client tcpsocket;
        ServiceAPIs apis = new ServiceAPIs();

        CancellationTokenSource cancel_source_reconnect = new CancellationTokenSource();
        Task reconnectTask;

        CancellationTokenSource cancel_source_receive= new CancellationTokenSource();
        Task readData;


        
        public TCPClient(ServiceInfo serviceInfo)
        {
            try
            {


                tcpsocket = new Client(serviceInfo.health_check_ip, serviceInfo.health_check_ip, false, serviceInfo.current_system_ip);


                this.service = serviceInfo;
                reconnectTask = Task.Factory.StartNew(() => Reconnect(), cancel_source_reconnect.Token);

                serviceInfo.hbTcpClient = this;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        /// <summary>
        ///  TCP Client connects to server 
        /// </summary>
        /// <param name="serviceInfo"></param>
        public override void Connect(ServiceInfo serviceInfo)
        {
            try
            {
                serviceInfo.hbTcpClient.tcpsocket

                  = new Client(serviceInfo.health_check_ip, serviceInfo.health_check_port, false, serviceInfo.current_system_ip);

                if (serviceInfo.hbTcpClient.tcpsocket.CheckConnection())
                {

                    serviceInfo.hbTcpClient.reconnect = false;


                    //if (serviceInfo.hbTcpClient.cancel_source_receive == null)
                    //{
                    serviceInfo.hbTcpClient.cancel_source_receive = new CancellationTokenSource();

                //}
                    serviceInfo.hbTcpClient.readData = Task.Factory.StartNew(() => Receive(serviceInfo), serviceInfo.hbTcpClient.cancel_source_receive.Token);

                    //serviceInfo.hbTcpClient.readData.Start();
                    ServiceInfo serviceDetails = new ServiceInfo();
                    //JObject payload = new JObject();
                    //payload["app_id"] = serviceInfo.name;
                    //payload["auth_token"] = serviceInfo.name;
                    //payload["request_api"] = serviceInfo.health_check_api;
                    //serviceInfo.hbWsClient.websocket.Send(payload.ToString());
                    
                    

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
                else
                {
                    serviceInfo.hbTcpClient. reconnect = true;
                }

                // if connection is successful  start receive task 
               
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
            
        }


        /// <summary>
        ///  TCP client disconnects from server 
        /// </summary>
        /// <param name="serviceInfo"></param>
        public override void Disconnect(ServiceInfo serviceInfo)
        {
            try
            {
                if (serviceInfo.hbTcpClient.cancel_source_receive != null)
                {
                    serviceInfo.hbTcpClient.cancel_source_receive.Cancel();

                    
                }
                if (serviceInfo.hbSchedule != null)
                {
                    serviceInfo.hbSchedule.Stop();
                    serviceInfo.hbSchedule.Close();
                    serviceInfo.hbSchedule.Dispose();

                    serviceInfo.hbSchedule = null;

                }
                if (serviceInfo.hbTcpClient.reconnectCount == 1)
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
            

                if (serviceInfo.hbTcpClient != null)
                {
                    serviceInfo.hbTcpClient.tcpsocket.Close();
                }
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
        ///  TCP client reading data from tcp server 
        /// </summary>
        /// <param name="serviceInfo"></param>
        public  void Receive(ServiceInfo serviceInfo)
        {
            
            try
            {
               
                while (true)
                {
                    if (serviceInfo.hbTcpClient.cancel_source_receive.IsCancellationRequested)
                    {
                        serviceInfo.hbTcpClient.cancel_source_receive.Token.ThrowIfCancellationRequested();
                    }
                    string data = serviceInfo.hbTcpClient. tcpsocket.Receive(SERVER_TERMINATOR);
                    if (data != null && data != string.Empty)
                    {
                        string[] receivedData = Utility.jSonTokeniser(data);
                        for (int i = 0; i < receivedData.Length; i++)
                        {
                            if (receivedData[0].Contains("HB"))
                            {
                                serviceInfo.hbCount++;
                                Console.WriteLine("hb received ");
                            }
                            
                        }
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine("Cancelled task receive {0}", ex.Message);
            }
            finally
            {
                serviceInfo.hbTcpClient.cancel_source_receive.Dispose();
                serviceInfo.hbTcpClient.cancel_source_receive = null;

            }
        }
    }
}
