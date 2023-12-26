using ClinicDentServer.Exceptions;
using ClinicDentServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        [HttpGet("tooth/{id}")]
        public async Task<ActionResult<ToothUnderObservationDTO>> GetTooth(int id)
        {
            using (ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                ToothUnderObservationDTO toothUnderObservation = await db.ToothUnderObservations.AsNoTracking().Include(t => t.Stage).ThenInclude(s => s.Patient).Where(t => t.Id == id).Select(t => new ToothUnderObservationDTO(t, t.Stage.Patient.Name)).FirstOrDefaultAsync();
                if (toothUnderObservation == null)
                {
                    throw new NotFoundException($"Tooth observation with id={id} cannot be found");
                }
                return Ok(toothUnderObservation);
            }
        }

        [HttpGet("allTooth")]
        public async Task<ActionResult<List<ToothUnderObservationDTO>>> GetAllTooth()
        {
            using (ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                return await db.ToothUnderObservations.Include(t=>t.Stage).ThenInclude(s=>s.Patient).Select(t=>new ToothUnderObservationDTO(t,t.Stage.Patient.Name)).ToListAsync();
            }
        }

        [HttpPost("tooth")]
        [Produces("text/plain")]

        public async Task<ActionResult<string>> PostTooth(ToothUnderObservationDTO toothDTO)
        {
            using (ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                ToothUnderObservation toothFromDTO = new ToothUnderObservation(toothDTO);
                db.ToothUnderObservations.Add(toothFromDTO);

                await db.SaveChangesAsync();
                return Ok(toothFromDTO.Id.ToString());
            }
        }
        [HttpPut("tooth")]
        public async Task<ActionResult> PutTooth(ToothUnderObservationDTO toothDTO)
        {
            using (ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                ToothUnderObservation toothFromDTO = new ToothUnderObservation(toothDTO);

                db.ToothUnderObservations.Update(toothFromDTO);
                await db.SaveChangesAsync();
                return NoContent();
            }
        }
        [HttpDelete("tooth/{id}")]
        public async Task<ActionResult> DeleteTooth(int id)
        {
            using (ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                ToothUnderObservation toothUnderObservation = await db.ToothUnderObservations.FirstOrDefaultAsync(x => x.Id == id);
                if (toothUnderObservation == null)
                {
                    throw new NotFoundException($"Tooth observation with id={id} cannot be found");
                }

                db.ToothUnderObservations.Remove(toothUnderObservation);
                //db.Database.ExecuteSqlRaw($"UPDATE [Stages] SET []='{r.CurePlan}' WHERE [Id]={r.PatientId}");
                db.SaveChanges();
                return NoContent();
            }


        }
    }
}
