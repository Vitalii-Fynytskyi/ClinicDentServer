using ClinicDentServer.Dto;
using ClinicDentServer.Exceptions;
using ClinicDentServer.Models;
using ClinicDentServer.RequestCustomAnswers;
using ClinicDentServer.Requests;
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
    public class ScheduleController : ControllerBase
    {
        [HttpGet("getRecordsForDay/{datetime}/{tableNumber}")]
        public async Task<ActionResult<ScheduleRecordsForDayInCabinet>> GetRecordsForDay(string datetime,int tableNumber)
        {
            DateTime targetDt;
            if (DateTime.TryParse(datetime, out targetDt) == false)
            {
                throw new NotValidException($"'{datetime}' is not valid datetime");
            }
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                ScheduleDTO[] schedules = await db.Schedules.Include(s => s.Patient).Include(s => s.Doctor).Include(s => s.Cabinet).Where(s => (targetDt.Date == s.StartDatetime.Date && s.CabinetId == tableNumber)).Select(s => new ScheduleDTO(s)).ToArrayAsync();


                // Fetch relevant stages from the database first
                var stagesList = db.Stages
                    .Where(s => s.StageDatetime.Date==targetDt.Date)
                    .ToList();

                // Now, group and aggregate in-memory
                var stagesInfo = stagesList
                    .GroupBy(s => s.PatientId)
                    .ToDictionary(
                        g => g.Key,
                        g =>
                        (
                            isAllSentViaMessager: !g.Any(s => s.IsSentViaViber == false),
                            totalPaid: g.Sum(s => s.Payed),
                            totalPrice: g.Sum(s => s.Price)
                        )
                    );

                // Assign the aggregated data to the schedules
                for(int i =0;i<schedules.Length;i++)
                {
                    if (stagesInfo.TryGetValue(schedules[i].PatientId.Value, out var info))
                    {
                        schedules[i].StagesSentViaMessagerState = info.isAllSentViaMessager == true ?ScheduleIsSentViaMessagetState.AllSent : ScheduleIsSentViaMessagetState.CanSend;
                        schedules[i].StagesPaidSum = info.totalPaid;
                        schedules[i].StagesPriceSum = info.totalPrice;
                    }
                    else
                    {
                        // In case there are no stages for the given schedule
                        schedules[i].StagesSentViaMessagerState = ScheduleIsSentViaMessagetState.NoStages;
                        schedules[i].StagesPaidSum = 0;
                        schedules[i].StagesPriceSum = 0;
                    }
                }
                CabinetComment cabinetComment = db.CabinetComments.FirstOrDefault(c => c.Date == targetDt && c.CabinetId == tableNumber);
                string cabinetCommentStr = null;
                if(cabinetComment != null)
                {
                    cabinetCommentStr = cabinetComment.CommentText;
                }
                ScheduleRecordsForDayInCabinet scheduleRecordsForDayInCabinet = new ScheduleRecordsForDayInCabinet()
                {
                    Schedules = schedules,
                    CabinetComment = cabinetCommentStr
                };
                return Ok(scheduleRecordsForDayInCabinet);
            }
            
        }
        //PUT api/patients
        [HttpPut]
        public async Task<ActionResult<ScheduleDTO>> Put(ScheduleDTO scheduleDTO)
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Schedule schedule = new Schedule(scheduleDTO);
                db.Schedules.Update(schedule);
                await db.SaveChangesAsync();
                return Ok(scheduleDTO);
            }

            
        }
        [HttpPost]
        public async Task<ActionResult<ScheduleDTO>> Post(ScheduleDTO schedule)
        {
            Schedule scheduleFromDTO = new Schedule(schedule);
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                db.Schedules.Add(scheduleFromDTO);
                await db.SaveChangesAsync();

                schedule.Id = scheduleFromDTO.Id;
                return Ok(schedule);
            }
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Schedule schedule = db.Schedules.FirstOrDefault(x => x.Id == id);
                if (schedule == null)
                {
                    throw new NotFoundException($"Schedule record with id={id} cannot be found");
                }
                db.Schedules.Remove(schedule);
                await db.SaveChangesAsync();
                return NoContent();
            }

            
        }
        [HttpGet("getCabinets")]
        public async Task<ActionResult<IEnumerable<Cabinet>>> GetCabinets()
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Cabinet[] cabinets = await db.Cabinets.ToArrayAsync();
                return Ok(cabinets);
            }
            
        }
        [HttpPut("weekMoneySummary")]
        public async Task<ActionResult<WeekMoneySummaryRequestAnswer>> GetWeekMoneySummary(WeekMoneySummaryRequest r)
        {
            using (ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {

                int?[] schedulePatientIds = await db.Schedules.Where(s => s.CabinetId == r.CabinetId && s.StartDatetime.Date <= r.AnySunday.Date && s.StartDatetime.Date >= r.AnySunday.Date - TimeSpan.FromDays(6)).Select(s=>s.PatientId).ToArrayAsync();
                // Fetch relevant stages from the database first
                var stagesList = db.Stages
                    .Where(s => s.StageDatetime.Date <= r.AnySunday.Date && s.StageDatetime.Date >= r.AnySunday.Date - TimeSpan.FromDays(6))
                    .ToList();

                // Now, group and aggregate in-memory
                var stagesInfo = stagesList
                    .GroupBy(s => s.PatientId)
                    .ToDictionary(
                        g => g.Key,
                        g =>
                        (
                            totalPaid: g.Sum(s => s.Payed),
                            totalPrice: g.Sum(s => s.Price)
                        )
                    );
                WeekMoneySummaryRequestAnswer answer = new WeekMoneySummaryRequestAnswer();
                // Assign the aggregated data to the schedules
                for (int i = 0; i < schedulePatientIds.Length; i++)
                {
                    if (stagesInfo.TryGetValue(schedulePatientIds[i].Value, out var info))
                    {
                        answer.PaidSum = info.totalPaid;
                        answer.PriceSum = info.totalPrice;
                    }
                }
                return Ok(answer);
            }
        }
    }
}
