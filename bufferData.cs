
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading.Tasks;

namespace FeedBotV1._0
{

    public class evtInfo
    {
       public DateTime log_date_time = new DateTime();
        public string ip { set; get; }
        public string name { set; get; }

        //public override bool Equals(object obj)
        //{
        //     if (!(obj is evtInfo))
        //    {
        //        return false;
        //    }
        //    var other = (evtInfo)obj;
        //    return name == other.name;
        //}

        //public override int GetHashCode()
        //{
        //    return name.GetHashCode();
        //}
    }
    /// <summary>
    /// Decides the type of buffer based on enum value
    /// </summary>
    public enum BufferType
    {
        Circular,                                      //buffer in the circular form so that it always contains latest n entries
        PlainArray,                                    //Plainarray buffer type holds buffer of particular size only
        Pingpong                                       //If streaming data is more then two buffers can be filled alternatively
    }

    /// <summary>
    /// Decides the type of data that should be coming in into the buffer
    /// </summary>
    public enum DataCfg
    {
        List,                                          //List collction to be filled in buffer
        ByteArray,                                     //Bytearray to be filled in buffer
        ArrayofObjects,                                //Array of objects to be filled in buffer                  
        evtInfo                                        //evtInfos from data tabel to be filled in buffer 
    }


    /// <summary>
    /// Buffer class to be initialized based on the buffer configurations
    /// </summary>
    public class BufferCustom
    {
        //Instance of buffer config class which will be initialized based on buffer configurations

        CircularBuffer BufferDt;

        CircularBuffer Algo_OutBufferDt;

        private bool FillBufferFlag;
        public int BufDepth { get; set; }
        public evtInfo dt { get; set; }

        public evtInfo AlgoLastDtRw { get; set; }

        public int AlgoOutBufDepth { get; set; }

        /// <summary>
        /// Buffer configurations
        /// </summary>   
        /// <param name="BfCfg"> instance of buffer config class to initiate buffer</param>
        public BufferCustom(int BufDepth)
        {
            this.BufDepth = BufDepth;
            BufferDt = new CircularBuffer(this.BufDepth, false);
            Algo_OutBufferDt = new CircularBuffer(this.AlgoOutBufDepth, false);
        }

        /// <summary>
        /// Property to store latest data row added in buffer
        /// </summary>
        public evtInfo GetLatestevtInfo
        {
            get { return this.dt; }
        }

        //get buffer
        public List<evtInfo> GetAllInDtBuffer()
        {
            List<evtInfo> RtDtRow = new List<evtInfo>();
            foreach (var item in BufferDt._buffer)
            {
                if (item == null)
                {

                }
                else
                {
                    RtDtRow.Add(item);
                }

            }
            return RtDtRow;
        }

        public List<evtInfo> GetAllOutDtBuffer()
        {
            List<evtInfo> RtDtRow = new List<evtInfo>();
            foreach (var item in Algo_OutBufferDt._buffer)
            {
                if (item == null)
                {

                }
                else
                {
                    RtDtRow.Add(item);
                }

            }
            return RtDtRow;
        }

        /// <summary>
        /// Fills the data published by data publisher to buffer
        /// </summary>
        /// <param name="publisheddtRw">evtInfo published by Data Publisher</param>
        public void FillBuffer(evtInfo publisheddtRw)
        {
            if (FillBufferFlag.Equals(true))
            {
                BufferDt.Enqueue(publisheddtRw);
                //Copy latest evtInfo --- to provide to indata buffer present in Algorithm class
                this.dt = publisheddtRw;
            }
            else
            {
                Console.WriteLine("Sorry buffer can't be filled , open the gate first");
            }

        }


        /// <summary>
        /// Fills the data published by data publisher to buffer
        /// </summary>
        /// <param name="publisheddtRw">evtInfo published by Data Publisher</param>
        public void FillProcessed_Buffer(evtInfo ComputeddtRw)
        {

            Algo_OutBufferDt.Enqueue(ComputeddtRw);
            //Copy latest evtInfo --- to provide to indata buffer present in Algorithm class
            this.AlgoLastDtRw = ComputeddtRw;

        }

        /// <summary>
        /// Function which closes or opens buffer gate based on parameter received
        /// </summary>
        /// <param name="Flag"></param>
        public void BufferOpenClose(bool Flag)
        {
            FillBufferFlag = Flag;
        }

