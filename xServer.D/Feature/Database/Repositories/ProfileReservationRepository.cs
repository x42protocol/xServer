using MongoDB.Driver;
using System.Linq;
using System.Threading.Tasks;
using x42.Feature.Database.Context;
using x42.Feature.Database.Entities;

namespace x42.Feature.Database.Repositories
{

    public class ProfileReservationRepository : BaseRepository<ProfileReservation>, IProfileReservationRepository
    {
        public ProfileReservationRepository(IMongoContext context) : base(context)
        {

        }

        public async Task<int> GetProfileReservationCountSearchAsync(string name, string keyAddress)
        {
            var nameFilter = Builders<ProfileReservation>.Filter.Eq("name", name);
            var keyAddressFilter = Builders<ProfileReservation>.Filter.Eq("keyAddress", keyAddress);

            var data = await DbSet.FindAsync(Builders<ProfileReservation>.Filter.And(nameFilter, keyAddressFilter));
            return data.ToList().Count();

        }

        public async Task<int> GetProfileReservationCountByNameAsync(string name)
        {
            var nameFilter = Builders<ProfileReservation>.Filter.Eq("name", name);

            var data = await DbSet.FindAsync(Builders<ProfileReservation>.Filter.And(nameFilter));
            return data.ToList().Count();
        }

        public async Task<int> GetProfileReservationCountByKeyAddressAsync(string keyAddress)
        {
            var keyAddressFilter = Builders<ProfileReservation>.Filter.Eq("keyAddress", keyAddress);

            var data = await DbSet.FindAsync(Builders<ProfileReservation>.Filter.And(keyAddressFilter));
            return data.ToList().Count();
        }

    

        public async Task<ProfileReservation> GetProfileReservationByKeyAddressAsync(string keyAddress)
        {
            var keyAddressFilter = Builders<ProfileReservation>.Filter.Eq("keyAddress", keyAddress);

            var data = await DbSet.FindAsync(Builders<ProfileReservation>.Filter.And(keyAddressFilter));
            return data.FirstOrDefault();
        }

        public async Task<ProfileReservation> GetProfileReservationByNameAsync(string name)
        {
            var nameFilter = Builders<ProfileReservation>.Filter.Eq("name", name);

            var data = await DbSet.FindAsync(Builders<ProfileReservation>.Filter.And(nameFilter));
            return data.FirstOrDefault();

        }
    }
}
