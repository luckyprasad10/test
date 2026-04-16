using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using FeedBotV1._0;
using HTTPModule;
using Newtonsoft.Json.Linq;

namespace ServiceMonitor
{
    class CustomServiceRequest : HTTPInterface
    {
        public ServiceAPIs apis = new ServiceAPIs();

        JObject customRestartJson;
        public CustomServiceRequest(JObject restartJson)
        {
            customRestartJson = restartJson;
            //JArray jarr = JArray.Parse(restartJson["services"].ToString());
        }
        public void DELETE(HttpListenerContext context)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// GET request from http client 
        /// </summary>
        /// <param name="context"></param>
        public void GET(System.Net.HttpListenerContext context)
        {
            string url = context.Request.Url.LocalPath;
            string getResponse = string.Empty;
        }


        /// <summary>
        /// POST request from http client 
        /// </summary>
        /// <param name="context"></param>
        public void POST(System.Net.HttpListenerContext context)
        {
            string url = context.Request.Url.LocalPath;

            string payload;
            // read the payloads
            using (var reader = new StreamReader(context.Request.InputStream,
                                                 context.Request.ContentEncoding))
            {
                payload = reader.ReadToEnd();
            }
            Stream output = null;
            JObject postResjson = new JObject();

            String postResponse = string.Empty;
            //convert payload to json 
            JObject jsonPayload = JObject.Parse(payload);
            try
            {

                switch (url)
                {
                    // download - in uri list 0
                    case "/reconnect-service/":
                        {

                            List<ServiceInfo> lists = apis.GetAllServicesInfo();

                            //get the service
                            if (customRestartJson.ContainsKey("/reconnect-service"))
                            {
                                Console.WriteLine("Reconnect request received ");
                                GlobalVar.reconnectReqFlag = true;
                                evtInfo bufferData = new evtInfo();
                                bufferData.log_date_time = DateTime.Now;
                                if (jsonPayload.ContainsKey("machine_ip"))
                                {
                                    bufferData.ip = jsonPayload["machine_ip"].ToString();
                                    bufferData.name = jsonPayload["machine_name"].ToString();
                                }

                                // Add to buffer
                                GlobalVar.evntBuffer.BufferOpenClose(true);
                                // check the queue for reconnect 
                                GlobalVar.evntBuffer.FillBuffer(bufferData);
                                Console.WriteLine("Buffer added for  " + jsonPayload["machine_name"].ToString() + " " + bufferData.log_date_time);
                                postResjson["status"] = "Success";
                                postResjson["message"] = "Data added to buffer";
                                postResjson["machine_name"] = jsonPayload["machine_name"].ToString();
                                postResjson["machine_ip"] = jsonPayload["machine_ip"].ToString();
                                JObject
                                   actionOrder = JObject.Parse(customRestartJson["/reconnect-service"].ToString());
                                int actionOrderCount = actionOrder.Count;

                                //Get last n records based on configuration
                                /*-------------------Exp-------------------------
                                 * If record 1 is inserted at 1 pm and record 2 pm is inserted at 2 then in list
                                 * Recrod 2 - at 2 pm will be at 0th index and record 1  at 1 pmwill be at index 1 
                                 * _________________________________________________________________________________
                                 */

                                //List<evtInfo> latestnRecords = GlobalVar.evntBuffer.GetAllInDtBuffer().OrderByDescending(s => s.log_date_time).Distinct().Take(GlobalVar.lastnRecordcnt).ToList();
                                var latestnRecords = (from c in GlobalVar.evntBuffer.GetAllInDtBuffer() orderby c.log_date_time descending select c).GroupBy(g => g.name).Select(x => x.FirstOrDefault()).ToList();// GlobalVar.evntBuffer.GetAllInDtBuffer().OrderByDescending(s => s.log_date_time).Distinct().Take(GlobalVar.lastnRecordcnt).ToList();

                                int conditionMatchCntr = 0;

                                for (int i = 0; i < latestnRecords.Count() - 1; i++)
                                {

                                    TimeSpan ts = latestnRecords[i].log_date_time - latestnRecords[i + 1].log_date_time;
                                    int secondDiff = ts.Seconds;
                                    if (secondDiff <= GlobalVar.reconnectFreqThresholdInSeconds)
                                    {
                                        conditionMatchCntr++;
                                    }

                                }

                                if (conditionMatchCntr >= GlobalVar.affordableThreshold)
                                {

                                    postResjson["status"] = "Success";

                                   
                                    for (int i = 1; i <= actionOrderCount; i++)
                                    {
                                        JObject Info = JObject.Parse(actionOrder["action" + i.ToString()].ToString());
                                        ServiceInfo serviceInfo = lists.Find(m => m.name == Info["serviceName"].ToString());
                                        int delay = Convert.ToInt32(Info["actionBeforeDelay"].ToString());
                                     
                                        apis.ZookeeperAction(Info, serviceInfo);
                                    }
                                    postResjson["status"] = "Success";
                                    postResjson["message"] = "Action executed";
                                    postResjson["machine_name"] = jsonPayload["machine_name"].ToString();
                                    postResjson["machine_ip"] = jsonPayload["machine_ip"].ToString();
                                    GlobalVar.evntBuffer = new BufferCustom(GlobalVar.BufferDepth);

                                }
                                GlobalVar.reconnectReqFlag = false;
                                Console.WriteLine("reconnect false");
                            }
                            else
                            {
                                // return no such conf exist 

                                postResponse = "Url not matched ";
                                postResjson["status"] = "Failure";
                                postResjson["message"] = postResponse;
                                postResjson["machine_name"] = jsonPayload["machine_name"].ToString();
                                postResjson["machine_ip"] = jsonPayload["machine_ip"].ToString();
                            }
                        }
                        break;
                    case "/trigger-action/":
                        {
                            List<ServiceInfo> lists = apis.GetAllServicesInfo();
                            if (customRestartJson.ContainsKey("/trigger-action"))
                            {
                                GlobalVar.reconnectReqFlag = true;
                                //Console.WriteLine("trigger action received");
                                // Get action mapped for the item 
                                JObject actionOrder = JObject.Parse(customRestartJson["/trigger-action"].ToString());
                                int actionOrderCount = actionOrder.Count;
                                Console.WriteLine("Trigger action received ");

                                for (int i = 1; i <= actionOrderCount; i++)
                                {
                                    JObject actionMap = JObject.Parse(actionOrder["action" + i.ToString()].ToString());
                                    ServiceInfo serviceInfo = lists.Find(m => m.name == actionMap["serviceName"].ToString());
                                    apis.ZookeeperAction(actionMap, serviceInfo);
                                }
                                GlobalVar.reconnectReqFlag = false;
                            }
                            else
                            {
                                // return no such conf exist 

                                postResponse = "Url not matched ";
                                postResjson["status"] = "Failure";
                                postResjson["message"] = postResponse;
                                postResjson["machine_name"] = jsonPayload["machine_name"].ToString();
                                postResjson["machine_ip"] = jsonPayload["machine_ip"].ToString();
                            }
                        }
                        break;
                    case "/new-service":
                        {
                            Console.WriteLine("Adding new Service");
                            JObject newServerDetails = JObject.Parse(jsonPayload.ToString());
                            ServiceAPIs service = new ServiceAPIs();
                            service.AddingNewService(newServerDetails);
                        }
                        break;
                    case "/delete-service":
                        {
                            Console.WriteLine("Deleting the Service");
                            JObject deleteServerDetails = JObject.Parse(jsonPayload.ToString());
                            ServiceAPIs service = new ServiceAPIs();
                            service.DeleteService(deleteServerDetails);
                        }
                        break;
                    case "/restart-service":
                        {
                            string serviceName = jsonPayload["mats_name"].ToString();
                            Console.WriteLine("Manual  service restart " + serviceName);
                            // get the service info from list
                            ServiceInfo serviceInfo = ServiceAPIs.serviceList.Find(m => m.name == serviceName);
                            // int index = serviceList.FindIndex(m => m.name == serviceName);
                            ServiceInfo serviceDetails = new ServiceInfo();

                            serviceDetails.name = serviceInfo.name;
                            serviceDetails.exeName = serviceInfo.exeName;

                            serviceDetails.startTime = serviceInfo.startTime;
                            serviceDetails.stopTime = DateTime.Now;
                            serviceDetails.stopReason = "Manual";
                            serviceDetails.status = 0;
                            serviceDetails.health_check_timeout = serviceInfo.health_check_timeout;
                            serviceInfo.SkipQueue.Enqueue(serviceDetails);

                            if (serviceInfo.hbWsClient.ResetConnectUri(serviceInfo))
                            {
                                postResjson["name"] = serviceName;
                                postResjson["status"] = "Success";

                                postResponse = postResjson.ToString();
                            }
                            else
                            {
                                postResjson["name"] = serviceName;
                                postResjson["status"] = "Failure";
                                postResponse = postResjson.ToString();
                            }
                        }
                        break;
                    default:
                        {
                            postResponse = "GET url not mapped";
                            postResjson["status"] = "Failure";
                            postResjson["message"] = postResponse;
                            postResjson["machine_name"] = jsonPayload["machine_name"].ToString();
                            postResjson["machine_ip"] = jsonPayload["machine_ip"].ToString();
                        }
                        break;
                }

                //----- Writing response to the client socket------//
                //  context.Response.StatusCode = 200;

                //----- Writing response to the client socket------//
                context.Response.StatusCode = 200;
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                output = context.Response.OutputStream;

                byte[] array = Encoding.ASCII.GetBytes(postResjson.ToString());
                output.Write(array, 0, array.Count());
                //----- Writing response to the client socket------//

                // close the responsew
                context.Response.Close();


            }
            catch (Exception ex)
            {
                GlobalVar.reconnectReqFlag = false;
                context.Response.StatusCode = 502;
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                output = context.Response.OutputStream;

                postResponse = "Error in Server:" + ex.Message;
                postResjson["status"] = "Failure";
                postResjson["message"] = postResponse;
                postResjson["machine_name"] = jsonPayload["machine_name"].ToString();
                postResjson["machine_ip"] = jsonPayload["machine_ip"].ToString();
                byte[] array = Encoding.ASCII.GetBytes(postResjson.ToString());
                output.Write(array, 0, array.Count());
                //----- Writing response to the client socket------//

                // close the responsew
                context.Response.Close();
            }

        }

        public void PUT(HttpListenerContext context)
        {
            throw new NotImplementedException();
        }
    }
}
