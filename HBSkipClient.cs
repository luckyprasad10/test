using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceMonitor
{
    public class HBSkipClient
    {
        ServiceAPIs apis = new ServiceAPIs();


        public HBSkipClient(ServiceInfo service)
        {
            try
            {

                service.hbSkipClient = this;
                apis.RestartService(service.name);
                Connect(service);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        /// <summary>
        /// On connect starting hb skip event to add details it in queue and then update to
        /// db when service is running
        /// </summary>
        /// <param name="service"></param>
        public void Connect(ServiceInfo service)
        {
            try
            {
                ServiceInfo serviceDetails = new ServiceInfo();
                serviceDetails.name = service.name;
                serviceDetails.exeName = service.exeName;

                serviceDetails.startTime = DateTime.Now;
                serviceDetails.stopTime = null;
                serviceDetails.stopReason = string.Empty;
                serviceDetails.status = 1;
                serviceDetails.health_check_timeout = service.health_check_timeout;
                service.startTime = serviceDetails.startTime;
                service.stopTime = serviceDetails.stopTime;
                service.stopReason = serviceDetails.stopReason;
                service.status = serviceDetails.status;
                service.SkipQueue.Enqueue(serviceDetails);
                service.hbSchedule = new System.Timers.Timer(service.health_check_timeout);
                service.hbSchedule.Elapsed += (s1, e2) => apis.HBSkipEvent(s1, e2, service);
                service.hbSchedule.Start();
                apis.UpdateDBandSendUI(service);
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
        /// Disconnect close the timer and start is it again
        /// </summary>
        /// <param name="service"></param>
        public void Disconnect(ServiceInfo service)
        {
            try
            {
                if (service.hbSchedule != null)
                {
                    service.hbSchedule.Stop();
                    service.hbSchedule.Close();
                    service.hbSchedule.Dispose();

                    service.hbSchedule = null;

                }
                int index = ServiceAPIs.serviceList.FindIndex(m => m.name == service.name);
                ServiceAPIs.serviceList.RemoveAt(index);
                ServiceAPIs.serviceList.Add(service);
              
                Connect(service);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
