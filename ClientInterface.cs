using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace ServiceMonitor
{
    public abstract class ClientInterface<T>
    {

        // config flag
        public string name;
        public int hbCheckTime = 15000;
        public bool enable = true;
        public string reportType = string.Empty;
        public int reportingPeriod = 0;
        public string module = string.Empty;
        public string serverIp = string.Empty;
        public string port = string.Empty;
        public bool enableDebug = true;
        public bool networkAvailable = true;
        [JsonIgnore]
        public Thread clientConnThread;
        [JsonIgnore]
        public bool reconnect = true;
        [JsonIgnore]
        public int reconnectTime = 1000;
        public int reconnectCount = 0;
        public T service;

        //[JsonIgnore]
        //public Queue<string> Rqueue = new Queue<string>();
        //[JsonIgnore]
        //public Queue<string> Squeue = new Queue<string>(); 


        // [JsonIgnore]
        //public System.Timers.Timer hbTimer;
        [JsonIgnore]
        public int hbCount = 0;

        // connect to respective destination
        public abstract void Connect(T t);

        // disconnect from the respective destination
        public abstract void Disconnect(T t);

        //// send data to respective destination
        //public abstract void Send();

        //// receive data to respective destination
        //public abstract void Receive();

        /// <summary>
        /// Reconnect 
        /// </summary>
        public void Reconnect()
        {
            try
            {
                if (enableDebug)
                {
                    Console.WriteLine("Reconnect Thread Running");
                }
                reconnectCount = 0;
                while (enable)
                {
                    while (reconnect || !networkAvailable)
                    {
                        try
                        {
                            reconnectCount++;
                            if (enableDebug)
                            {
                                Console.WriteLine("Trying to connect {0}", reconnectCount);
                            }

                            // client Disconnect from server
                            Disconnect(service);

                            // client Connect to server 
                            Connect(service);
                            if (!reconnect)
                            {
                                reconnectCount = 0;
                            }
                            // sleep of 1sec
                            Thread.Sleep(reconnectTime);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("re" + ex.Message);
                        }

                    }
                    Thread.Sleep(reconnectTime);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }//reconnect

        //private void HbTimedEvent(object source, ElapsedEventArgs e)
        //{
        //    if (enableDebug)
        //       Console.WriteLine("hb timer check  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        //    try
        //    {
        //            if (hbCount == 0){
        //                reconnect = true;
        //                Disconnect(service);
        //             }
        //            else
        //            {
        //                reconnect = false;
        //            }
        //        hbCount = 0;

        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }
        //}

        //protected void setHbTimer()
        //{
        //    if (enableDebug)
        //    {
        //        Console.WriteLine("hb timer started");
        //    }

        //    hbTimer = new System.Timers.Timer(hbCheckTime); //changed
        //    hbTimer.Elapsed += HbTimedEvent;
        //    hbTimer.AutoReset = true;
        //    hbTimer.Enabled = true;

        //}


    }
}
