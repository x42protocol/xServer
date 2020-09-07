using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace x42.Feature.Database.Tables
{
    [Table("dictionary")]
    public class DictionaryData
    {
        [Key]
        public string Key { get; set; }
        public string Value { get; set; }
    }
}