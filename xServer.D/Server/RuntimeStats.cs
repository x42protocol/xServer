using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace x42.Server
{
    public class RuntimeStats
    {
        private readonly object LOCK_PUBLIC_REQUEST = new object();
        private DateTime _timeStart = DateTime.MinValue;
        private long _publicRequestCount;

        public RuntimeStats()
        {
            _timeStart = DateTime.Now;
        }

        public long PublicRequestCount
        {
            get
            {
                lock (LOCK_PUBLIC_REQUEST)
                {
                    return _publicRequestCount;
                }
            }
        }

        public string TimeString
        {
            get
            {
                string day = "days";
                string hour = "days";
                string minute = "days";

                TimeSpan now = DateTime.Now - _timeStart;
                object[] timeFormat = new object[3];
                int days = now.Days;
                timeFormat[0] = days.ToString("D2");
                days = now.Hours;
                timeFormat[1] = days.ToString("D2");
                days = now.Minutes;
                timeFormat[2] = days.ToString("D2");
                return string.Format("{0} days {1} hours {2} minutes", timeFormat);
            }
        }

        public void IncrementPublicRequest()
        {
            lock (LOCK_PUBLIC_REQUEST)
            {
                if (_publicRequestCount < long.MaxValue)
                {
                    _publicRequestCount++;
                }
            }
        }

    }
}
