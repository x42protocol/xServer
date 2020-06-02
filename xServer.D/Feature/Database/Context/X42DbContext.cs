using Microsoft.EntityFrameworkCore;
using x42.Feature.Database.Tables;

namespace x42.Feature.Database.Context
{
    class X42DbContext : DbContext
    {
        public virtual DbSet<ServerNodeData> ServerNodes { get; set; }
        public virtual DbSet<ServerData> Servers { get; set; }

        #region Initilize
        private readonly string _connectionString = "Server=127.0.0.1;Port=5432;Database=myDataBase;Integrated Security=true;";

        public X42DbContext() { }

        public X42DbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(_connectionString);

        }
        #endregion Initilize
    }
}