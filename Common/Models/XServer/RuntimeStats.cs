using System;

namespace Common.Models.XServer
{
    public class RuntimeStats
    {
        private readonly object LOCK_TIER_LEVEL = new object();
        private Tier.TierLevel _tier;

        private DateTime _timeStart = DateTime.MinValue;

        private readonly object LOCK_STATE = new object();
        private int _state;

        private readonly object LOCK_PUBLIC_REQUEST = new object();
        private long _publicRequestCount;

        public StartupStatus StartupState { get; set; }

        public long BlockHeight { get; set; }

        public long AddressIndexerHeight { get; set; }

        public enum RuntimeState
        {
            Started = 1,
            Stopped = 2
        }

        public RuntimeStats()
        {
            _timeStart = DateTime.Now;
            _tier = Tier.TierLevel.Seed;
            _state = (int)RuntimeState.Stopped;
        }

        public void Reset(bool start)
        {
            _timeStart = DateTime.Now;
            lock (LOCK_STATE)
            {
                if (start)
                {
                    _state = (int)RuntimeState.Started;
                }
                else
                {
                    _state = (int)RuntimeState.Stopped;
                }
            }
            lock (LOCK_PUBLIC_REQUEST)
            {
                _publicRequestCount = 0;
            }
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

        public double SessionRunTimeSeconds
        {
            get
            {
                return (DateTime.Now - _timeStart).TotalSeconds;
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

        public void UpdateTierLevel(Tier.TierLevel tier)
        {
            lock (LOCK_TIER_LEVEL)
            {
                _tier = tier;
            }
        }

        public int State
        {
            get
            {
                lock (LOCK_STATE)
                {
                    return _state;
                }
            }
        }


        public Tier.TierLevel TierLevel
        {
            get
            {
                lock (LOCK_TIER_LEVEL)
                {
                    return _tier;
                }
            }
        }

    }
}
