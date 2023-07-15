using ClinicDentServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
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
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            Patient patient = await db.Patients.Include(p => p.Stages).ThenInclude(p => p.Doctor).FirstOrDefaultAsync(x => x.Id == patientId);
            if (patient == null)
            {
                db.Dispose();
                return NotFound();
            }
            List<Stage> result = patient.Stages.AsEnumerable().Select(x =>
            {
                DateTime dt;
                return new { valid = DateTime.TryParse(x.StageDatetime, out dt), date = dt, stage = x };
            }).OrderByDescending(x => x.date).ThenByDescending(x => x.stage.Id).Select(x => x.stage).ToList();
            List<StageDTO> resultDTO = new List<StageDTO>(result.Count);
            foreach (Stage s in result)
            {
                resultDTO.Add(new StageDTO(s));
            }
            db.Dispose();
            return Ok(resultDTO);
        }
        [HttpGet("getPhotosForStage/{stageId}")]
        public async Task<ActionResult<IEnumerable<ImageDTO>>> GetPhotosForStage(int stageId)
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

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
            db.Dispose();
            return Ok(images);
        }
        [HttpGet("addPhotoToStage/{photoId}/{stageId}")]
        public async Task<ActionResult<StageDTO>> AddPhotoToStage(int photoId, int stageId)
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            Image image = db.Images.FirstOrDefault(x => x.Id == photoId);
            if (image == null)
            {
                db.Dispose();
                return NotFound();
            }
            Stage stage = db.Stages.Include(s=>s.Doctor).FirstOrDefault(x => x.Id == stageId);
            stage.Images.Add(image);
            await db.SaveChangesAsync();
            db.Dispose();
            return Ok(new StageDTO(stage));
        }
        [HttpPost]
        public async Task<ActionResult<StageDTO>> Post(StageDTO stageDTO)
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            Doctor doctor = db.Doctors.FirstOrDefault(d => d.Email == User.Identity.Name);
            Stage stageToDb = new Stage(stageDTO);
            db.Stages.Add(stageToDb);
            await db.SaveChangesAsync();
            db.Dispose();
            stageDTO.Id = stageToDb.Id;
            stageDTO.DoctorName = doctor.Name;
            return Ok(stageDTO);
        }
        [HttpPut]
        public async Task<ActionResult<StageDTO>> Put(StageDTO stageDTO)
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            Stage stageToDb = new Stage(stageDTO);
            db.Stages.Update(stageToDb);
            await db.SaveChangesAsync();
            db.Dispose();
            return Ok(stageDTO);
        }
        [HttpDelete("{stageId}")]
        public async Task<ActionResult<StageDTO>> Delete(int stageId)
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            Stage stage = db.Stages.Include(s=>s.Doctor).FirstOrDefault(s => s.Id == stageId);
            if(stage == null)
            {
                db.Dispose();
                return NotFound();
            }
            db.Stages.Remove(stage);
            await db.SaveChangesAsync();
            StageDTO stageDTO = new StageDTO(stage);
            db.Dispose();
            return Ok(stageDTO);
        }
        [HttpDelete("removePhotoFromStage/{photoId}/{stageId}")]
        public async Task<ActionResult<ImageDTO>> RemovePhotoFromStage(int photoId,int stageId)
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            Stage stage = db.Stages.Include(s=>s.Images).Include(s=>s.Doctor).Include(s=>s.Patient).FirstOrDefault(x => x.Id == stageId);
            if (stage == null)
            {
                db.Dispose();
                return NotFound();
            }
            Image imageToDelete = stage.Images.Where(i => i.Id == photoId).FirstOrDefault();
            if(imageToDelete == null)
            {
                db.Dispose();
                return NotFound();
            }
            stage.Images.Remove(imageToDelete);
            await db.SaveChangesAsync();
            db.Dispose();
            return Ok(new StageDTO(stage));
        }
        [HttpGet("stageAssets")]
        public async Task<ActionResult<IEnumerable<StageAsset>>> GetStageAssets()
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            List<StageAsset> stageAssets = await db.StageAssets.ToListAsync();
            db.Dispose();
            return Ok(stageAssets);
        }
        [HttpPost("stageAsset")]
        [Produces("text/plain")]
        public async Task<ActionResult<int>> Post(StageAsset stageAsset)
        {
            stageAsset.Id = 0;
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);
            try
            {
                db.StageAssets.Add(stageAsset);
                await db.SaveChangesAsync();
            }
            finally
            {
                db?.Dispose();
            }

            return Ok(stageAsset.Id);
        }
    }
}
