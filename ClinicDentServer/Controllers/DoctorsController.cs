using ClinicDentServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        //returns collection of doctors
        [HttpGet("getAll")]
        public async Task<ActionResult<IEnumerable<DoctorDTO>>> GetAll()
        {
            using(ClinicContext clinicContext = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                DoctorDTO[] doctors = await clinicContext.Doctors.AsNoTracking().Select(i => new DoctorDTO()
                {
                    Id = i.Id,
                    Name = i.Name,
                }).ToArrayAsync();
                return Ok(doctors);
            }
        }
    }
}
