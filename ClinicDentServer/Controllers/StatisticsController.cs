using ClinicDentServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicDentServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StatisticsController : ControllerBase
    {
        [HttpGet("futureWorkingMinutes/{cabinetId:int}")]
        [Produces("text/plain")]
        public async Task<string> GetFutureWorkingMinutesAmount(int cabinetId)
        {
            using (ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                DateTime now = DateTime.Now;
                int countMinutes = (int)db.Schedules.AsNoTracking().Where(s => s.CabinetId == cabinetId && s.StartDatetime>now).Select(s => new {startDatetime=s.StartDatetime, endDatetime=s.EndDatetime}).AsEnumerable().Sum(a => (a.endDatetime - a.startDatetime).TotalMinutes);
                return countMinutes.ToString();
            }
        }
        [HttpGet("futureUniquePatientsAmount/{cabinetId:int}")]
        [Produces("text/plain")]
        public async Task<int> GetFutureUniquePatientsAmount(int cabinetId)
        {
            using (ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                DateTime now = DateTime.Now;
                int uniquePatientsAmount = await db.Schedules.AsNoTracking().Where(s => s.CabinetId == cabinetId && s.StartDatetime > now).Select(s => s.PatientId.Value).Distinct().CountAsync();
                return uniquePatientsAmount;
            }
        }
    }
}