        /// <summary>
        /// Clears all data from buffer
        /// </summary>
        public void FlushBuffer()
        {
            while (!BufferDt.IsEmpty)
            {
                BufferDt.Dequeue();
            }
        }
    }

    /// <summary>
    /// Class for circular buffer configurations and execution
    /// </summary>
    public class CircularBuffer
    {
        Stack<evtInfo> bf = new Stack<evtInfo>();

        public evtInfo[] _buffer;                              //Storing buffer in generic array
        public int _head { get; set; }                          //Contains head value that is last index
        public int _tail { get; set; }                          //Contains tail value that is first index    
        int _length;                         //Stores current length of buffer    
        int _bufferSize;                    //stores size of buffer
        bool _FlagOnBufferFull;             //Setting up the flag when buffer is filled    


        /// <summary>
        /// constructor for circular buffer class
        /// </summary>
        /// <param name="bufferSize"> depth of buffer</param>
        /// <param name="FlagOnBufferFull">whether to indicate user that buffer is filled or not</param>
        public CircularBuffer(int bufferSize, bool FlagOnBufferFull)
        {
            //Initiating the buffer
            _buffer = new evtInfo[bufferSize];

            //Initiating the buffersize
            _bufferSize = bufferSize;

            //Setting up the head to the buffer size - 1 
            _head = bufferSize - 1;

            //Setting flag according to the flag configurations
            _FlagOnBufferFull = FlagOnBufferFull;

        }

        /// <summary>
        /// Property which returs whether buffer is empty or not
        /// </summary>
        public bool IsEmpty
        {
            get { return _length == 0; }
        }

        /// <summary>
        /// Property which returs whether buffer is full or not
        /// </summary>
        public bool IsFull
        {
            get { return _length >= _bufferSize; }

        }

        /// <summary>
        /// Will be used to dequeue the data from buffer
        /// </summary>
        /// <returns></returns>
        public evtInfo Dequeue()
        {

            evtInfo dequeued = _buffer[_tail];
            _tail = NextPosition(_tail);
            _length--;
            return dequeued;
        }

        /// <summary>
        /// Returns current buffer in list format
        /// </summary>
        /// <returns></returns>
        public evtInfo[] GetBufferArray()
        {
            evtInfo[] RtBuffer = new evtInfo[_bufferSize];

            int j = 0;
            for (int i = _head; i == _tail; i++)
            {
                if (i == _bufferSize)
                {
                    i = 0;
                }
                if (_buffer[i].Equals(null))
                {
                    Console.WriteLine("Sorry");
                }
                else
                {
                    RtBuffer[j] = _buffer[i];
                    j++;
                }
            }

            // Console.WriteLine("Size of arrary is "+_buffer.Length);
            return _buffer;
        }

        /// <summary>
        /// Funtion to decide in which index the next upcoming value is to be filled
        /// </summary>
        /// <param name="Position"></param>
        /// <returns></returns>
        private int NextPosition(int Position)
        {
            if (Position == (_bufferSize - 1))                                                       //Else value of position will increase by 1
            {
                Position = 0;
            }
            else                                                                                    //Else value of position will increase by 1
            {
                Position += 1;
            }
            return Position;                                                                        //Returns position of index to be returned to store new value
        }

        /// <summary>
        /// Adds the data in buffer
        /// </summary>
        /// <param name="toAdd"> parameter to be added in buffer</param>
        public void Enqueue(evtInfo toAdd)
        {
            if (_FlagOnBufferFull)
            {
                _head = NextPosition(_head);
                if (IsFull)                                                                            //If buffer is full then takes action based on the flag set for over writing
                {
                    Console.WriteLine("Sorry couldn't write bufffer is filled");
                }
                else                                                                                   //If buffer is not full then adds value to the buffer
                {
                    _buffer[_head] = toAdd;
                    _length++;
                }
            }
            else                                                                                       //If flag is set to false then buffer will be get added in circular form
            {

                _head = NextPosition(_head);

                _buffer[_head] = toAdd;
                if (IsFull)                                                                             //If buffer is full then removes the first entry and adds latest entry to first position  
                {
                    _tail = NextPosition(_tail);
                }
                else
                {
                    _length++;
                }
            }
        }

    }
}


