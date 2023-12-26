using ClinicDentServer.Models;
using ClinicDentServer.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                StageDTO[] resultDTO = await db.Stages.AsNoTracking()
                                                    .Include(s => s.Doctor).Include(s=>s.ToothUnderObservation)
                                                    .Where(s => s.PatientId == patientId)
                                                    .OrderByDescending(s => s.StageDatetime)
                                                    .ThenByDescending(s => s.Id)
                                                    .Select(s => new StageDTO(s))
                                                    .ToArrayAsync();
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
                Stage stage = db.Stages.Include(s => s.Doctor).Include(s => s.ToothUnderObservation).FirstOrDefault(x => x.Id == stageId);
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
                Doctor doctor = await db.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Email == User.Identity.Name);
                Stage stageToDb = new Stage(stageDTO);
                db.Stages.Add(stageToDb);
                db.SaveChanges();
                stageDTO.Id = stageToDb.Id;
                stageDTO.DoctorName = doctor.Name;
                return Ok(stageDTO);
            }
        }
        [HttpPut]
        public async Task<ActionResult> Put(StageDTO stageDTO)
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Stage stageToDb = new Stage(stageDTO);
                db.Stages.Update(stageToDb);
                await db.SaveChangesAsync();
                return NoContent();
            }
        }
        [HttpPut("putMany")]
        public async Task<ActionResult> PutMany(PutStagesRequest putStagesRequest)
        {
            //convert dtos to database entities
            List<Stage> dbStages = putStagesRequest.stageDTO.Select(dto => new Stage(dto)).ToList();
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);
            try
            {
                db.Stages.UpdateRange(dbStages);
                await db.SaveChangesAsync();
                return NoContent();
            }
            finally
            {
                db.Dispose();
                StringBuilder stringBuilder = new StringBuilder(32);
                for(int i =0; i < dbStages.Count; i++)
                {
                    if (putStagesRequest.stageDTO[i].OldPrice != putStagesRequest.stageDTO[i].Price || putStagesRequest.stageDTO[i].Payed != putStagesRequest.stageDTO[i].OldPayed || putStagesRequest.stageDTO[i].Expenses!= putStagesRequest.stageDTO[i].OldExpenses)
                    {
                        int expensesDifference = putStagesRequest.stageDTO[i].Expenses - putStagesRequest.stageDTO[i].OldExpenses;
                        int priceDifference = putStagesRequest.stageDTO[i].Price - putStagesRequest.stageDTO[i].OldPrice;
                        int payedDifference = putStagesRequest.stageDTO[i].Payed - putStagesRequest.stageDTO[i].OldPayed;

                        stringBuilder.Append($"{putStagesRequest.stageDTO[i].PatientId},{putStagesRequest.stageDTO[i].StageDatetime},{priceDifference},{payedDifference},{putStagesRequest.stageDTO[i].DoctorId},{expensesDifference}");
                    }
                }
                Program.TcpServer.SendToAll("stagePayInfoUpdated", stringBuilder.ToString());

            }
        }
        [HttpDelete("{stageId}")]
        public async Task<ActionResult> Delete(int stageId)
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);
            Stage stage = null;
            try
            {
                stage = await db.Stages.FirstOrDefaultAsync(s => s.Id == stageId);
                if (stage == null)
                {
                    return NotFound();
                }
                db.Stages.Remove(stage);
                db.SaveChanges();
                return NoContent();
            }
            finally
            {
                db.Dispose();
                if (stage != null)
                {
                    int priceDifference = -stage.Price;
                    int payedDifference = -stage.Payed;
                    int expensesDifference = -stage.Expenses;
                    string stagePayInfo = $"{stage.PatientId},{stage.StageDatetime.ToString(Options.DateTimePattern)},{priceDifference},{payedDifference},{stage.DoctorId},{expensesDifference}";
                    Program.TcpServer.SendToAll("stagePayInfoUpdated", stagePayInfo);
                }
            }
        }
        [HttpDelete("removePhotoFromStage/{photoId}/{stageId}")]
        public async Task<ActionResult<ImageDTO>> RemovePhotoFromStage(int photoId,int stageId)
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Stage stage = db.Stages.Include(s => s.Images).Include(s => s.Doctor).Include(s => s.Patient).Include(s => s.ToothUnderObservation).FirstOrDefault(x => x.Id == stageId);
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
                List<StageAsset> stageAssets = await db.StageAssets.AsNoTracking().ToListAsync();
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
        [HttpGet("sentViaMessager/{stageId:int}/{mark:int}")]
        public async Task<ActionResult> SentViaMessager(int stageId, int mark)
        {
            using (ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                await db.Database.ExecuteSqlRawAsync($"UPDATE [Stages] SET [IsSentViaViber]={mark} WHERE [Id]={stageId}");
                return Ok();
            }
        }
    }
}
