using Common.Models.XDocuments.Zones;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace Common.Services
{


    public class RecordModel
    {
        [BsonElement("content")]
        public string Content { get; set; }

        [BsonElement("disabled")]
        public bool Disabled { get; set; }

        public RecordModel(DnsRecord dnsRecord)
        {
            Content = dnsRecord.Content;
            Disabled = dnsRecord.Disabled;
        }
    }

    public class ZoneDocumentModel
    {
        [BsonElement("_id")]
        public string Id { get; set; }

        [BsonElement("zone")]
        public string Zone { get; set; }

        [BsonElement("keyAddress")]
        public string KeyAddress { get; set; }

        [BsonElement("signature")]
        public string Signature { get; set; }

        [BsonElement("pricelockId")]
        public object PricelockId { get; set; }

        [BsonElement("blockConfirmed")]
        public int BlockConfirmed { get; set; }

        [BsonElement("rrSets")]
        public List<RrSetModel> RrSets { get; set; }
    }

    public class RrSetModel
    {
        [BsonElement("changetype")]
        public string Changetype { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("records")]
        public List<RecordModel> Records { get; set; }

        [BsonElement("ttl")]
        public int Ttl { get; set; }

        [BsonElement("type")]
        public string Type { get; set; }

        public RrSetModel(RrSet rrSet)
        {
            Changetype = rrSet.ChangeType;
            Name = rrSet.Name;
            Records = rrSet.Records.Select(record => new RecordModel(record)).ToList();
            Ttl = rrSet.Ttl;
            Type = rrSet.Type;

        }
    }
}