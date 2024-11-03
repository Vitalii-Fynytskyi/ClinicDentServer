using ClinicDentServer.Interfaces.Repositories;
using ClinicDentServer.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ClinicDentServer.Repositories
{
    public class StageRepository<T> : DefaultRepository<T>, IStageRepository<T> where T : Stage
    {
        public StageRepository(ClinicContext clinicContextToSet) : base(clinicContextToSet)
        {
        }

        public async Task SendViaMessager(int stageId, int mark)
        {
            await clinicContext.Database.ExecuteSqlAsync($"UPDATE [Stages] SET [IsSentViaViber]={mark} WHERE [Id]={stageId}");

        }
    }
}
