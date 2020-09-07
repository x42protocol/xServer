using System;
using Microsoft.Extensions.Logging;
using x42.Feature.Database.Context;
using System.Linq;
using x42.Feature.Database.Tables;
using Microsoft.EntityFrameworkCore;

namespace x42.Feature.Database
{
    /// <inheritdoc />
    /// <summary>
    ///     Function for dictionary
    /// </summary>
    public class Dictionary
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>Instance logger.</summary>
        private readonly DatabaseSettings databaseSettings;

        public bool DatabaseConnected { get; set; } = false;

        public Dictionary(
            ILoggerFactory loggerFactory,
            DatabaseSettings databaseSettings
            )
        {
            logger = loggerFactory.CreateLogger(GetType().FullName);
            this.databaseSettings = databaseSettings;
        }

        public dynamic Get<T>(string key)
        {
            var result = default(T);
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                string value = dbContext.DictionaryItems.Where(d => d.Key == key).Select(d => d.Value).FirstOrDefault();
                result = ConvertType<T>(value);
            }
            return result;
        }

        public bool Set(string key, object value)
        {
            bool result = false;
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                var item = dbContext.DictionaryItems.Where(d => d.Key == key).FirstOrDefault();
                if (item == null)
                {
                    var newDictionaryItem = new DictionaryData()
                    {
                        Key = key,
                        Value = ConvertType<string>(value)
                    };
                    var addedItem = dbContext.Add(newDictionaryItem);
                    if (addedItem.State == EntityState.Added)
                    {
                        dbContext.SaveChanges();
                        result = true;
                    }
                }
                else
                {
                    item.Value = ConvertType<string>(value);
                    dbContext.SaveChanges();
                    result = true;
                }
            }
            return result;
        }

        private dynamic ConvertType<T>(object value)
        {
            if (value == null)
            {
                return default(T);
            }
            if (typeof(T) == typeof(int))
            {
                return Convert.ToInt32(value);
            }
            if (typeof(T) == typeof(long))
            {
                return Convert.ToInt64(value);
            }
            if (typeof(T) == typeof(decimal))
            {
                return Convert.ToDecimal(value);
            }
            if (typeof(T) == typeof(DateTime))
            {
                return Convert.ToDateTime(value).ToString();
            }
            if (typeof(T) == typeof(bool))
            {
                return Convert.ToBoolean(value);
            }
            else
            {
                return Convert.ToString(value);
            }
        }
    }
}