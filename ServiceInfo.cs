using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Timers;
using WebSocketSharp;

namespace ServiceMonitor
{
    public  class ServiceInfo 
    {
        public string name { get; set; }
        public string customSchedulename { get; set; }
        public string folderPath { get; set; } 
        public int status { get; set; }
        public string args { get; set; }
        public string logPath { get; set; }
        public int restartWaitTime { get; set; }
        public  List<scheduled_timings> restartSchedule { get; set; }
        public List<scheduled_timings> startSchedule { get; set; }
        public List<scheduled_timings> stopSchedule { get; set; }
        public List<scheduled_timings> customSchedule { get; set; }
        public string exeName { get; set; }
        public string appSearch { get; set; }
       // public int skip_hb { get; set; } // if skip hb is enabled then it will only check 
        public int health_check_timeout { get; set; }
        public string health_check_ip { get; set; }
        public string health_check_port { get; set; }
        public string health_check_protocol { get; set; }
        public string health_check_api { get; set; }

        public string current_system_ip { get; set; }
        public Timer hbSchedule { get; set; }

        public WebsocketClient hbWsClient { get; set; }
       
        public HTTPClient hbHttpClient { get; set; }
     
        public TCPClient hbTcpClient { get; set; }
        public int hbCount { get; set; }

        public DateTime? startTime { get; set; }
        public DateTime? stopTime { get; set; }
        
        public string stopReason { get; set; }
     
        public bool enable { get; set; }
        public  HBSkipClient hbSkipClient { get; set; }

        public Queue<ServiceInfo> SkipQueue { get; set; }
    }
    public class NewServiceInfo
    {
        public string name { get; set; }
        public string folderPath { get; set; }
        public int status { get; set; }
        public string args { get; set; }
        public string logPath { get; set; }
        public int restartWaitTime { get; set; }
        public List<scheduled_timings> restartSchedule { get; set; }
        public List<scheduled_timings> startSchedule { get; set; }
        public List<scheduled_timings> stopSchedule { get; set; }
        public List<scheduled_timings> customSchedule { get; set; }
        public string exeName { get; set; }
        public string appSearch { get; set; }
        // public int skip_hb { get; set; } // if skip hb is enabled then it will only check 
        public int health_check_timeout { get; set; }
        public string health_check_protocol { get; set; }
    }
}
