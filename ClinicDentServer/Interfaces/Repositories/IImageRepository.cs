using ClinicDentServer.Models;
using System.Threading.Tasks;

namespace ClinicDentServer.Interfaces.Repositories
{
    public interface IImageRepository<T> : IDefaultRepository<T> where T : BaseModel
    {
        Task ChangeImageName(int imageId, string newName);
    }
}
