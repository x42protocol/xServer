using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using x42.Feature.Database.Context;
using x42.Feature.Database.Entities;

namespace x42.Feature.Database.Repositories.Profiles
{
    public class ProfileRepository : BaseRepository<XServerProfile>, IProfileRepository
    {
        public ProfileRepository(IMongoContext context) : base(context)
        {

        }
        public async Task<int> GetProfileCountSearchAsync(string name, string keyAddress)
        {
            var nameFilter = Builders<XServerProfile>.Filter.Eq("name", name);
            var keyAddressFilter = Builders<XServerProfile>.Filter.Eq("keyAddress", keyAddress);

            var data = await DbSet.FindAsync(Builders<XServerProfile>.Filter.And(nameFilter, keyAddressFilter));
            return data.ToList().Count();
        }

 
        public async Task<int> GetProfileCountByNameAsync(string name)
        {

            var nameFilter = Builders<XServerProfile>.Filter.Eq("name", name);

            var data = await DbSet.FindAsync(Builders<XServerProfile>.Filter.And(nameFilter));
            return data.ToList().Count();
        }

        public async Task<int> GetProfileCountByKeyAddressAsync(string keyAddress)
        {

            var keyAddressFilter = Builders<XServerProfile>.Filter.Eq("keyAddress", keyAddress);

            var data = await DbSet.FindAsync(Builders<XServerProfile>.Filter.And( keyAddressFilter));
            return data.ToList().Count();
        }

        public async Task<List<XServerProfile>> GetFirstProfilesFromBlockAsync(int fromBlock, int take)
        {
            var statusFilter = Builders<XServerProfile>.Filter.Eq("status", (int)Profile.Status.Created);
            var blockConfirmedFilter = Builders<XServerProfile>.Filter.AnyGt("blockConfirmed", fromBlock);
            var data = await DbSet.FindAsync(Builders<XServerProfile>.Filter.And(statusFilter, blockConfirmedFilter));

            return data.ToList().OrderBy(p => p.BlockConfirmed).Take(take).ToList();

        }

        public async Task<XServerProfile> GetProfileByKeyAddressAsync(string keyAddress)
        {

            var keyAddressFilter = Builders<XServerProfile>.Filter.Eq("keyAddress", keyAddress);

            var data = await DbSet.FindAsync(Builders<XServerProfile>.Filter.And(keyAddressFilter));
            return data.FirstOrDefault();

        }

        public async Task<XServerProfile> GetProfileByNameAsync(string name)
        {

            var nameFilter = Builders<XServerProfile>.Filter.Eq("name", name);

            var data = await DbSet.FindAsync(Builders<XServerProfile>.Filter.And(nameFilter));
            return data.FirstOrDefault();
        }

    }
}
