using ClinicDentServer.Interfaces.Repositories;
using ClinicDentServer.Models;
using Microsoft.AspNetCore.Authorization;
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
    public class DoctorsController : ControllerBase
    {
        public IDefaultRepository<Doctor> doctorsRepository;
        public DoctorsController(IDefaultRepository<Doctor> doctorsRepositoryToSet)
        {
            doctorsRepository= doctorsRepositoryToSet;
        }
        //returns collection of doctors
        [HttpGet("getAll")]
        public async Task<ActionResult<IEnumerable<DoctorDTO>>> GetAll()
        {
            DoctorDTO[] doctors = await doctorsRepository.dbSet.AsNoTracking().Select(i => new DoctorDTO()
            {
                Id = i.Id,
                Name = i.Name,
            }).ToArrayAsync();
            return Ok(doctors);
        }
    }
}
