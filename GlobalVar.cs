using FeedBotV1._0;
using System;
using System.Collections.Generic;
using WebSocketSharp.Server;

namespace ServiceMonitor
{
    public class GlobalVar
    {

        //TODO - take this from configuration
        public static int BufferDepth = 32;
        
        public static int reconnectFreqThresholdInSeconds = 60;
        public static int affordableThreshold = 20;

        public static Queue<ServiceInfo> database_conn_log_queue = new Queue<ServiceInfo>();
        public static BufferCustom evntBuffer;
        //Variable to hold the Tcpsocket port.
        public static int TcpSocketPort;// = 9876;
        //Variable to hold the Websocket port.
        public static int WebSocketPort;// = 8888;
        //Variable to hold theHTTP port.
        public static int HTTPSocketPort;// = 8887;

        //Variable will be used to console the data while debugging.
        public static bool IsDebugModeEnabled = false;

        /* Variable Which holds the Database data in the queue. */
        //public static Queue<JObject> DBDataQueue = new Queue<JObject>();

        /// <summary>
        /// Database Operations variable to access functions from MySQLOperations
        /// @author - Nitu @Date 7-Nov-2019


        //For websocket session
        public static MyWebsocketServer ws_server;
        public static WebSocketSessionManager wbSessionManager;

        /* Variable to store the data terminator for the tcp socket benchs*/
        public static string TcpSocketDesktopBenchTerminator = "}";

        public static string MasterIp = string.Empty;
        public static string SlaveIp = string.Empty;
        public static string HttpIp = string.Empty;
        /* Variable to check the valid state of the client bench. */
        public static bool CheckForValidState = true;

        public static string EthernetIp = string.Empty;

        public const string RequestState = "STATE";
        public const string RequestHeartBeat = "HB";

        //Status byte Values
        public const byte CONNECTED_R_VERIFIED = 1;
        public const byte CONNECTFAIL_R_VERIFYFAIL = 2;
        public const byte SOCKET_FAILED = 0;
        public const byte WAIT_FOR_INFO_ACK = 3;

        public enum OBJECT_DATA_TYPE
        {
            STRING_VAL = 0,
            JSON_OBJECT = 1,
            BOOLEAN_VAL = 2,
            INT_VAL = 3,
            FLOAT_VAL = 4,
            JSON_ARRAY = 5
        }

        //public static string CurrentFolderPath = System.IO.Path.GetDirectoryName(Application.ExecutablePath);
        public static bool Is_Windows = true;
      

        //public static  MySQLOperation DbOpp = new MySQLOperation();

        public static List<string> uriList;
        public static string UPLOAD_PATH = string.Empty;
        public static string LOCALHOST = string.Empty;

        public static string SHUTDOWN_PATH = string.Empty;

        public delegate void SetTimer();
        public static int updateEventCount;
        public static int conveyor_run_time =0;

        public static bool reconnectReqFlag = false;

        public static List<scheduled_timings> scheduled_timings_list = new List<scheduled_timings>();
    }
}
