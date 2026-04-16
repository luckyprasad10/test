using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ServiceMonitor
{
    public class scheduled_timings
    {
        public DateTime start_time; // start time- when task is invoked
        public Timer timer; // timer for performing schedule

        [JsonConverter(typeof(StringEnumConverter))]
        public RepeatSchedule repeat; // Schedule Repeat  param(Daily /weekly...etc)
        public int repeatEvery = 1; //  number of times to repeat
        public bool useDefaultSchedule = false; // this flag ignores the start time of schedule and starts timer immediately but repeat schedule is consider
        public scheduled_timings(DateTime start_time)
        {
            this.start_time = start_time;

        }
    }
    public enum RepeatSchedule
    {
        Once = 0,
        Daily = 1,
        Hourly = 2,
        Weekly = 3,
        Monthly = 4,
        Yearly = 5,
        Minutes = 6,
        Seconds = 7
    }
    public class SchedulerS
    {

        public SchedulerS()
        {

        }

        /// <summary>
        /// Start schedule timer for the service , timer will be started at start_time mentioned in scheduledList, if the start time is missed , it will 
        /// be executed from next day
        /// </summary>
        /// <param name="t"></param>
        //public void Schedule_Timer(ServiceInfo t)
        //{
        //    DateTime nowTime = DateTime.Now;
        //    DateTime defaultSchedule = new DateTime(nowTime.Year, nowTime.Month, nowTime.Day, 0, 0, 0, 0);
        //    int smallest_hour = 23;
        //    int smallest_min = 59;
        //    int smallest_sec = 59;
        //    for (int i = 0; i < t.scheduledList.Count; i++)
        //    {
        //        RepeatSchedule repeat = RepeatSchedule.Daily;
        //        int repeatEvery = 1;

        //        if (!t.scheduledList[i].useDefaultSchedule)
        //        {
        //            repeat = t.scheduledList[i].repeat;
        //            repeatEvery = t.scheduledList[i].repeatEvery;
        //            if (smallest_hour >= t.scheduledList[i].start_time.Hour)
        //            {
        //                smallest_hour = t.scheduledList[i].start_time.Hour;
        //                if (smallest_min > t.scheduledList[i].start_time.Minute)
        //                {
        //                    smallest_min = t.scheduledList[i].start_time.Minute;
        //                    if (smallest_sec > t.scheduledList[i].start_time.Second)
        //                    {
        //                        smallest_sec = t.scheduledList[i].start_time.Second;
        //                    }
        //                }
        //            }
        //        }
        //        else
        //        {
        //            t.scheduledList[i].start_time = defaultSchedule;
        //            smallest_hour = smallest_min = smallest_sec = 0;

        //        }

        //        DateTime scheduledTime = new DateTime(nowTime.Year, nowTime.Month, nowTime.Day, t.scheduledList[i].start_time.Hour, t.scheduledList[i].start_time.Minute, t.scheduledList[i].start_time.Second, 0); //Specify your scheduled time HH,MM,SS [8am and 42 minutes]

        //        if (nowTime < scheduledTime)
        //        {
        //            //set timer here as timer current time is yet to reach scheduled time
        //            double tickTime = (double)(scheduledTime - DateTime.Now).TotalMilliseconds;
        //            Console.WriteLine("Scheduled time {0}", scheduledTime);
        //            t.scheduledList[i].timer = new Timer(tickTime);
        //            t.scheduledList[i].timer.AutoReset = true;
        //            t.scheduledList[i].timer.Elapsed += (s1, e2) => timer_Elapsed(s1, e2, t, repeat, repeatEvery);
        //            t.scheduledList[i].timer.Start();
        //        }
        //        else
        //        {

        //            DateTime scheduledTime2 = new DateTime(t.scheduledList[i].start_time.Year, t.scheduledList[i].start_time.Month, t.scheduledList[i].start_time.Day, smallest_hour, smallest_min, smallest_sec, 0); //Specify your scheduled time HH,MM,SS [8am and 42 minutes]
        //            scheduledTime2 = scheduledTime2.AddDays(1);
        //            double tickTime = (double)(scheduledTime2 - DateTime.Now).TotalMilliseconds;
        //            Console.WriteLine( "Scheduled time {0}", scheduledTime2);
        //            t.scheduledList[i].timer = new Timer(tickTime);
        //            t.scheduledList[i].timer.AutoReset = true;
        //            t.scheduledList[i].timer.Elapsed += (s1, e2) => timer_Elapsed(s1, e2, t, repeat, repeatEvery);

        //            t.scheduledList[i].timer.Start();

        //        }



        //    }
        //}

        /// <summary>
        /// Action to be taken when timer is hit, if reatSchedule is 0 , task will be executed and stopped , in repeated schedule , next schedule
        /// ticktime is calculateed on basis of schedule and interval is set and timer is started again
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="srv"></param>
        /// <param name="repeatSch"></param>
        /// <param name="repeatEvery"></param>
        public void timer_Elapsed(object sender, ElapsedEventArgs e, ServiceInfo srv, RepeatSchedule repeatSch, int repeatEvery)
        {
            try
            {
                int repeat = (int)repeatSch;
                Task.Run(() =>
                {
                    CustomEventArgs cust = new CustomEventArgs(srv);
                    schedulerActionEvent?.Invoke(this, cust);
                });
                // Console.WriteLine("Start reset " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.ffff"));
                Timer timer = sender as Timer;
                timer.Stop();
                DateTime datetime = DateTime.Now;
                DateTime schedule;
                double tickTime = 0;
                switch (repeat)
                {
                    case 0:
                        {
                            timer.Stop();
                            timer.Dispose();
                        }
                        break;
                    case 1:
                        {
                            schedule = datetime.AddDays(1 * repeatEvery);
                            tickTime = (double)(schedule - datetime).TotalMilliseconds;
                        }
                        break;
                    case 2:
                        {
                            schedule = datetime.AddHours(1 * repeatEvery);
                            tickTime = (double)(schedule - datetime).TotalMilliseconds;
                        }
                        break;
                    case 3:
                        {
                            schedule = datetime.AddDays(7 * repeatEvery);
                            tickTime = (double)(schedule - datetime).TotalMilliseconds;
                        }
                        break;
                    case 4: //monthly
                        {
                            schedule = datetime.AddMonths(1 * repeatEvery);
                            tickTime = (double)(schedule - datetime).TotalMilliseconds;
                        }
                        break;
                    case 5:
                        {
                            schedule = datetime.AddYears(1 * repeatEvery);
                            tickTime = (double)(schedule - datetime).TotalMilliseconds;
                        }
                        break;
                    case 6:
                        {
                            schedule = datetime.AddMinutes(1 * repeatEvery);
                            tickTime = (double)(schedule - datetime).TotalMilliseconds;
                        }
                        break;
                    case 7:
                        {
                            schedule = datetime.AddSeconds(1 * repeatEvery);
                            tickTime = (double)(schedule - datetime).TotalMilliseconds;
                        }
                        break;
                    default:
                        {
                            timer.Stop();
                            timer.Dispose();
                        }
                        break;
                }

                timer.Interval = tickTime;
                timer.Start();


            }
            catch (Exception ex)
            {
                // if (IsDebugModeEnabled)
                Console.WriteLine("timer_Elapsed Error: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Event to be called on timer elapsed event
        /// </summary>
        public event EventHandler<CustomEventArgs> schedulerActionEvent;


    }

    /// <summary>
    /// Custom event arg for schedulerActionEvent
    /// </summary>
    public class CustomEventArgs : EventArgs
    {
        public CustomEventArgs(ServiceInfo service)
        {
            ServiceDetails = service;
        }

        public ServiceInfo ServiceDetails { get; set; }
    }
    
}
