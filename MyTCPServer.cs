using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public delegate void cbFncSocketAccept(Socket s, IPAddress ipAddress);
namespace ServiceMonitor
{
    class MyTCPServer
    {
        TcpListener myList;
        cbFncSocketAccept cbSocket;
        Queue<string> ReceivedDataQueue = new Queue<string>();
        public bool errorReceived = false;
        public string SEND_TERMINATOR = "\r";
        // Thread to accept the socket
        Thread acceptSocket;
        //Blank constructor
        public MyTCPServer() { }
        //Constructor
        public MyTCPServer(string ipAddress, string port, cbFncSocketAccept socketReceive)
        {
            try
            {
                IPAddress ipAd = IPAddress.Parse(ipAddress);
                /* Initializes the Listener */
                myList = new TcpListener(ipAd, Convert.ToInt32(port));
                cbSocket = socketReceive;
                /* Start Listeneting at the specified port */
                myList.Start();
            }
            catch (Exception e)
            {
                errorReceived = true;
                Console.WriteLine("Unable to Establish Server Connection.\n" + "Message:" + e.Message);
                Console.ReadKey();
                Environment.Exit(0);
            }
        }//end of constructor

        public void Start()
        {
            acceptSocket = new Thread(StartAccept);
            acceptSocket.Start();
        }
        private void StartAccept()
        {
            try
            {
              
                while (true)
                {
                    ThreadState state = Thread.CurrentThread.ThreadState;
                    if (state != ThreadState.AbortRequested && state != ThreadState.Aborted)
                    {
                        Socket s = myList.AcceptSocket();
                        IPAddress ip = ((IPEndPoint)(s.RemoteEndPoint)).Address;
                        cbSocket(s, ip);
                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception e)
            {
                ThreadState state = Thread.CurrentThread.ThreadState;
                if (state != ThreadState.AbortRequested && state != ThreadState.Aborted)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }// End of StartAccept()

        /* Receives the data from the client and returns the byte[]
         * If there is any timeout occurs, It returns null. 
         */
        private byte[] Read(Socket s)
        {
            try
            {
                if (s.Connected)
                {
                    byte[] b = new byte[500000];
                    s.ReceiveTimeout = 10000;
                    int k = s.Receive(b);
                    if (k > 0)
                    {
                        //Console.WriteLine("Server Received Data:" + ASCIIEncoding.ASCII.GetString(b, 0, k));
                        Array.Resize(ref b, k);
                        return b;
                    }
                }
                else
                {
                    Console.WriteLine("Connection Dropped for IP : {0} ", ((IPEndPoint)(s.RemoteEndPoint)).Address.ToString());
                    return null;
                }
            }
            catch (SocketException ex)
            {
                if (GlobalVar.IsDebugModeEnabled)
                    Console.WriteLine("Read(Socket s) Error: " + ex.Message);
            }
            return null;
        }//End of Read Socket

        public byte[] Receive(Socket s, string comparer)
        {
            if (s.Connected)
            {
                try
                {
                    byte[] result = new byte[5000000];
                    int ind = 0;
                    // receives the data from the client bench
                    byte[] bb = Read(s);
                    if (bb == null || bb.Length == 0)
                    {
                        return null;
                    }
                    while ((bb[bb.Length - 1] != Convert.ToByte(comparer[0])))
                    {
                        Array.Copy(bb, 0, result, ind, bb.Length);
                        ind += bb.Length;
                        bb = Read(s);
                        if (bb == null || bb.Length == 0)
                        {
                            return null;
                        }
                    }
                    Array.Copy(bb, 0, result, ind, bb.Length);
                    ind += bb.Length;
                    // Resize the array according to the number of bytes received
                    Array.Resize(ref result, ind);
                    return result;
                }
                catch (Exception ex)
                {
                    if (GlobalVar.IsDebugModeEnabled)
                        Console.WriteLine("RECEIVE ERROR : {0}", ex.Message);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }//End of receive Socket

        //I have not added all receive functions as we are using only one. - nitu

        public void Write(Socket s, string data)
        {
            if (s.Connected)
            {
                s.Send(ASCIIEncoding.ASCII.GetBytes(data));
            }
        }
        public void Write(Socket s, byte[] data)
        {
            if (s.Connected)
            {
                s.Send(data);
            }
        }
        public void ClearReceiveBuffer(Socket s)
        {
            try
            {
                s.Receive(new byte[s.ReceiveBufferSize]);
            }
            catch (Exception ex)
            {
                if (GlobalVar.IsDebugModeEnabled)
                    Console.WriteLine("ClearReceiveBuffer ERROR : {0}", ex.Message);
            }
        }//public void ClearReceiveBuffer(Socket s)

        public void Disconnect(Socket s)
        {
            if (s.Connected)
            {
                s.Close();
            }
        }//public void Disconnect(Socket s)

        public void Stop()
        {
            if (myList != null)
            {
                if (myList.Server.Connected)
                {
                    myList.Server.Disconnect(false);
                }
                myList.Server.Close();
                myList.Stop();
            }

            if (acceptSocket != null)
            {
                acceptSocket.Abort();
            }
        }//public void Stop()


    }//End of class MyTCPServer
}//End of Name space
