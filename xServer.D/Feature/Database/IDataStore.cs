using System.Collections.Generic;
using x42.Feature.Database.Tables;

namespace x42.Feature.Database
{
    /// <summary>
    ///     Provides an interface for the data store.
    /// </summary>
    public interface IDataStore
    {
        ServerData GetSelfServer();
        int GetProfileReservationCountSearch(string name, string keyAddress);
        int GetProfileReservationCountByName(string name);
        int GetProfileReservationCountByKeyAddress(string keyAddress);
        int GetProfileCountSearch(string name, string keyAddress);
        int GetProfileCountByName(string name);
        int GetProfileCountByKeyAddress(string keyAddress);
        List<ProfileData> GetFirstProfilesFromBlock(int fromBlock, int take);
        ProfileData GetProfileByKeyAddress(string keyAddress);
        ProfileData GetProfileByName(string name);
        ProfileReservationData GetProfileReservationByKeyAddress(string keyAddress);
        ProfileReservationData GetProfileReservationByName(string name);
    }
}