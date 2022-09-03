using System;

namespace x42.Feature.Database.Entities
{
    public class Server
    {
        public int Id { get; set; }
        public string ProfileName { get; set; }
        public string SignAddress { get; set; }
        public int ProfileHeight { get; set; }
        public DateTime DateAdded { get; set; }
    }
}
