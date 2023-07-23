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
    public class StagesController : ControllerBase
    {
        [HttpGet("{patientId}")]
        public async Task<ActionResult<IEnumerable<StageDTO>>> Get(int patientId)
        {
            using (ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Patient patient = await db.Patients.Include(p => p.Stages).ThenInclude(p => p.Doctor).FirstOrDefaultAsync(x => x.Id == patientId);
                if (patient == null)
                {
                    return NotFound();
                }
                List<Stage> result = patient.Stages.OrderByDescending(s => s.StageDatetime).ThenByDescending(s => s.Id).ToList();
                List<StageDTO> resultDTO = new List<StageDTO>(result.Count);
                foreach (Stage s in result)
                {
                    resultDTO.Add(new StageDTO(s));
                }
                return Ok(resultDTO);
            }
        }
        [HttpGet("getPhotosForStage/{stageId}")]
        public async Task<ActionResult<IEnumerable<ImageDTO>>> GetPhotosForStage(int stageId)
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                var images = await db.Stages.AsNoTracking()
            .Where(s => s.Id == stageId)
            .SelectMany(s => s.Images)
            .Select(i => new ImageDTO
            {
                Id = i.Id,
                CompressedBytes = i.CompressedBytes,
                FileName = i.FileName,
                DoctorId = i.DoctorId.GetValueOrDefault()
            })
            .ToArrayAsync();
                return Ok(images);
            }

            
        }
        [HttpGet("addPhotoToStage/{photoId}/{stageId}")]
        public async Task<ActionResult<StageDTO>> AddPhotoToStage(int photoId, int stageId)
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Image image = db.Images.FirstOrDefault(x => x.Id == photoId);
                if (image == null)
                {
                    return NotFound();
                }
                Stage stage = db.Stages.Include(s => s.Doctor).FirstOrDefault(x => x.Id == stageId);
                stage.Images.Add(image);
                await db.SaveChangesAsync();
                return Ok(new StageDTO(stage));
            }
        }
        [HttpPost]
        public async Task<ActionResult<StageDTO>> Post(StageDTO stageDTO)
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Doctor doctor = db.Doctors.FirstOrDefault(d => d.Email == User.Identity.Name);
                Stage stageToDb = new Stage(stageDTO);
                db.Stages.Add(stageToDb);
                await db.SaveChangesAsync();
                stageDTO.Id = stageToDb.Id;
                stageDTO.DoctorName = doctor.Name;
                return Ok(stageDTO);
            }
        }
        [HttpPut]
        public async Task<ActionResult<StageDTO>> Put(StageDTO stageDTO)
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Stage stageToDb = new Stage(stageDTO);
                db.Stages.Update(stageToDb);
                await db.SaveChangesAsync();
                return Ok(stageDTO);
            }
        }
        [HttpDelete("{stageId}")]
        public async Task<ActionResult<StageDTO>> Delete(int stageId)
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Stage stage = db.Stages.Include(s => s.Doctor).FirstOrDefault(s => s.Id == stageId);
                if (stage == null)
                {
                    return NotFound();
                }
                db.Stages.Remove(stage);
                await db.SaveChangesAsync();
                StageDTO stageDTO = new StageDTO(stage);
                return Ok(stageDTO);
            }

            
        }
        [HttpDelete("removePhotoFromStage/{photoId}/{stageId}")]
        public async Task<ActionResult<ImageDTO>> RemovePhotoFromStage(int photoId,int stageId)
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Stage stage = db.Stages.Include(s => s.Images).Include(s => s.Doctor).Include(s => s.Patient).FirstOrDefault(x => x.Id == stageId);
                if (stage == null)
                {
                    return NotFound();
                }
                Image imageToDelete = stage.Images.Where(i => i.Id == photoId).FirstOrDefault();
                if (imageToDelete == null)
                {
                    return NotFound();
                }
                stage.Images.Remove(imageToDelete);
                await db.SaveChangesAsync();
                return Ok(new StageDTO(stage));
            }
        }
        [HttpGet("stageAssets")]
        public async Task<ActionResult<IEnumerable<StageAsset>>> GetStageAssets()
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                List<StageAsset> stageAssets = await db.StageAssets.ToListAsync();
                return Ok(stageAssets);
            }
        }
        [HttpPost("stageAsset")]
        [Produces("text/plain")]
        public async Task<ActionResult<int>> PostStageAsset(StageAsset stageAsset)
        {
            stageAsset.Id = 0;
            using (ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                db.StageAssets.Add(stageAsset);
                await db.SaveChangesAsync();
                return Ok(stageAsset.Id);
            }
           
        }
    }
}
