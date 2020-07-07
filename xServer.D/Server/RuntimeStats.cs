using NBitcoin.Policy;
using System;
using x42.ServerNode;

namespace x42.Server
{
    public class RuntimeStats
    {
        private readonly object LOCK_PUBLIC_REQUEST = new object();
        private Tier.TierLevel _tier;
        private readonly object LOCK_TIER_LEVEL = new object();
        private DateTime _timeStart = DateTime.MinValue;
        private long _publicRequestCount;

        public RuntimeStats()
        {
            _timeStart = DateTime.Now;
            _tier = Tier.TierLevel.Seed;
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

        public void UpdateTierLevel(Tier.TierLevel tier)
        {
            lock (LOCK_TIER_LEVEL)
            {
                _tier = tier;
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
