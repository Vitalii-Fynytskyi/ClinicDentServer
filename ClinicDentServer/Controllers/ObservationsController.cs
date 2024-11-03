using ClinicDentServer.Exceptions;
using ClinicDentServer.Interfaces.Repositories;
using ClinicDentServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicDentServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ObservationsController : ControllerBase
    {
        public IDefaultRepository<ToothUnderObservation> toothObservationRepository;
        public ObservationsController(IDefaultRepository<ToothUnderObservation> toothObservationRepositoryToSet)
        {
            toothObservationRepository = toothObservationRepositoryToSet;
        }
        [HttpGet("tooth/{id}")]
        public async Task<ActionResult<ToothUnderObservationDTO>> GetTooth(int id)
        {
            ToothUnderObservationDTO toothUnderObservation = await toothObservationRepository.dbSet.AsNoTracking().Include(t => t.Stage).ThenInclude(s => s.Patient).Where(t => t.Id == id).Select(t => new ToothUnderObservationDTO(t, t.Stage.Patient.Name)).FirstOrDefaultAsync();
            if (toothUnderObservation == null)
            {
                throw new NotFoundException($"Tooth observation with id={id} cannot be found");
            }
            return Ok(toothUnderObservation);
        }

        [HttpGet("allTooth")]
        public async Task<ActionResult<List<ToothUnderObservationDTO>>> GetAllTooth()
        {
            return await toothObservationRepository.dbSet.Include(t => t.Stage).ThenInclude(s => s.Patient).Select(t => new ToothUnderObservationDTO(t, t.Stage.Patient.Name)).ToListAsync();

        }

        [HttpPost("tooth")]
        [Produces("text/plain")]

        public async Task<ActionResult<string>> PostTooth(ToothUnderObservationDTO toothDTO)
        {
            ToothUnderObservation toothFromDTO = new ToothUnderObservation(toothDTO);
            await toothObservationRepository.Add(toothFromDTO);
            return Ok(toothFromDTO.Id.ToString());
        }
        [HttpPut("tooth")]
        public async Task<ActionResult> PutTooth(ToothUnderObservationDTO toothDTO)
        {
            ToothUnderObservation toothFromDTO = new ToothUnderObservation(toothDTO);

            await toothObservationRepository.Update(toothFromDTO);
            return NoContent();
        }
        [HttpDelete("tooth/{id}")]
        public async Task<ActionResult> DeleteTooth(int id)
        {
            ToothUnderObservation toothUnderObservation = await toothObservationRepository.dbSet.FirstOrDefaultAsync(x => x.Id == id);
            if (toothUnderObservation == null)
            {
                throw new NotFoundException($"Tooth observation with id={id} cannot be found");
            }

            await toothObservationRepository.Remove(toothUnderObservation);
            return NoContent();
        }
    }
}
