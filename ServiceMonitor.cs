
using HTTPModule;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ServiceMonitor

{
    class ServiceMonitor : HTTPInterface
    {
        public ServiceAPIs apis = new ServiceAPIs();

        public ServiceMonitor()
        {
            apis.GetServiceListfile();
        }

        /// <summary>
        /// GET request from http client 
        /// </summary>
        /// <param name="context"></param>
        public void GET(System.Net.HttpListenerContext context)
        {
            string url = context.Request.Url.LocalPath;
            string getResponse = string.Empty;
            try
            {

                switch (url)
                {
                    // download - in uri list 0
                    case "/get-services":
                        {

                            List<ServiceInfo> lists = apis.GetAllServicesInfo();
                            var options = new JsonSerializerOptions { WriteIndented = true };
                            getResponse = JsonSerializer.Serialize(lists, options);
                        }
                        break;


                    default:
                        {
                            getResponse = "GET url not mapped";
                        }
                        break;
                }

                //----- Writing response to the client socket------//
                //  context.Response.StatusCode = 200;

                //----- Writing response to the client socket------//
                context.Response.StatusCode = 200;
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                Stream output = context.Response.OutputStream;

                byte[] array = Encoding.ASCII.GetBytes(getResponse);
                output.Write(array, 0, array.Count());
                //----- Writing response to the client socket------//

                // close the responsew
                context.Response.Close();


            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 502;
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                Stream output = context.Response.OutputStream;
                getResponse = "Error in Server:" + ex.Message;
                byte[] array = Encoding.ASCII.GetBytes(getResponse);
                output.Write(array, 0, array.Count());
                //----- Writing response to the client socket------//

                // close the responsew
                context.Response.Close();
            }

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
                // match url 
                switch (url)
                {
                    case "/restart-service":
                        {


                            string serviceName = jsonPayload["name"].ToString();
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
                    case "/stop-service":
                        {
                            Console.WriteLine("Stop service uri");
                            string value = jsonPayload["name"].ToString();
                            if (apis.StopService(value))
                            {
                                postResjson["name"] = value;
                                postResjson["status"] = "Success";
                                postResponse = postResjson.ToString();
                            }
                            else
                            {
                                postResjson["name"] = value;
                                postResjson["status"] = "Failure";
                                postResponse = postResjson.ToString();
                            }
                        }
                        break;
                    
                        break;


                    default:
                        {
                            postResponse = "POST Url not mapped";
                        }
                        break;
                }
                // response msg to be sent 
                output = context.Response.OutputStream;

                byte[] array = Encoding.ASCII.GetBytes(postResponse);
                output.Write(array, 0, array.Length);
                context.Response.Close();
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 502;
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                postResponse = "Error in service  " + url + " " + ex.Message;
                byte[] array = Encoding.ASCII.GetBytes(postResponse);
                output.Write(array, 0, array.Length);
                context.Response.Close();
            }

        }


        /// <summary>
        /// PUT request from http client 
        /// </summary>
        /// <param name="context"></param>
        public void PUT(System.Net.HttpListenerContext context)
        {

            throw new NotImplementedException();
        }


        /// <summary>
        /// DELETE request from http client 
        /// </summary>
        /// <param name="context"></param>
        public void DELETE(System.Net.HttpListenerContext context)
        {

            throw new NotImplementedException();
        }
    }
}
