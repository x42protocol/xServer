using System;

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

        public TimeSpan RuntimeTime
        {
            get
            {
                return DateTime.Now - _timeStart;
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
