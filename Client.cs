//using LEDDetect;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Network;

public delegate void cbFncBytesToRead(byte[] data);
public delegate void cbFncStringToRead(string data);
namespace Network
{
    public class Client
    {
        TcpClient tcpclnt;
        NetworkStream stm;
        Stream nNetStream;
        private byte[] result = new byte[5000];
           
        private int index = 0;
        private byte[] sentData = new byte[100];
        public bool READ_TIME_OUT = false;
        Thread readbytes;
        bool threadRun = false;
        Int32 msgcount;
        long readlen;
        string receiveJsonData;

        ~Client() 
        {

            if (tcpclnt != null)
                tcpclnt.Close();
            result = null;
            sentData = null;

            
        }

        bool ENABLE_DNS = false;

        /*
         * Below constructor connects to the server program provided valid ipAddress and valid port.
         */
        public Client(string ipAddress, string port)
        {
            try
            {

                if (ENABLE_DNS)
                {
                    tcpclnt = new TcpClient(ipAddress, Convert.ToInt16(port));
                }
                else {
                    tcpclnt = new TcpClient();
                    tcpclnt.Connect(ipAddress, Convert.ToInt32(port));
                
                }
             
                stm = tcpclnt.GetStream();
              
            }
            
            catch (Exception ex )
            {
             
                Console.WriteLine("Not able to connect to the server {0}", ex.Message);

               
               return;
               
            }
        }

        public Client(string ipAddress, string port, bool ENABLE_DNS, string client_ip)
        {
            try
            {
                var localEndPoint = new IPEndPoint(IPAddress.Parse(client_ip), port: 0);
                tcpclnt = new TcpClient(localEndPoint);
                if (ENABLE_DNS)
                {
                    //tcpclnt = new TcpClient(ipAddress, Convert.ToInt16(port));
                    tcpclnt.Connect(ipAddress, Convert.ToInt32(port));
                }
                else
                {
                    //tcpclnt = new TcpClient();

                    IPAddress ip = IPAddress.Parse(ipAddress);
                    tcpclnt.Connect(ip, Convert.ToInt32(port));

                }

                stm = tcpclnt.GetStream();

            }

            catch (Exception ex)
            {

                Console.WriteLine("Exception in Connecting to Server {0}", ex.Message);
                // Environment.Exit(0);

                

            }
        }

        public bool Connected
        {
            get { return tcpclnt.Connected; }
        }
        public void AsyncRead(cbFncStringToRead read)
        {
            readbytes = new Thread(() => ReceiveAsync(read,"}}}"));
            readbytes.Start();
        }
        public void AsyncRead(cbFncBytesToRead read, bool start)
        {
            if (start)
            {

              
                readbytes = new Thread(() => Read(read));
                readbytes.Start();
                threadRun = true;
            }
            else
            {
                if (readbytes != null)//&& (readbytes.ThreadState == ThreadState.Running || readbytes.IsAlive == true))
                {
                    threadRun = false;
                    Thread.Sleep(1000);
                    readbytes = new Thread(() => Read(read));
                    readbytes = null;

                 
                }
            }
        }
        /*
         * Send function sends the given string to the server.
         * Converts the string to the byte[] and write to the stream declared above
         */


        public bool CheckConnection()
        {
            try
            {
                if (tcpclnt.Client != null && tcpclnt.Client.Connected)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in Check connection {0}",ex.Message);
                return false;
            }
        }
        public void Send(string str)
        {
            try
            {

                if (TcpClientConnected() )
                {
                    result = new byte[5000];
                    
                    ASCIIEncoding asen = new ASCIIEncoding();
                    byte[] ba = asen.GetBytes(str);
                    stm.Write(ba, 0, ba.Length); 
                }
               
               
            }
            catch(Exception ex)
            {
               // Console.WriteLine("Function : Send {0}",ex.Message);
                return;
            }
        }
       
