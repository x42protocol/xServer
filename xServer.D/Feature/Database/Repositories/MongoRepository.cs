
using x42.Feature.Database.Context;

namespace x42.Feature.Database.Repositories
{
    public class MongoRepository<TEntity> : BaseRepository<TEntity> where TEntity : class, new()
    {
        public MongoRepository(IMongoContext context) : base(context)
        {
        }
    }
}
