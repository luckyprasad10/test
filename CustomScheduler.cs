using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ServiceMonitor
{
   
    public class CustomScheduler
    {
        public List<scheduled_timings> schedule = new List<scheduled_timings>();

        public CustomScheduler()
        {

        }

        /// <summary>
        /// Start schedule timer for the service , timer will be started at start_time mentioned in scheduledList, if the start time is missed , it will 
        /// be executed from next day
        /// </summary>
        /// <param name="t"></param>
        public void Schedule_Timer(ServiceInfo t)
        {
            DateTime nowTime = DateTime.Now;
            DateTime defaultSchedule = new DateTime(nowTime.Year, nowTime.Month, nowTime.Day, 0, 0, 0, 0);
            int smallest_hour = 23;
            int smallest_min = 59;
            int smallest_sec = 59;
            for (int i = 0; i < schedule.Count; i++)
            {
                RepeatSchedule repeat = RepeatSchedule.Daily;
                int repeatEvery = 1;

                if (!schedule[i].useDefaultSchedule)
                {
                    repeat = schedule[i].repeat;
                    repeatEvery = schedule[i].repeatEvery;
                    if (smallest_hour >= schedule[i].start_time.Hour)
                    {
                        smallest_hour = schedule[i].start_time.Hour;
                        if (smallest_min > schedule[i].start_time.Minute)
                        {
                            smallest_min = schedule[i].start_time.Minute;
                            if (smallest_sec > schedule[i].start_time.Second)
                            {
                                smallest_sec = schedule[i].start_time.Second;
                            }
                        }
                    }
                }
                else
                {
                    schedule[i].start_time = defaultSchedule;
                    smallest_hour = smallest_min = smallest_sec = 0;
                }

                DateTime scheduledTime = new DateTime(nowTime.Year, nowTime.Month, nowTime.Day, schedule[i].start_time.Hour, schedule[i].start_time.Minute, schedule[i].start_time.Second, 0); //Specify your scheduled time HH,MM,SS [8am and 42 minutes]

                if (nowTime < scheduledTime)
                {
                    //set timer here as timer current time is yet to reach scheduled time
                    double tickTime = (double)(scheduledTime - DateTime.Now).TotalMilliseconds;
                    Console.WriteLine("Scheduled time {0}", scheduledTime);
                    schedule[i].timer = new Timer(tickTime);
                    schedule[i].timer.AutoReset = true;
                    schedule[i].timer.Elapsed += (s1, e2) => timer_Elapsed(s1, e2, t, repeat, repeatEvery);
                    schedule[i].timer.Start();
                }
                else
                {
                    DateTime scheduledTime2 = new DateTime();
                    scheduledTime2 = scheduledTime.AddDays(1);
                    double tickTime = (double)(scheduledTime2 - DateTime.Now).TotalMilliseconds;
                    Console.WriteLine("Scheduled time {0}", scheduledTime2);
                    schedule[i].timer = new Timer(tickTime);
                    schedule[i].timer.AutoReset = true;
                    schedule[i].timer.Elapsed += (s1, e2) => timer_Elapsed(s1, e2, t, repeat, repeatEvery);
                    schedule[i].timer.Start();
                }
            }
        }

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

   
}