        /*
         * Send function sends the byte[] to the server
         */
        public void Send(byte[] ba, int length)
        {
           // result = new byte[5000];
            sentData = ba;
            stm.Write(ba, 0, length);
        }
        /*
         * Below function closes the TcpClient 
         */
        public void Close()
        {
            try
            {
               if (tcpclnt!= null)
                tcpclnt.Close();

               if (stm != null)
               {
                   stm.Close();
                   stm.Dispose();
               }
            }
            catch (Exception)
            {
              
                Console.WriteLine("Exception in closing client ");
                return;
            }
            

        }
        /*
         * Below function reads the data from the server and stores in byte[]
         */
        private byte[] Read()
        {
            try
            {
                if(tcpclnt.Connected)
                {
                    byte[] bb = new byte[5000];
                    stm.ReadTimeout = 1000;
                    int k = stm.Read(bb, 0, 5000);
                    Array.Resize(ref bb, k);
                    if (k > 0)
                        return bb;
                    else
                    {
                        return null;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
               //if (GlobalVar.debugFlag)
              //  Console.WriteLine("except byte[] Read() {0}", ex.Message);
                // Send("");
                return null;
            }
        }

        private byte[] Read(cbFncBytesToRead read)
        {
            string msgtxt;
            msgtxt = "";
            msgcount = 0;
            while (threadRun)
            {
                readlen = 0;
                if (tcpclnt.Connected)
                {
                    try
                    {
                        byte[] bb = new byte[5000];
                        // stm.ReadTimeout = 1000;
                        if (stm.DataAvailable)
                        {
                            //                            long tmp = stm.Length;
                            int k = stm.Read(bb, 0, 5000);
                            //                            stm.Flush();
                            msgcount = msgcount + k;
                            if (k > 0)
                            {
                                Array.Resize(ref bb, k);
                                read(bb);
                            }
                        }
                    }
                    catch (System.IO.IOException ex)
                    {
                        msgtxt = "stm: " + stm.CanRead.ToString() + "    tcp: " + tcpclnt.Connected.ToString();
                        Console.WriteLine(ex.Message, msgtxt);
                      
                    }
                } // if(tcpclnt.Connected)
            }
            return null;
        }
        // Check Whether Socket is connected or not
        public bool TcpClientConnected()
        {
            try
            {
                bool part1 = tcpclnt.Client.Poll(1000, SelectMode.SelectRead);
                bool part2 = (tcpclnt.Client.Available == 0);
                if ((part1 && part2) || (tcpclnt.Client.Connected == false))
                    return false;
                else
                    return true;
            }
            catch (Exception ex)
            {

                Console.WriteLine("Exception in TcpClientConnected {0}", ex.Message);
                return false;
            }
           
        }
        /*
         * Below function reads the data from the server till terminator is reached
         */
        //public byte[] Receive(string terminator)
        //{
        //    byte[] bb = Read();
        //    if (bb != null)
        //    {
        //        while (bb[bb.Length - 1] != Convert.ToByte(terminator))
        //        {
        //            Array.Copy(bb, 0, result, index, bb.Length);
        //            index += bb.Length;
        //            bb = Read();
        //            if (bb == null)
        //            {
        //                index = 0;
        //                return null;
        //            }
        //        }
        //        Array.Copy(bb, 0, result, index, bb.Length);
        //        index += bb.Length;
        //    }
        //    else
        //    {
        //        index = 0;
        //        return null;
        //    }
        //    Array.Resize(ref result, index);
        //    index = 0;
        //    return result;
        //}

        public string ReceiveAsync( cbFncStringToRead fnx,string comparer)
        {
            while (true)
            {
                fnx(Receive("}}}"));
            }
        }

        public string Receive(string comparer)
        {
            try
            //while(true)
            {

                byte[] bb = Read();
                if(bb == null && !tcpclnt.Connected )
                {
                    return null;
                }
                while (bb != null)
                {
                    string str = ASCIIEncoding.ASCII.GetString(bb);
                    receiveJsonData += str;
                    int lastIndex = receiveJsonData.Length - comparer.Length;
                    bool isContinue = false;
                    foreach (char c in comparer)
                    {
                        char val = receiveJsonData[lastIndex++];
                        if ( val!= c)
                        {
                            isContinue = true;
                            break;
                        }
                    }
                    if(isContinue)
                    {
                        bb = Read();
                        continue;
                    }
                    bb = null;
                }
                string retVal = receiveJsonData;
                receiveJsonData = string.Empty;
                return retVal;
            }
            catch(Exception ex)
            {
               // Console.WriteLine("Exception in Receive :{0}", ex.Message);
                return null;
            }
        }
        /*
         * Below function reads the data from the server till terminator is reached
         */
        public byte[] Receive()
        {
            byte[] bb = Read();
            if (bb != null)
            {
                while (bb[bb.Length - 1] != 13)
                {
                    Array.Copy(bb, 0, result, index, bb.Length);
                    index += bb.Length;
                    bb = Read();
                    if (bb == null)
                    {
                        index = 0;
                        return null;
                    }
                }
                Array.Copy(bb, 0, result, index, bb.Length);
                index += bb.Length;
            }
            else
            {
                index = 0;
                return null;
            }
            Array.Resize(ref result, index);
            index = 0;
            return result;
        }

        //public string Receive(string compareString)
        //{
        //    int compareLen = compareString.Length;
        //    byte[] bb = Read();
        //    if (bb != null)
        //    {
        //        int j =compareLen-1;
        //        bool isCompared = true;
        //        for (int i = bb.Length-1; j != 0;i--,j--)
        //        {
        //            if(Convert.ToChar(bb[i]) != compareString[j])
        //            {
        //                isCompared = false;
        //            }
        //        }
        //        while (!isCompared)
        //        {
        //            Array.Copy(bb, 0, result, index, bb.Length);
        //            index += bb.Length;
        //            bb = Read();
        //            if (bb == null)
        //            {
        //                index = 0;
        //                return null;
        //            }
        //        }
        //        Array.Copy(bb, 0, result, index, bb.Length);
        //        index += bb.Length;
        //    }
        //    else
        //    {
        //        index = 0;
        //        return null;
        //    }
        //    Array.Resize(ref result, index);
        //    index = 0;
        //    return ASCIIEncoding.ASCII.GetString(result);
        //}

        public byte[] Receive(long count)
        {
            byte[] bb = Read();
            if (bb != null)
            {
                while ((bb.Length + index) != count)
                {
                    Array.Copy(bb, 0, result, index, bb.Length);
                    index += bb.Length;
                    bb = Read();
                    if (bb == null)
                    {
                        index = 0;
                        return null;
                    }
                }
                Array.Copy(bb, 0, result, index, bb.Length);
                index += bb.Length;
            }
            else
            {
                index = 0;
                return null;
            }
            Array.Resize(ref result, index);
            index = 0;
            return result;
        }
    }
}
