﻿using ClinicDentServer.Dto;
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
        //TODO rewrite with stored procedure usage
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
                ScheduleDTO[] schedules = await db.Schedules.AsNoTracking().Include(s => s.Patient).Include(s => s.Doctor).Include(s => s.Cabinet).Where(s => (targetDt.Date == s.StartDatetime.Date && s.CabinetId == tableNumber)).Select(s => new ScheduleDTO(s)).ToArrayAsync();

                // Fetch relevant stages from the database first
                var stagesList = db.Stages.AsNoTracking()
                    .Where(s => s.StageDatetime.Date==targetDt.Date)
                    .ToList();
                var stagesInfo = stagesList
                    .GroupBy(s => s.PatientId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.GroupBy(s => s.DoctorId)
                              .ToDictionary(
                                  gg => gg.Key,
                                  gg => (
                                      isAllSentViaMessager: !gg.Any(s => s.IsSentViaViber == false),
                                      totalPaid: gg.Sum(s => s.Payed),
                                      totalPrice: gg.Sum(s => s.Price),
                                      totalExpenses: gg.Sum(s=>s.Expenses)
                                  )
                              )
                    );
                // Assign the aggregated data to the schedules
                for (int i = 0; i < schedules.Length; i++)
                {
                    var patientId = schedules[i].PatientId.Value;

                    if (stagesInfo.TryGetValue(patientId, out var patientInfo))
                    {
                        foreach (var doctorInfo in patientInfo)
                        {
                            var doctorId = doctorInfo.Key;
                            var info = doctorInfo.Value;

                            schedules[i].DoctorIds.Add(doctorId);
                            schedules[i].StagesPaidSum.Add(info.totalPaid);
                            schedules[i].StagesPriceSum.Add(info.totalPrice);
                            schedules[i].StagesExpensesSum.Add(info.totalExpenses);

                        }

                        // Check if all the doctor's stages are sent via messager; if even one doctor has not sent all, then set the state to "CanSend"
                        if (patientInfo.Values.All(info => info.isAllSentViaMessager))
                        {
                            schedules[i].StagesSentViaMessagerState = ScheduleIsSentViaMessagetState.AllSent;
                        }
                        else
                        {
                            schedules[i].StagesSentViaMessagerState = ScheduleIsSentViaMessagetState.CanSend;
                        }
                    }
                    else
                    {
                        // In case there are no stages for the given schedule
                        schedules[i].StagesSentViaMessagerState = ScheduleIsSentViaMessagetState.NoStages;
                    }
                }

                CabinetComment cabinetComment = db.CabinetComments.AsNoTracking().FirstOrDefault(c => c.Date == targetDt && c.CabinetId == tableNumber);
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
        [HttpGet("getPatientFutureAppointments/{patientId:int}")]
        public async Task<ActionResult<IEnumerable<ScheduleDTO>>> GetPatientFutureAppointments(int patientId)
        {
            using (ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                ScheduleDTO[] schedules = await db.Schedules.AsNoTracking().Include(s => s.Patient).Include(s => s.Doctor).Include(s => s.Cabinet).Where(s => s.PatientId==patientId && s.StartDatetime.Date>=DateTime.Now.Date).OrderBy(s=>s.StartDatetime).Select(s => new ScheduleDTO(s)).ToArrayAsync();
                return Ok(schedules);
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
                Schedule schedule = await db.Schedules.FirstOrDefaultAsync(x => x.Id == id);
                if (schedule == null)
                {
                    throw new NotFoundException($"Schedule record with id={id} cannot be found");
                }
                db.Schedules.Remove(schedule);
                db.SaveChanges();
                return NoContent();
            }

            
        }
        [HttpGet("getCabinets")]
        public async Task<ActionResult<IEnumerable<Cabinet>>> GetCabinets()
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Cabinet[] cabinets = await db.Cabinets.AsNoTracking().ToArrayAsync();
                return Ok(cabinets);
            }
            
        }
        [HttpPut("weekMoneySummary")]
        public async Task<ActionResult<WeekMoneySummaryRequestAnswer>> GetWeekMoneySummary(WeekMoneySummaryRequest r)
        {
            using (ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {

                // Fetch relevant stages from the database first
                var stagesList = await db.Stages.AsNoTracking()
                    .Where(s => s.StageDatetime.Date <= r.AnySunday.Date && s.StageDatetime.Date >= r.AnySunday.Date - TimeSpan.FromDays(6))
                    .ToArrayAsync();

                var groupedStages = stagesList
                            .GroupBy(s => s.DoctorId)
                            .ToList();
                WeekMoneySummaryRequestAnswer weekMoneySummaryRequestAnswer = new WeekMoneySummaryRequestAnswer();

                foreach (var group in groupedStages)
                {
                    int doctorId = group.Key;
                    int totalPaidForDoctor = group.Sum(s => s.Payed);
                    int totalPriceForDoctor = group.Sum(s => s.Price);
                    int totalExpensesForDoctor = group.Sum(s => s.Expenses);


                    weekMoneySummaryRequestAnswer.DoctorIds.Add(doctorId);
                    weekMoneySummaryRequestAnswer.StagesPaidSum.Add(totalPaidForDoctor);
                    weekMoneySummaryRequestAnswer.StagesPriceSum.Add(totalPriceForDoctor);
                    weekMoneySummaryRequestAnswer.StagesExpensesSum.Add(totalExpensesForDoctor);
                }

                return Ok(weekMoneySummaryRequestAnswer);
            }
        }
    }
}
