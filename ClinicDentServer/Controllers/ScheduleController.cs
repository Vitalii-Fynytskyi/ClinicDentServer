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
    public class ScheduleController : ControllerBase
    {
        [HttpGet("getRecordsForDay/{datetime}/{tableNumber}")]
        public async Task<ActionResult<IEnumerable<ScheduleDTO>>> GetRecordsForDay(string datetime,int tableNumber)
        {
            DateTime targetDt;
            if (DateTime.TryParse(datetime, out targetDt) == false)
            {
                return NotFound();
            }
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            ScheduleDTO[] schedules = await db.Schedules.Include(s => s.Patient).Include(s=>s.Doctor).Include(s=>s.Cabinet).Where(s => (targetDt.Date == s.StartDatetime.Date && s.CabinetId == tableNumber)).Select(s => new ScheduleDTO(s)).ToArrayAsync();
            db.Dispose();
            return Ok(schedules);
        }
        [HttpGet("getRelatedStages/{scheduleId}")]
        public async Task<ActionResult<IEnumerable<StageDTO>>> GetRelatedStages(int scheduleId)
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            StageDTO[] relatedStages = await db.Stages.Include(s=>s.Doctor).Where((s) => s.ScheduleId == scheduleId).Select(s => new StageDTO(s)).ToArrayAsync();
            db.Dispose();
            return Ok(relatedStages);
        }
        //PUT api/patients
        [HttpPut]
        public async Task<ActionResult<ScheduleDTO>> Put(ScheduleDTO scheduleDTO)
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            Schedule schedule = new Schedule(scheduleDTO);
            db.Schedules.Update(schedule);
            await db.SaveChangesAsync();
            db.Dispose();
            return Ok(scheduleDTO);
        }
        [HttpPost]
        public async Task<ActionResult<ScheduleDTO>> Post(ScheduleDTO schedule)
        {
            Schedule scheduleFromDTO = new Schedule(schedule);
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            db.Schedules.Add(scheduleFromDTO);
            await db.SaveChangesAsync();

            schedule.Id = scheduleFromDTO.Id;
            db.Dispose();
            return Ok(schedule);
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult<ScheduleDTO>> Delete(int id)
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            Schedule schedule = db.Schedules.FirstOrDefault(x => x.Id == id);
            if (schedule == null)
            {
                db.Dispose();
                return NotFound();
            }
            db.Schedules.Remove(schedule);
            await db.SaveChangesAsync();
            db.Dispose();
            return NoContent();
        }
        [HttpGet("getCabinets")]
        public async Task<ActionResult<IEnumerable<Cabinet>>> GetCabinets()
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);
            Cabinet[] cabinets =  await db.Cabinets.ToArrayAsync();
            db.Dispose();
            return Ok(cabinets);
        }
    }
}
