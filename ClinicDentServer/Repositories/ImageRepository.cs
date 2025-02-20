using ClinicDentServer.Interfaces.Repositories;
using ClinicDentServer.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ClinicDentServer.Repositories
{
    public class ImageRepository<T> : DefaultRepository<T>, IImageRepository<T> where T:Image
    {
        public ImageRepository(ClinicContext clinicContextToSet) : base(clinicContextToSet)
        {
        }

        public async Task ChangeImageName(int imageId, string newName)
        {
            await clinicContext.Database.ExecuteSqlRawAsync("UPDATE [Images] SET [FileName]={0} WHERE [Id]={1}", newName, imageId);
        }
    }
}
