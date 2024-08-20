using ClinicDentServer.Exceptions;
using ClinicDentServer.Models;
using ClinicDentServer.RequestCustomAnswers;
using ClinicDentServer.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NumSharp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tensorflow;

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
                bool patientExists = await db.Patients.AnyAsync(p => p.Id == patientId);
                if (!patientExists)
                {
                    throw new NotFoundException($"Patient with ID= {patientId} not found.");
                }
                StageDTO[] resultDTO = db.Stages.AsNoTracking()
                                                    .Include(s => s.Doctor).Include(s=>s.ToothUnderObservation)
                                                    .Where(s => s.PatientId == patientId)
                                                    .OrderByDescending(s => s.StageDatetime)
                                                    .ThenByDescending(s => s.Id)
                                                    .Select(s => new StageDTO(s))
                                                    .ToArray();
                return Ok(resultDTO);
            }
        }
        /// <summary>
        /// Endpoint returns array of stages based on requested stage ids in body
        /// </summary>
        /// <param name="stagesIdStr">example: "1,2,3,4". It is stage ids separated by comma that will be returned</param>
        /// <returns></returns>
        [HttpPost("getMany")]
        [Consumes("text/plain")]
        public async Task<ActionResult<IEnumerable<StageDTO>>> GetMany([FromBody] string stagesIdStr)
        {
            using (ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                int[] stagesId = stagesIdStr.Split(',').Select(s => Int32.Parse(s)).ToArray();
                StageDTO[] resultDTO = await db.Stages.AsNoTracking()
                                                    .Include(s => s.Doctor).Include(s => s.ToothUnderObservation)
                                                    .Where(s => stagesId.Contains(s.Id))
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
                DateTime now = DateTime.Now;
                stageToDb.CreatedDateTime = now;
                stageToDb.LastModifiedDateTime = now;
                db.Stages.Add(stageToDb);
                db.SaveChanges();
                stageDTO.Id = stageToDb.Id;
                stageDTO.DoctorName = doctor.Name;
                stageDTO.CreatedDateTime= stageToDb.CreatedDateTime.ToString(Options.ExactDateTimePattern);
                stageDTO.LastModifiedDateTime = stageToDb.LastModifiedDateTime.ToString(Options.ExactDateTimePattern);
                return Ok(stageDTO);
            }
        }
        [HttpPut]
        public async Task<ActionResult> Put(StageDTO stageDTO)
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Stage existingStage = await db.Stages.AsNoTracking().FirstOrDefaultAsync(s=>s.Id==stageDTO.Id);
                Stage stageToDb = new Stage(stageDTO);
                if (existingStage.LastModifiedDateTime > stageToDb.LastModifiedDateTime)
                {
                    throw new ConflictException("Another process have updated the stage");
                }
                db.Stages.Update(stageToDb);
                db.SaveChanges();
                return NoContent();
            }
        }
        [HttpPut("putMany")]
        public async Task<ActionResult<PutStagesRequestAnswer>> PutMany(PutStagesRequest putStagesRequest)
        {
            //convert dtos to database entities
            List<Stage> stagesFromDTO = putStagesRequest.stageDTO.Select(dto => new Stage(dto)).ToList();
            List<int> stageIds = new List<int>(stagesFromDTO.Count);
            DateTime now = DateTime.Now;
            for(int i =0;i< stagesFromDTO.Count; i++)
            {
                stageIds.Add(stagesFromDTO[i].Id);
            }
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);
            try
            {
                Dictionary<int, DateTime> stageIdDateMap = await db.Stages
                .AsNoTracking()
                .Where(s => stageIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.LastModifiedDateTime);
                List<int> conflictedStageIds = new List<int>();
                for (int i = 0; i < stagesFromDTO.Count; i++)
                {
                    var stage = stagesFromDTO[i];
                    if (stageIdDateMap.TryGetValue(stage.Id, out var lastModifiedDateTime))
                    {
                        if (stage.LastModifiedDateTime < lastModifiedDateTime)
                        {
                            conflictedStageIds.Add(stage.Id);
                            stagesFromDTO.RemoveAt(i);
                            i--;
                        }
                    }
                }

                if (stagesFromDTO.Any())
                {
                    for (int i = 0; i < stagesFromDTO.Count; i++)
                    {
                        stagesFromDTO[i].LastModifiedDateTime = now;
                    }
                    db.Stages.UpdateRange(stagesFromDTO);
                    db.SaveChanges();
                }
                if(conflictedStageIds.Count > 0)
                {
                    return Conflict(new PutStagesRequestAnswer()
                    {
                        ConflictedStagesIds = conflictedStageIds,
                        NewLastModifiedDateTime = now.ToString(Options.ExactDateTimePattern)
                    });
                }
                else
                {
                    return Ok(new PutStagesRequestAnswer()
                    {
                        NewLastModifiedDateTime = now.ToString(Options.ExactDateTimePattern)
                    });
                }
            }
            finally
            {
                db.Dispose();
                StringBuilder stringBuilder = new StringBuilder(32);
                for(int i =0; i < stagesFromDTO.Count; i++)
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
