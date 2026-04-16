using FeedBotV1._0;
using HTTPModule;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace ServiceMonitor
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                //reading file for http server config
                JObject setup = JObject.Parse(File.ReadAllText("Resources/setup.json"));
                JArray jarray = JArray.Parse(setup["uri"].ToString());
                List<string> uriList = setup["uri"].ToObject<List<string>>();
                ServiceAPIs.isMSSQL = Boolean.Parse(setup["isMSSQLDb"].ToString());

                // creating handling http client request class
                ServiceMonitor handler = new ServiceMonitor();
                List<ServiceInfo> services = handler.apis.GetAllServicesInfo();
                //Buffer bfData = new Buffer(GlobalVar.BufDepth)
                GlobalVar.evntBuffer = new BufferCustom(GlobalVar.BufferDepth);

                CustomScheduler startAppScheduler = new CustomScheduler();
                startAppScheduler.schedulerActionEvent += handler.apis.StartScheduledEventFunc;

                CustomScheduler stopAppScheduler = new CustomScheduler();
                stopAppScheduler.schedulerActionEvent += handler.apis.StopScheduledEventFunc;

                CustomScheduler restartAppScheduler = new CustomScheduler();
                restartAppScheduler.schedulerActionEvent += handler.apis.ScheduledEventFunc;

                foreach (ServiceInfo si in services)
                {
                    try
                    {

                        if (si.restartSchedule != null && si.restartSchedule.Count != 0)
                        {
                            // schedule to restart app
                            restartAppScheduler.schedule = si.restartSchedule;
                            restartAppScheduler.Schedule_Timer(si);
                        }

                        if (si.stopSchedule != null && si.stopSchedule.Count != 0)
                        {
                            // schedule to stop app
                            stopAppScheduler.schedule = si.stopSchedule;
                            stopAppScheduler.Schedule_Timer(si);
                        }

                        if (si.startSchedule != null && si.startSchedule.Count != 0)
                        {
                            // schedule to start app
                            startAppScheduler.schedule = si.startSchedule;
                            startAppScheduler.Schedule_Timer(si);
                        }
                       
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }


                // creating http server instances
                //HTTPServer httpServer = new HTTPServer(handler.GET, handler.POST, setup["ip_address"].ToString(), setup["port"].ToString(), uriList);
                //httpServer.Start();
                IPAddress ip = IPAddress.Parse(setup["ip_address"].ToString());
                int port = Convert.ToInt32(setup["port"]);
                // websocket server for service status check 
                //MyWebsocketServer hbserver = new MyWebsocketServer(ip, 9999);
                //hbserver.Start();
                string custom_restart = File.ReadAllText(@"Resources/custom_service_restart.json");
               
                JObject customRestartJson = JObject.Parse(custom_restart);

                GlobalVar.BufferDepth = Convert.ToInt32(customRestartJson["bufferDepth"].ToString());
               
                GlobalVar.reconnectFreqThresholdInSeconds = Convert.ToInt32(customRestartJson["reconnectFreqThresholdInSeconds"].ToString());
                GlobalVar.affordableThreshold = Convert.ToInt32(customRestartJson["affordableThreshold"].ToString());
                CustomServiceRequest request = new CustomServiceRequest(customRestartJson);
                HTTPServer httpServer = new HTTPServer(request.GET, request.POST, setup["ip_address"].ToString(), setup["port"].ToString(), uriList);
                httpServer.Start();

            }
            catch (Exception ex)
            {
                
                Console.WriteLine(ex.Message);
            }
            Console.ReadLine();
        }
    }
}
