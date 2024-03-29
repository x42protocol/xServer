﻿using System;

namespace x42.Feature.Database.Entities
{
    public class ServerNode
    {
        public long Id { get; set; }
        public string ProfileName { get; set; }
        public int NetworkProtocol { get; set; }
        public string NetworkAddress { get; set; }
        public long NetworkPort { get; set; }
        public string KeyAddress { get; set; }
        public string SignAddress { get; set; }
        public string FeeAddress { get; set; }
        public int Tier { get; set; }
        public string Signature { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime LastSeen { get; set; }
        public long Priority { get; set; }
        public bool Active { get; set; }
        public bool Relayed { get; set; }
    }
}
