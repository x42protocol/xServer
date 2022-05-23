using System.Threading.Tasks;

namespace x42.Feature.Database.UoW
{
    public interface IUnitOfWork
    {
        Task<bool> Commit();
        void Dispose();
    }
}