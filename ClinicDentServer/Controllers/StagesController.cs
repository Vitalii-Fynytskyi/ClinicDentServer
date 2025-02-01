using ClinicDentServer.Exceptions;
using ClinicDentServer.Interfaces.Repositories;
using ClinicDentServer.Interfaces.Services;
using ClinicDentServer.Models;
using ClinicDentServer.RequestCustomAnswers;
using ClinicDentServer.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicDentServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StagesController : ControllerBase
    {
        Lazy<IStageRepository<Stage>> stageRepository;
        Lazy<IDefaultRepository<StageAsset>> stageAssetRepository;

        Lazy<IDefaultRepository<Patient>> patientRepository;
        Lazy<IDefaultRepository<Tooth>> teethRepository;

        Lazy<IImageRepository<Image>> imageRepository;

        Lazy<IDefaultRepository<Doctor>> doctorRepository;
        Lazy<IStagesService> stagesService;


        public StagesController(Lazy<IStageRepository<Stage>> stageRepositoryToSet, Lazy<IDefaultRepository<Patient>> patientRepositoryToSet, Lazy<IImageRepository<Image>> imageRepositoryToSet, Lazy<IDefaultRepository<Doctor>> doctorRepositoryToSet, Lazy<IDefaultRepository<Tooth>> teethRepositoryToSet, Lazy<IStagesService> stagesServiceToSet, Lazy<IDefaultRepository<StageAsset>> stageAssetRepositoryToSet)
        {
            stageRepository = stageRepositoryToSet;
            patientRepository = patientRepositoryToSet;
            imageRepository = imageRepositoryToSet;
            doctorRepository = doctorRepositoryToSet;
            teethRepository=teethRepositoryToSet;
            stagesService = stagesServiceToSet;
            stageAssetRepository = stageAssetRepositoryToSet;
        }
        [HttpGet("{patientId}")]
        public async Task<ActionResult<IEnumerable<StageDTO>>> Get(int patientId)
        {
            bool patientExists = await patientRepository.Value.dbSet.AnyAsync(p => p.Id == patientId);
            if (!patientExists)
            {
                throw new NotFoundException($"Patient with ID= {patientId} not found.");
            }
            StageDTO[] resultDTO = stageRepository.Value.dbSet.AsNoTracking()
                                                .Include(s => s.Doctor).Include(s => s.ToothUnderObservation).Include(s => s.Teeth)
                                                .Where(s => s.PatientId == patientId)
                                                .OrderByDescending(s => s.StageDatetime)
                                                .ThenByDescending(s => s.Id)
                                                .Select(s => new StageDTO(s))
                                                .ToArray();
            return Ok(resultDTO);
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
            int[] stagesId = stagesIdStr.Split(',').Select(s => Int32.Parse(s)).ToArray();
            StageDTO[] resultDTO = await stageRepository.Value.dbSet.AsNoTracking()
                                                .Include(s => s.Doctor).Include(s => s.ToothUnderObservation).Include(s => s.Teeth)
                                                .Where(s => stagesId.Contains(s.Id))
                                                .Select(s => new StageDTO(s))
                                                .ToArrayAsync();
            return Ok(resultDTO);
        }
        [HttpGet("getPhotosForStage/{stageId}")]
        public async Task<ActionResult<IEnumerable<ImageDTO>>> GetPhotosForStage(int stageId)
        {
            var images = await stageRepository.Value.dbSet.AsNoTracking()
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
        [HttpGet("addPhotoToStage/{photoId}/{stageId}")]
        public async Task<ActionResult<StageDTO>> AddPhotoToStage(int photoId, int stageId)
        {
            Image image = imageRepository.Value.dbSet.FirstOrDefault(x => x.Id == photoId);
            if (image == null)
            {
                return NotFound();
            }
            Stage stage = stageRepository.Value.dbSet.Include(s => s.Doctor).Include(s => s.ToothUnderObservation).Include(s => s.Teeth).FirstOrDefault(x => x.Id == stageId);
            stage.Images.Add(image);
            await stageRepository.Value.Update(stage);
            return Ok(new StageDTO(stage));
        }
        [HttpPost]
        public async Task<ActionResult<StageDTO>> Post(StageDTO stageDTO)
        {
            Doctor doctor = await doctorRepository.Value.dbSet.FirstOrDefaultAsync(d => d.Email == User.Identity.Name);
            Stage stageToDb = new Stage(stageDTO);
            DateTime now = DateTime.Now;
            stageToDb.CreatedDateTime = now;
            stageToDb.LastModifiedDateTime = now;

            var toothIds = stageDTO.TeethNumbers;
            var teeth = teethRepository.Value.dbSet.Where(t => toothIds.Contains((byte)t.Id)).ToList();

            stageToDb.Teeth = teeth;

            await stageRepository.Value.Add(stageToDb);
            stageDTO.Id = stageToDb.Id;
            stageDTO.DoctorName = doctor.Name;
            stageDTO.CreatedDateTime = stageToDb.CreatedDateTime.ToString(Options.ExactDateTimePattern);
            stageDTO.LastModifiedDateTime = stageToDb.LastModifiedDateTime.ToString(Options.ExactDateTimePattern);
            return Ok(stageDTO);
        }
        [HttpPut]
        public async Task<ActionResult> Put(StageDTO stageDTO)
        {
            DateTime? lastModifiedDateTime = null;
            Stage existingStage = await stageRepository.Value.dbSet.Include(s => s.Teeth).FirstOrDefaultAsync(s => s.Id == stageDTO.Id);
            if (stageDTO.LastModifiedDateTime != null)
            {
                bool isValid = DateTime.TryParseExact(stageDTO.LastModifiedDateTime, Options.ExactDateTimePattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result);
                if (isValid)
                {
                    lastModifiedDateTime = result;
                }
                else
                {
                    throw new NotValidException($"'{stageDTO.LastModifiedDateTime}' datetime is not in correct format");
                }
            }
            if (existingStage.LastModifiedDateTime > lastModifiedDateTime)
            {
                throw new ConflictException("Another process have updated the stage");
            }
            existingStage.UpdateFromDTO(stageDTO);
            var toothIds = stageDTO.TeethNumbers;
            var teeth = teethRepository.Value.dbSet.Where(t => toothIds.Contains((byte)t.Id)).ToList();

            // Associate the existing Teeth with the Stage
            existingStage.Teeth = teeth;

            await stageRepository.Value.Update(existingStage);
            return NoContent();
        }
        [HttpPut("putMany")]
        public async Task<ActionResult<PutStagesRequestAnswer>> PutMany(PutStagesRequest putStagesRequest)
        {
            PutStagesRequestAnswer answer = await stagesService.Value.PutMany(putStagesRequest);
            if (answer.ConflictedStagesIds != null && answer.ConflictedStagesIds.Any())
            {
                return Conflict(answer);
            }
            return Ok(answer);
        }
        [HttpDelete("{stageId}")]
        public async Task<ActionResult> Delete(int stageId)
        {
            Stage stage = null;
            try
            {
                stage = await stageRepository.Value.dbSet.FirstOrDefaultAsync(s => s.Id == stageId);
                if (stage == null)
                {
                    return NotFound();
                }
                await stageRepository.Value.Remove(stage);
                return NoContent();
            }
            finally
            {
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
            Stage stage = stageRepository.Value.dbSet.Include(s => s.Images).Include(s => s.Doctor).Include(s => s.Patient).Include(s => s.ToothUnderObservation).Include(s => s.Teeth).FirstOrDefault(x => x.Id == stageId);
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
            await stageRepository.Value.Update(stage);
            return Ok(new StageDTO(stage));
        }
        [HttpGet("stageAssets")]
        public async Task<ActionResult<IEnumerable<StageAsset>>> GetStageAssets()
        {
            List<StageAsset> stageAssets = await stageAssetRepository.Value.dbSet.AsNoTracking().ToListAsync();
            return Ok(stageAssets);
        }
        [HttpPost("stageAsset")]
        [Produces("text/plain")]
        public async Task<ActionResult<int>> PostStageAsset(StageAsset stageAsset)
        {
            stageAsset.Id = 0;
            await stageAssetRepository.Value.Add(stageAsset);
            return Ok(stageAsset.Id);

        }
        [HttpGet("sentViaMessager/{stageId:int}/{mark:int}")]
        public async Task<ActionResult> SentViaMessager(int stageId, int mark)
        {
            using (ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                await stageRepository.Value.SendViaMessager(stageId, mark);
                return Ok();
            }
        }
    }
}
