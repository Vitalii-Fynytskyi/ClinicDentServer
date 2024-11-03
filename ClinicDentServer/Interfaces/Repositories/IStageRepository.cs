using ClinicDentServer.Models;
using System.Threading.Tasks;

namespace ClinicDentServer.Interfaces.Repositories
{
    public interface IStageRepository<T> : IDefaultRepository<T> where T : BaseModel
    {
        Task SendViaMessager(int stageId, int mark);
    }
}
