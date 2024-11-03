using ClinicDentServer.Interfaces.Repositories;
using ClinicDentServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicDentServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StatisticsController : ControllerBase
    {
        public IDefaultRepository<Schedule> scheduleRepository;
        public StatisticsController(IDefaultRepository<Schedule> scheduleRepositoryToSet)
        {
            scheduleRepository = scheduleRepositoryToSet;
        }

        [HttpGet("futureWorkingMinutes/{cabinetId:int}")]
        [Produces("text/plain")]
        public async Task<string> GetFutureWorkingMinutesAmount(int cabinetId)
        {
            DateTime now = DateTime.Now;
            int countMinutes = (int)scheduleRepository.dbSet.AsNoTracking().Where(s => s.CabinetId == cabinetId && s.StartDatetime > now).Select(s => new { startDatetime = s.StartDatetime, endDatetime = s.EndDatetime }).AsEnumerable().Sum(a => (a.endDatetime - a.startDatetime).TotalMinutes);
            return countMinutes.ToString();
        }
        [HttpGet("futureUniquePatientsAmount/{cabinetId:int}")]
        [Produces("text/plain")]
        public async Task<int> GetFutureUniquePatientsAmount(int cabinetId)
        {
            DateTime now = DateTime.Now;
            int uniquePatientsAmount = await scheduleRepository.dbSet.AsNoTracking().Where(s => s.CabinetId == cabinetId && s.StartDatetime > now).Select(s => s.PatientId.Value).Distinct().CountAsync();
            return uniquePatientsAmount;
        }
    }
}
