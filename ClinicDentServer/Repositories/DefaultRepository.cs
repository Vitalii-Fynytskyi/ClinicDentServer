using ClinicDentServer.Exceptions;
using ClinicDentServer.Interfaces.Repositories;
using ClinicDentServer.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ClinicDentServer.Repositories
{
    public class DefaultRepository<T> : IDefaultRepository<T> where T : BaseModel
    {
        public ClinicContext clinicContext;
        public DbSet<T> dbSet { get; set; }

        public DefaultRepository(ClinicContext clinicContextToSet)
        {
            dbSet = clinicContextToSet.Set<T>();
            clinicContext= clinicContextToSet;
        }
        public async virtual Task<int> Add(T entity)
        {
            if(entity == null)
            {
                throw new NotFoundException("Cannot add entity to database");
            }
            dbSet.Add(entity);
            await clinicContext.SaveChangesAsync();
            return entity.Id; 
        }

        public async virtual Task<bool> Remove(T entity)
        {
            if (entity == null)
            {
                throw new NotFoundException("Cannot remove entity from database");
            }

            dbSet.Remove(entity);
            await clinicContext.SaveChangesAsync();
            return true;

        }

        public async virtual Task<T> Update(T entity)
        {
            if (entity == null)
            {
                throw new NotFoundException("Cannot Update entity in database");
            }
            dbSet.Update(entity);
            await clinicContext.SaveChangesAsync();
            return entity;
        }
    }
}
