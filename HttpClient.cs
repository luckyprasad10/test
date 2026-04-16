using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using WebSocketSharp;
namespace ServiceMonitor
{

    public class HTTPClient 
    {

        public HttpClient httpclient;
         ServiceAPIs apis = new ServiceAPIs();
        
        public HTTPClient(ServiceInfo serviceInfo)
        {
            try
            {

                httpclient = new HttpClient();
                httpclient.BaseAddress = new Uri("http://"+serviceInfo.health_check_ip + ":" + serviceInfo.health_check_port);
                // Add an Accept header for JSON format.  
                httpclient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

               // this.service = serviceInfo;
               // reconnectTask = Task.Factory.StartNew(() => Reconnect(), cancel_source_reconnect.Token);
                serviceInfo.hbHttpClient = this;
                Connect(serviceInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
      
        public  void Connect(ServiceInfo serviceInfo)
        {

            try
            {
                ServiceInfo serviceDetails = new ServiceInfo();
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
                int index = ServiceAPIs.serviceList.FindIndex(m => m.name == serviceInfo.name);
                ServiceAPIs.serviceList.RemoveAt(index);
                ServiceAPIs.serviceList.Add(serviceInfo);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);


            }

        }

        public  void Disconnect(ServiceInfo serviceInfo)
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
                int index = ServiceAPIs.serviceList.FindIndex(m => m.name == serviceInfo.name);
                ServiceAPIs.serviceList.RemoveAt(index);
                ServiceAPIs.serviceList.Add(serviceInfo);
                apis.RestartService(serviceInfo.name);
                Connect(serviceInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }


    

    }
}
