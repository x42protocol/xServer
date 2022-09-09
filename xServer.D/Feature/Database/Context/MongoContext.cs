using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace x42.Feature.Database.Context
{
    public class MongoContext : IMongoContext
    {
        private IMongoDatabase Database { get; set; }
        public IClientSessionHandle Session { get; set; }
        public MongoClient MongoClient { get; set; }
        private readonly List<Func<Task>> _commands;
        private readonly DatabaseSettings databaseSettings;

        public MongoContext(DatabaseSettings databaseSettings)
        {

            // Every command will be stored and it'll be processed at SaveChanges
            _commands = new List<Func<Task>>();
            this.databaseSettings = databaseSettings;
        }

        public async Task<int> SaveChanges()
        {
            ConfigureMongo();

            using (Session = await MongoClient.StartSessionAsync())
            {
                var commandTasks = _commands.Select(c => c());

                await Task.WhenAll(commandTasks);
                _commands.Clear();

            }

            return _commands.Count;
        }

        private void ConfigureMongo()
        {
            if (MongoClient != null)
            {
                return;
            }

            if (string.IsNullOrEmpty(this.databaseSettings.Mongoconnectionstring))
            {
                throw new Exception("MongoDB connection has not been configured. Check your mongo settings.");
            }
            MongoClient = new MongoClient(this.databaseSettings.Mongoconnectionstring);
            Database = MongoClient.GetDatabase(this.databaseSettings.MongoDbName);
        }

        public IMongoCollection<T> GetCollection<T>(string name)
        {
            ConfigureMongo();

            return Database.GetCollection<T>(name);
        }

        public void Dispose()
        {
            Session?.Dispose();
            GC.SuppressFinalize(this);
        }

        public void AddCommand(Func<Task> func)
        {
            _commands.Add(func);
        }
    }
}
