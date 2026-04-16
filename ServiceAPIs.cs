using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Timers;
using System.Net.Http;
using System.IO.Compression;

namespace ServiceMonitor
{
    class ServiceAPIs
    {
        public static List<ServiceInfo> serviceList = new List<ServiceInfo>();
        MSSQL.DatabaseWrapper mssqldbWrapper = new MSSQL.DatabaseWrapper();
        MySQL.DatabaseWrapper mysqldbWrapper = new MySQL.DatabaseWrapper();
        public static bool isMSSQL = false;
        public static WebsocketHandlerService UI_handler;

        public static JArray tablewidget;

        /// <summary>
        /// Get list of services to be checked from file Resources/servicesInfo.json.
        /// And send details of it 
        /// </summary>
        public void GetServiceListfile()
        {
            try
            {
                tablewidget = JArray.Parse(File.ReadAllText("Resources/tablewidgetfields.json"));

                string serviceTxt = File.ReadAllText(@"Resources/servicesInfo.json");
                JObject serviceJson = JObject.Parse(serviceTxt);
                JArray jarr = JArray.Parse(serviceJson["services"].ToString());
                foreach (JObject serviceObj in jarr)
                {
                    ServiceInfo service = JsonConvert.DeserializeObject<ServiceInfo>(serviceObj.ToString());
                    service.SkipQueue = new Queue<ServiceInfo>();
                    serviceList.Add(service);
                    StartHealthCheck(service);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Get server file list error " + ex.Message);
            }
        }

        /// <summary>
        /// Get service list from serviceList variable 
        /// </summary>
        /// <returns></returns>
        public List<ServiceInfo> GetAllServicesInfo()
        {
            try
            {
                return serviceList;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Start a service by specifying its name
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public bool StartService(string serviceName)
        {
            try
            {

                //get service from name 
                ServiceInfo serviceInfo = serviceList.Find(m => m.name == serviceName);
                int index = serviceList.FindIndex(m => m.name == serviceName);

                //check if service already running 

                List<Process> processExist = GetServiceProcess(serviceInfo.appSearch);
                if (processExist != null && processExist.Count != 0)
                {
                    //service already running , so exit
                    return false;
                }
                using (Process process = new Process())
                {
                    try
                    {


                        process.StartInfo.FileName = serviceInfo.exeName;
                        process.StartInfo.Arguments = serviceInfo.args;
                        process.StartInfo.UseShellExecute = true;
                        process.StartInfo.WorkingDirectory = serviceInfo.folderPath;
                        process.StartInfo.CreateNoWindow = false;
                        //process.StartInfo.Verb = "runas";

                        process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                        //  process.StartInfo.RedirectStandardError = true;

                        //* Start process and handlers

                        process.Start();
                        Console.WriteLine("Process started " + serviceName);
                        serviceInfo.status = 1;
                        serviceList.RemoveAt(index);
                        serviceList.Add(serviceInfo);

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        return false;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Restart Service error " + serviceName + " " + ex.Message); return false;
            }
            return true;
        }

        /// <summary>
        /// Stop a service by specifying its name
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public bool StopService(string serviceName)
        {
            try
            {
                ServiceInfo serviceInfo = serviceList.Find(m => m.name == serviceName);
                int index = serviceList.FindIndex(m => m.name == serviceName);
                List<Process> process = GetServiceProcess(serviceInfo.appSearch);
                if (process != null)
                {


                    foreach (Process p in process)
                    {
                        p.Kill(true);
                        Console.WriteLine("Killed Application " + serviceName);
                    }
                    //   serviceInfo.status = 0;


                    return true;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Stop Service error " + serviceName + " " + ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Check if service is running or not, if functions return process , then its running 
        /// otherwise its not active
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public List<Process> GetServiceProcess(string serviceName)
        {
            try
            {
                Process[] prc = Process.GetProcesses();
                List<Process> process = prc.ToList();
                List<Process> app = process.FindAll(m => m.ProcessName == serviceName.Replace(".exe", ""));

                return app;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Restart service 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public bool RestartService(string serviceName)
        {
            Console.WriteLine("Restarting service " + serviceName);
            try
            {
                //check if service already running , stop it and rerun

                StopService(serviceName);

                using (Process process = new Process())
                {
                    try
                    {

                        //get service from name 
                        ServiceInfo serviceInfo = serviceList.Find(m => m.name == serviceName);
                        try
                        {
                            Thread.Sleep(serviceInfo.restartWaitTime);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }

                        process.StartInfo.FileName = serviceInfo.exeName;
                        process.StartInfo.Arguments = serviceInfo.args;
                        process.StartInfo.UseShellExecute = true;
                        process.StartInfo.WorkingDirectory = serviceInfo.folderPath;
                        process.StartInfo.CreateNoWindow = false;
                        process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                        //process.StartInfo.RedirectStandardError = true;

                        //* Start process and handlers
                        if(string.IsNullOrEmpty(serviceInfo.args))
                        {
                            try
                            {
                                Process.Start(process.StartInfo);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Arg Start error :"+ex.Message);
                            }
                        }
                        else
                        {
                            process.Start();
                        }
                       
                        Console.WriteLine("Process started " + serviceName);
                        //serviceList.Find(m => m.name == serviceName).status = 1;
                        //serviceInfo.stopReason = "Manual";
                        //serviceInfo.stopTime = DateTime.Now;
                        //serviceInfo.hbWsClient.reconnect = true;
                        if (serviceInfo.health_check_protocol == "WS")
                        {

                            serviceInfo.hbWsClient.ResetConnectUri(serviceInfo);
                        }


                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        return false;
                    }

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }


        /// <summary>
        /// To add the new service
        /// </summary>
        /// <param name="services"></param>
        public void AddingNewService(JObject service)
        {
            try
            {
                //getting the service details
                string serviceTxt = File.ReadAllText(@"Resources/servicesInfo.json");
                JObject serviceJson = JObject.Parse(serviceTxt);
                JArray jarr = JArray.Parse(serviceJson["services"].ToString());

                //foreach (JObject service in services)
                {
                    //getting the service details
                    //Serverinfo--for updating the server list
                    //newServerinfo--for updating the json file(all the params in the ServiceInfo
                    //is not required. So created a separate class
                    ServiceInfo Serverinfo = JsonConvert.DeserializeObject<ServiceInfo>(service.ToString());
                    NewServiceInfo newServerinfo = JsonConvert.DeserializeObject<NewServiceInfo>(service.ToString());
                    
                    //absolute folder path
                    string filePath = service["final_path"].ToString();
                   
                    //updating the values
                    newServerinfo.name = service["mats_name"].ToString();
                    //getting only the folder path
                    newServerinfo.folderPath = Path.GetDirectoryName(filePath).ToString();
                    newServerinfo.restartWaitTime = 100;
                    newServerinfo.health_check_protocol = "HB_SKIP";
                    newServerinfo.health_check_timeout = 20000;

                    //for adding to the server list
                    Serverinfo.name = service["mats_name"].ToString();
                    Serverinfo.folderPath = Path.GetDirectoryName(filePath).ToString();
                    Serverinfo.restartWaitTime = 100;
                    Serverinfo.health_check_protocol = "HB_SKIP";
                    Serverinfo.health_check_timeout = 20000;

                    //finding the .exe name
                    newServerinfo.exeName = Path.GetFileNameWithoutExtension(filePath.ToString());
                    newServerinfo.appSearch = Path.GetFileName(filePath.ToString());
                    Serverinfo.exeName = Path.GetFileNameWithoutExtension(filePath.ToString());
                    Serverinfo.appSearch = Path.GetFileName(filePath.ToString());


                    //adding in to the list and running the service
                    Serverinfo.SkipQueue = new Queue<ServiceInfo>();
                    serviceList.Add(Serverinfo);
                    StartHealthCheck(Serverinfo);


                    //updating the array
                    jarr.Add(JObject.Parse(JsonConvert.SerializeObject(newServerinfo)));
                }

                //updating the configuration file
                try
                {
                    serviceJson["services"] = jarr;
                    StreamWriter writer = new StreamWriter(@"Resources/servicesInfo.json");
                    writer.WriteLine(serviceJson);
                    writer.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception while adding the new service to the Json file : " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("AddingNewService : " + ex.Message);
            }
        }

        /// <summary>
        /// To delete the service from the list
        /// </summary>
        /// <param name="service"></param>
        public void DeleteService(JObject service)
        {
            try
            {
                //getting existing service details
                string serviceTxt = File.ReadAllText(@"Resources/servicesInfo.json");
                JObject serviceJson = JObject.Parse(serviceTxt);
                JArray jarr = JArray.Parse(serviceJson["services"].ToString());

                //iterating through each services to be deleted
                //foreach (JObject service in services)
                {
                    //getting the delete service info
                    ServiceInfo Serverinfo = ServiceAPIs.serviceList.Find(m => m.name == service["mats_name"].ToString());

                    
                    //1st stopping the service
                    StopService(Serverinfo.name.ToString());
                    //removing the service from the list
                    //serviceList.Remove(Serverinfo);
                     serviceList.RemoveAll(m => m.name == Serverinfo.name.ToString());
                    Serverinfo.hbSkipClient.Disconnect(Serverinfo);

                    //iterating through existing service info
                    int index = 0;
                    int count = 0;
                    foreach (JObject ExtService in jarr)
                    {
                        if (ExtService["name"].ToString().Equals(Serverinfo.name.ToString()))
                        {
                            index = count;
                        }
                        count++;
                    }
                    jarr[index].Remove();

                    //deleting the folder 
                    try
                    {
                        //creating the zip file
                        ZipFile.CreateFromDirectory(Serverinfo.folderPath.ToString(), Serverinfo.folderPath.ToString()+"-"+DateTime.Now.ToShortDateString()+".zip");
                        //deleting the folder
                        Directory.Delete(Serverinfo.folderPath.ToString(),true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Folder delete Error : " + ex.Message);
                    }
                }

                try
                {
                    //updating the configurations
                    serviceJson["services"] = jarr;
                    StreamWriter writer = new StreamWriter(@"Resources/servicesInfo.json");
                    writer.WriteLine(serviceJson);
                    writer.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception while deleting the service: " + ex.Message);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("DeleteService :" + ex.Message);
            }
        }


        /// <summary>
        /// Event to stop service 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void StopScheduledEventFunc(object sender, CustomEventArgs e)
        {
            if (!GlobalVar.reconnectReqFlag)
            {
                Console.WriteLine("Stop Schedule event " + e.ServiceDetails.name + " " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.ffff"));
                try
                {
                    ServiceInfo serviceDetails = new ServiceInfo();

                    serviceDetails.name = e.ServiceDetails.name;
                    serviceDetails.exeName = e.ServiceDetails.exeName;
                    serviceDetails.appSearch = e.ServiceDetails.appSearch;
                    serviceDetails.startTime = e.ServiceDetails.startTime;
                    serviceDetails.stopTime = DateTime.Now;
                    serviceDetails.stopReason = "Scheduled stop";
                    serviceDetails.status = 0;
                    serviceDetails.health_check_timeout = e.ServiceDetails.health_check_timeout;
                    e.ServiceDetails.SkipQueue.Enqueue(serviceDetails);
                    StopService(e.ServiceDetails.name);


                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

        }

        /// <summary>
        /// Event to start service 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void StartScheduledEventFunc(object sender, CustomEventArgs e)
        {

            if (!GlobalVar.reconnectReqFlag)
            {
                Console.WriteLine("Start Schedule event " + e.ServiceDetails.name + " " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.ffff"));
                try
                {
                    ServiceInfo serviceDetails = new ServiceInfo();

                    serviceDetails.name = e.ServiceDetails.name;
                    serviceDetails.exeName = e.ServiceDetails.exeName;
                    serviceDetails.appSearch = e.ServiceDetails.appSearch;
                    serviceDetails.startTime = e.ServiceDetails.startTime;
                    serviceDetails.stopTime = DateTime.Now;
                    serviceDetails.stopReason = "Scheduled start";
                    serviceDetails.status = 0;
                    serviceDetails.health_check_timeout = e.ServiceDetails.health_check_timeout;
                    e.ServiceDetails.SkipQueue.Enqueue(serviceDetails);
                    if (StartService(e.ServiceDetails.name))
                    {

                    }
                    else
                    {

                    }


                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

        }

        /// <summary>
        ///  Event to restart service from scheduler 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ScheduledEventFunc(object sender, CustomEventArgs e)
        {
            if (!GlobalVar.reconnectReqFlag)
            {
                Console.WriteLine("Schedule event " + e.ServiceDetails.name + " " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.ffff"));

                try
                {

                    ServiceInfo serviceDetails = new ServiceInfo();

                    serviceDetails.name = e.ServiceDetails.name;
                    serviceDetails.exeName = e.ServiceDetails.exeName;
                    serviceDetails.appSearch = e.ServiceDetails.appSearch;
                    serviceDetails.startTime = e.ServiceDetails.startTime;
                    serviceDetails.stopTime = DateTime.Now;
                    serviceDetails.stopReason = "Scheduled restart";
                    serviceDetails.status = 0;
                    serviceDetails.health_check_timeout = e.ServiceDetails.health_check_timeout;
                    e.ServiceDetails.SkipQueue.Enqueue(serviceDetails);
                    if (e.ServiceDetails.health_check_protocol == "HB_SKIP")
                    {
                        RestartService(e.ServiceDetails.name);
                        e.ServiceDetails.hbSkipClient.Disconnect(e.ServiceDetails);
                    }
                    else if (e.ServiceDetails.health_check_protocol == "WS")
                    {
                        e.ServiceDetails.hbWsClient.ResetConnectUri(e.ServiceDetails);
                    }
                    else if (e.ServiceDetails.health_check_protocol == "HTTP")
                    {
                        e.ServiceDetails.hbHttpClient.Disconnect(e.ServiceDetails);

                    }
                    else if (e.ServiceDetails.health_check_protocol == "TCP")
                    {
                        e.ServiceDetails.hbTcpClient.reconnect = true;
                    }


                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }
        }


        /// <summary>
        /// HB skip event 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="Service"></param>
        public void HBSkipEvent(object sender, ElapsedEventArgs e, ServiceInfo Service)
        {
            if (!GlobalVar.reconnectReqFlag)
            {
                Console.WriteLine("Hb Skip event  " + Service.name + " " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:sss"));
                try
                {
                    ServiceInfo serviceDetails;
                    List<Process> process = GetServiceProcess(Service.appSearch);
                    if (process != null && process.Count == 1)
                    {
                        //Console.WriteLine("Service running "+Service.name );
                        UpdateDBandSendUI(Service);
                    }
                    else
                    {
                        serviceDetails = new ServiceInfo();

                        serviceDetails.name = Service.name;
                        serviceDetails.exeName = Service.exeName;

                        serviceDetails.startTime = Service.startTime;
                        serviceDetails.stopTime = DateTime.Now;
                        serviceDetails.stopReason = "Service restart";
                        serviceDetails.status = 0;
                        serviceDetails.health_check_timeout = Service.health_check_timeout;
                        Service.SkipQueue.Enqueue(serviceDetails);
                        //Queue<ServiceInfo> value;
                        //skipQueueMap.TryGetValue(Service.name, out value);
                        //if (value != null)
                        //{
                        //    value.Enqueue(Service);
                        //    skipQueueMap[Service.name] = value;
                        //}
                        // UpdateDBandSendUI(Service);
                        Console.WriteLine("service restarted " + Service.name);
                        if (RestartService(Service.name))
                        {

                            Service.hbSkipClient.Disconnect(Service);
                            //Service.hbSkipClient.Disconnect(Service);
                            //Service.hbSchedule.Elapsed += (s1, e2) => HbEventFunc(s1, e2, Service);
                            //Service.hbSchedule.Start();
                            //Queue<ServiceInfo> value2;
                            //skipQueueMap.TryGetValue(Service.name, out value2);
                            //if (value2 != null)
                            //{
                            //    value2.Enqueue(Service);
                            //    skipQueueMap[Service.name] = value2;
                            //}
                            //UpdateDBandSendUI(Service);
                        }



                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// HB Send to other services for checking active status of service , if hb missed restarting service 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="Service"></param>
        public void HbEventFunc(object sender, ElapsedEventArgs e, ServiceInfo Service)
        {


            JObject hbJson = new JObject();
            JObject finalJson = new JObject();
            finalJson.Add("userName", "admin");
            finalJson.Add("pwd", "admin");
            finalJson.Add("request_api", Service.health_check_api);
            finalJson.Add("app_id", Service.name);
            hbJson.Add("requestType", "HB");
            hbJson.Add("name", Service.name);
            finalJson.Add("data", hbJson);
            Console.WriteLine("Hb Event " + Service.name + " " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.ffff"));
            try
            {
                Service.hbCount = 1;
                switch (Service.health_check_protocol)
                {
                    case "WS":
                        {
                            Service.hbWsClient.websocket.Send(finalJson.ToString());
                            Thread.Sleep(1000);
                        }
                        break;
                    case "HTTP":
                        {
                            HttpContent content = new StringContent(finalJson.ToString());
                            try
                            {
                                HttpResponseMessage responsePost = Service.hbHttpClient.httpclient.PostAsync(Service.health_check_api, content).Result;

                                if (responsePost.IsSuccessStatusCode)
                                {
                                    var output = responsePost.Content.ReadAsStringAsync().Result;
                                    if (output.Contains("HB"))
                                    {
                                        Service.hbCount++;
                                    }


                                }
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("HTTP post error");
                            }
                        }
                        break;
                    case "TCP":
                        {
                            Service.hbTcpClient.tcpsocket.Send(finalJson.ToString() + "\r\n");
                            Thread.Sleep(1000);
                        }
                        break;
                }


                if (Service.hbCount == 1)
                {
                    ServiceInfo serviceDetails = new ServiceInfo();

                    serviceDetails.name = Service.name;
                    serviceDetails.exeName = Service.exeName;
                    serviceDetails.appSearch = Service.appSearch;
                    serviceDetails.startTime = Service.startTime;
                    serviceDetails.stopTime = DateTime.Now;
                    serviceDetails.stopReason = "HB missed";
                    serviceDetails.status = 0;
                    serviceDetails.health_check_timeout = Service.health_check_timeout;
                    Service.SkipQueue.Enqueue(serviceDetails);

                    ////Service.health_check_timeout = Service.health_check_timeout;
                    //Service.SkipQueue.Enqueue(serviceDetails);




                    if (Service.health_check_protocol == "WS")
                    {
                        Service.hbWsClient.ResetConnectUri(Service);
                    }
                    else if (Service.health_check_protocol == "TCP")
                    {
                        Service.hbTcpClient.reconnect = true;

                    }
                    else if (Service.health_check_protocol == "HTTP")
                    {
                        Service.hbHttpClient.Disconnect(Service);
                    }

                }
                else
                {
                    UpdateDBandSendUI(Service);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }



        }

        /// <summary>
        /// connect to service to check hb
        /// </summary>
        /// <param name="serviceInfo"></param>
        private void StartHealthCheck(ServiceInfo serviceInfo)
        {
            try
            {
                switch (serviceInfo.health_check_protocol)
                {
                    case "WS":
                        {
                            WebsocketClient ws = new WebsocketClient(serviceInfo);
                        }
                        break;
                    case "HTTP":
                        {
                            HTTPClient http = new HTTPClient(serviceInfo);
                        }
                        break;
                    case "TCP":
                        {
                            TCPClient tcp = new TCPClient(serviceInfo);
                        }
                        break;
                    case "HB_SKIP":
                        {
                            HBSkipClient skiphb = new HBSkipClient(serviceInfo);
                        }
                        break;
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Update Connection log of service in database 
        /// </summary>
        /// <param name="service"></param>
        public void UpdateDBandSendUI(ServiceInfo service)
        {
            try
            {
                if (service.SkipQueue != null)
                {
                    while (service.SkipQueue.Count != 0)
                    {

                        Console.WriteLine("count {0}", service.SkipQueue.Count);

                        Console.WriteLine("dequeue {0}", service.SkipQueue.Count);

                        ServiceInfo serviceInfo = service.SkipQueue.Dequeue();
                        //skipQueueMap[service.name] = value;
                        if (serviceInfo.startTime != null)
                        {
                            Connection_log conn_log = new Connection_log();
                            conn_log.name = serviceInfo.name;
                            conn_log.start_time = serviceInfo.startTime;
                            conn_log.stop_time = serviceInfo.stopTime;
                            conn_log.stop_reason = serviceInfo.stopReason;
                            conn_log.current_status = serviceInfo.status;
                            if (serviceInfo.stopTime == null)
                            {
                                JObject errMsg = new JObject();
                                if (isMSSQL)
                                {
                                    errMsg = mssqldbWrapper.AddConnectionLog(conn_log);
                                }
                                else
                                {
                                    errMsg = mysqldbWrapper.AddConnectionLog(conn_log);

                                }
                                if (errMsg["err_msg"].ToString() != "Success")
                                {
                                    service.SkipQueue.Enqueue(serviceInfo);
                                    //skipQueueMap[service.name] = value;
                                    break;
                                }
                                //else
                                //{
                                //    Thread.Sleep(100);
                                //    JObject queryMsg = dbWrapper.UpdateLastDisconnectedTime(conn_log);
                                //    if (queryMsg["err_msg"].ToString() != "Success")
                                //    {
                                //        Console.WriteLine("Update disconnected time failed " + conn_log.name);
                                //    }
                                //    else
                                //    {
                                //        Console.WriteLine("Updated disconnected time " + conn_log.name);
                                //    }
                                //}
                            }
                            else
                            {
                                JObject errMsg = new JObject();
                                if (isMSSQL)
                                {
                                    errMsg = mssqldbWrapper.UpdateConnectionLog(conn_log);
                                }
                                else
                                {
                                    errMsg = mysqldbWrapper.UpdateConnectionLog(conn_log);

                                }
                                if (errMsg["err_msg"].ToString() != "Success")
                                {
                                    service.SkipQueue.Enqueue(serviceInfo);
                                    //skipQueueMap[service.name] = value;
                                    break;
                                }
                            }
                        }

                    }
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("Send DB error " + ex.Message);
            }

        }


        public void UpdateDB()
        {
            try
            {
                if (GlobalVar.database_conn_log_queue != null)
                {
                    while (GlobalVar.database_conn_log_queue.Count != 0)
                    {

                        ServiceInfo serviceInfo = GlobalVar.database_conn_log_queue.Dequeue();
                        //skipQueueMap[service.name] = value;
                        if (serviceInfo.startTime != null)
                        {

                            Connection_log conn_log = new Connection_log();
                            conn_log.name = serviceInfo.name;
                            conn_log.start_time = serviceInfo.startTime;
                            conn_log.stop_time = serviceInfo.stopTime;
                            conn_log.stop_reason = serviceInfo.stopReason;
                            conn_log.current_status = serviceInfo.status;
                            if (serviceInfo.stopTime == null)
                            {
                                JObject errMsg = new JObject();
                                if (isMSSQL)
                                {
                                    errMsg = mssqldbWrapper.AddConnectionLog(conn_log);
                                }
                                else
                                {
                                    errMsg = mysqldbWrapper.AddConnectionLog(conn_log);

                                }
                                if (errMsg["err_msg"].ToString() != "Success")
                                {
                                    GlobalVar.database_conn_log_queue.Enqueue(serviceInfo);
                                    //skipQueueMap[service.name] = value;
                                    break;
                                }
                                //else
                                //{
                                //    Thread.Sleep(100);
                                //    JObject queryMsg = dbWrapper.UpdateLastDisconnectedTime(conn_log);
                                //    if (queryMsg["err_msg"].ToString() != "Success")
                                //    {
                                //        Console.WriteLine("Update disconnected time failed " + conn_log.name);
                                //    }
                                //    else
                                //    {
                                //        Console.WriteLine("Updated disconnected time " + conn_log.name);
                                //    }
                                //}
                            }
                            else
                            {
                                JObject errMsg = new JObject();
                                if (isMSSQL)
                                {
                                    errMsg = mssqldbWrapper.UpdateConnectionLog(conn_log);
                                }
                                else
                                {
                                    errMsg = mysqldbWrapper.UpdateConnectionLog(conn_log);

                                }
                                if (errMsg["err_msg"].ToString() != "Success")
                                {
                                    GlobalVar.database_conn_log_queue.Enqueue(serviceInfo);
                                    //skipQueueMap[service.name] = value;
                                    break;
                                }
                            }
                        }

                    }
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("Send DB error " + ex.Message);
            }

        }

        /// <summary>
        /// External request sent to zookeeper to trigger action 
        /// </summary>
        /// <param name="actionMap"></param>
        /// <param name="serviceInfo"></param>
        public void ZookeeperAction(JObject actionMap, ServiceInfo serviceInfo)
        {
            try
            {
                // Check action key is present 
                if (actionMap.ContainsKey("action"))
                {
                    int delay = Convert.ToInt32(actionMap["actionBeforeDelay"].ToString());

                    // switch action => start /stop /restart
                    switch (actionMap["action"].ToString())
                    {

                        case "start":
                            {
                                Console.WriteLine("Start delay " + serviceInfo.name);
                                Thread.Sleep(delay);

                                // Update to ping log 

                                ServiceInfo serviceDetails = new ServiceInfo();

                                serviceDetails.name = serviceInfo.name;
                                serviceDetails.exeName = serviceInfo.exeName;
                                serviceDetails.appSearch = serviceInfo.appSearch;
                                serviceDetails.startTime = DateTime.Now;
                                serviceDetails.stopTime = null;
                                serviceDetails.stopReason = "";
                                serviceDetails.status = 1;
                                StartService(serviceDetails.name);
                                ServiceAPIs.serviceList.Find(s => s.name == serviceDetails.name).startTime = serviceDetails.startTime;
                                ServiceAPIs.serviceList.Find(s => s.name == serviceDetails.name).stopTime = serviceDetails.stopTime;
                                ServiceAPIs.serviceList.Find(s => s.name == serviceDetails.name).status = serviceDetails.status;
                                ServiceAPIs.serviceList.Find(s => s.name == serviceDetails.name).stopReason = serviceDetails.stopReason;
                                GlobalVar.database_conn_log_queue.Enqueue(serviceDetails);
                                UpdateDB();
                            }
                            break;
                        case "stop":
                            {
                                Console.WriteLine("Stop delay " + serviceInfo.name);
                                Thread.Sleep(delay);

                                ServiceInfo serviceDetails = new ServiceInfo();

                                serviceDetails.name = serviceInfo.name;
                                serviceDetails.exeName = serviceInfo.exeName;
                                serviceDetails.appSearch = serviceInfo.appSearch;
                                serviceDetails.startTime = serviceInfo.startTime;
                                serviceDetails.stopTime = DateTime.Now;
                                serviceDetails.stopReason = "Zookeeper stop";
                                serviceDetails.status = 0;
                                StopService(serviceDetails.name);
                                ServiceAPIs.serviceList.Find(s => s.name == serviceDetails.name).stopTime = serviceDetails.stopTime;
                                ServiceAPIs.serviceList.Find(s => s.name == serviceDetails.name).status = serviceDetails.status;
                                ServiceAPIs.serviceList.Find(s => s.name == serviceDetails.name).stopReason = serviceDetails.stopReason;
                                GlobalVar.database_conn_log_queue.Enqueue(serviceDetails);
                                UpdateDB();
                            }
                            break;
                        case "restart":
                            {
                                Console.WriteLine("Stop delay " + serviceInfo.name);
                                Thread.Sleep(delay);

                                ServiceInfo serviceDetails = new ServiceInfo();

                                serviceDetails.name = serviceInfo.name;
                                serviceDetails.exeName = serviceInfo.exeName;
                                serviceDetails.appSearch = serviceInfo.appSearch;
                                serviceDetails.startTime = serviceInfo.startTime;
                                serviceDetails.stopTime = DateTime.Now;
                                serviceDetails.stopReason = "Zookeeper restart";
                                serviceDetails.status = 0;
                                RestartService(serviceDetails.name);
                                ServiceAPIs.serviceList.Find(s => s.name == serviceDetails.name).stopTime = serviceDetails.stopTime;
                                ServiceAPIs.serviceList.Find(s => s.name == serviceDetails.name).status = serviceDetails.status;
                                ServiceAPIs.serviceList.Find(s => s.name == serviceDetails.name).stopReason = serviceDetails.stopReason;
                                GlobalVar.database_conn_log_queue.Enqueue(serviceDetails);
                                UpdateDB();
                            }
                            break;
                        default:
                            {
                                Console.WriteLine("action not defined");
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Zookeeper Action {0}", ex.Message);
            }
        }
    }

}
