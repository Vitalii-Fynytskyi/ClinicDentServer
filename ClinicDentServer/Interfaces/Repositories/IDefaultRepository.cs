using ClinicDentServer.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ClinicDentServer.Interfaces.Repositories
{
    public interface IDefaultRepository<T> where T : BaseModel
    {
        public DbSet<T> dbSet { get; set; }
        public Task<bool> Remove(T entity);
        public Task<int> Add(T entity);
        public Task<T> Update(T entity);
    }
}
