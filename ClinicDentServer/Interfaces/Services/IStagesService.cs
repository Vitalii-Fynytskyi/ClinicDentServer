using ClinicDentServer.RequestCustomAnswers;
using ClinicDentServer.Requests;
using System.Threading.Tasks;

namespace ClinicDentServer.Interfaces.Services
{
    public interface IStagesService
    {
        //TODO move rest of controller actions here
        Task<PutStagesRequestAnswer> PutMany(PutStagesRequest putStagesRequest);
    }
}
