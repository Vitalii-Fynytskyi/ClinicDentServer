using ClinicDentServer.Exceptions;
using ClinicDentServer.Models;
using ClinicDentServer.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    public class PatientsController : ControllerBase
    {
        [HttpGet("{id}")]
        public async Task<ActionResult<PatientDTO>> Get(int id)
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Patient patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (patient == null)
                {
                    throw new NotFoundException($"Patient with id={id} cannot be found");
                }
                return Ok(new PatientDTO(patient));
            }
        }
        
        //POST api/patients
        [HttpPost]
        public async Task<ActionResult<PatientDTO>> Post(PatientDTO patient)
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Patient patientFromDTO = new Patient(patient);
                db.Patients.Add(patientFromDTO);

                await db.SaveChangesAsync();
                patient.Id = patientFromDTO.Id;
                return Ok(patient);
            }
        }
        //PUT api/patients
        [HttpPut]
        public async Task<ActionResult<PatientDTO>> Put(PatientDTO patient)
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Patient patientFromDTO = new Patient(patient);
                db.Patients.Update(patientFromDTO);
                await db.SaveChangesAsync();
                return NoContent();
            }

            
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Patient patient = await db.Patients.FirstOrDefaultAsync(x => x.Id == id);
                if (patient == null)
                {
                    throw new NotFoundException($"Patient with id={id} cannot be found");
                }
                db.Patients.Remove(patient);
                db.SaveChanges();
                return NoContent();
            }

            
        }
        [HttpPut("changeCurePlan")]
        public async Task<ActionResult> ChangeCurePlan(ChangeCurePlanRequest r)
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                db.Database.ExecuteSqlRaw($"UPDATE [Patients] SET [CurePlan]='{r.CurePlan}' WHERE [Id]={r.PatientId}");
                return Ok();
            }
        }


        [HttpGet("{status}/{sortDescription}/{page}/{patientsPerPage}/{searchText}")]
        public async Task<ActionResult<PatientsToClient>> Get(string status, string sortDescription, int page, int patientsPerPage, string searchText)
        {
            if (page <= 0)
            {
                throw new NotValidException($"Page='{page}'; Page required to be greater than 0");
            }
            if (searchText == "<null>") //param that means no filter
                searchText = "";
            PatientsToClient result = new PatientsToClient();
            Patient[] patientsToReturn = null;
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                double countPages = await db.Patients.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status))).CountAsync() / Convert.ToDouble(patientsPerPage);
                if (countPages - Math.Truncate(countPages) != 0)
                {
                    countPages = Math.Ceiling(countPages);
                }
                switch (sortDescription)
                {
                    case "Ім'я: від А до Я":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status))).OrderBy(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Ім'я: від Я до А":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status))).OrderByDescending(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "За замовчуванням":
                        patientsToReturn = db.Patients.AsNoTracking()
                            .Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)))
                            .Select(p => new
                            {
                                Patient = p,
                                LastStageTime = p.Stages.Max(s => s.StageDatetime) // Gets the date of the latest stage
                            })
                            .OrderByDescending(p => p.LastStageTime) // Orders by the date of the latest stage
                            .Select(p => p.Patient) // Gets only the patient objects
                            .ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "За замовчуванням навпаки":
                        patientsToReturn = db.Patients.AsNoTracking()
                            .Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)))
                            .Select(p => new
                            {
                                Patient = p,
                                LastStageTime = p.Stages.Max(s => s.StageDatetime) // Gets the date of the latest stage
                            })
                            .OrderBy(p => p.LastStageTime) // Orders by the date of the latest stage
                            .Select(p => p.Patient) // Gets only the patient objects
                            .ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Дата реєстрації: спочатку недавні":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)))).AsEnumerable().Select(x =>
                        {
                            DateTime dt;
                            return new { valid = DateTime.TryParse(x.RegisterDate, out dt), date = dt, patient = x };
                        }).OrderByDescending(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Дата реєстрації: спочатку старіші":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)))).AsEnumerable().Select(x =>
                        {
                            DateTime dt;
                            if (DateTime.TryParse(x.RegisterDate, out dt) == false)
                            {
                                dt = new DateTime(2500, 1, 1);
                            }
                            return new { date = dt, patient = x };
                        }).OrderBy(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Вік: спочатку молодші":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)))).AsEnumerable().Select(x =>
                        {
                            DateTime dt;
                            int birthYear;
                            if (DateTime.TryParse(x.Birthdate, out dt) == false)
                            {
                                if (Int32.TryParse(x.Birthdate, out birthYear) == false)
                                {
                                    dt = new DateTime(1800, 1, 1);
                                }
                                else
                                {
                                    dt = new DateTime(birthYear, 1, 1);
                                }
                            }
                            return new { date = dt, patient = x };
                        }).OrderByDescending(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Вік: спочатку старші":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)))).AsEnumerable().Select(x =>
                        {
                            DateTime dt;
                            int birthYear;
                            if (DateTime.TryParse(x.Birthdate, out dt) == false)
                            {
                                if (Int32.TryParse(x.Birthdate, out birthYear) == false)
                                {
                                    dt = new DateTime(2500, 1, 1);
                                }
                                else
                                {
                                    dt = new DateTime(birthYear, 1, 1);
                                }
                            }
                            return new { date = dt, patient = x };
                        }).OrderBy(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                }
                result.CountPages = (int)countPages;
                PatientDTO[] patientsDTO = new PatientDTO[patientsToReturn.Length];
                for (int i = 0; i < patientsToReturn.Length; ++i)
                {
                    patientsDTO[i] = new PatientDTO(patientsToReturn[i]);
                }
                result.Patients = patientsDTO;
                return Ok(result);
            }
        }
        [HttpGet("{status}/{sortDescription}/{page}/{patientsPerPage}/{searchText}/{doctorId}")]
        public async Task<ActionResult<PatientsToClient>> Get(string status, string sortDescription, int page, int patientsPerPage, string searchText, int doctorId)
        {
            if (page <= 0)
            {
                throw new NotValidException($"Page='{page}'; Page required to be greater than 0");
            }
            using (ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Doctor doctor = await db.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Id == doctorId);
                if (doctor == null)
                {
                    throw new NotFoundException($"Doctor with id={doctorId} cannot be found");
                }
                if (searchText == "<null>") //param that means no filter
                    searchText = "";
                PatientsToClient result = new PatientsToClient();
                Patient[] patientsToReturn = null;

                double countPages = db.Patients.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any())).Count() / Convert.ToDouble(patientsPerPage);
                if (countPages - Math.Truncate(countPages) != 0)
                {
                    countPages = Math.Ceiling(countPages);
                }
                switch (sortDescription)
                {
                    case "Ім'я: від А до Я":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any())).OrderBy(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Ім'я: від Я до А":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any())).OrderByDescending(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "За замовчуванням":
                        patientsToReturn = db.Patients.AsNoTracking()
                            .Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))
                            .Select(p => new
                            {
                                Patient = p,
                                LastStageTime = p.Stages.Where(s => s.DoctorId == doctorId).Max(s => s.StageDatetime) // Gets the date of the latest stage
                            })
                            .OrderByDescending(p => p.LastStageTime) // Orders by the date of the latest stage
                            .Select(p => p.Patient) // Gets only the patient objects
                            .ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "За замовчуванням навпаки":
                        patientsToReturn = db.Patients.AsNoTracking()
                            .Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))
                            .Select(p => new
                            {
                                Patient = p,
                                LastStageTime = p.Stages.Where(s => s.DoctorId == doctorId).Max(s => s.StageDatetime) // Gets the date of the latest stage
                            })
                            .OrderBy(p => p.LastStageTime) // Orders by the date of the latest stage
                            .Select(p => p.Patient) // Gets only the patient objects
                            .ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Дата реєстрації: спочатку недавні":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))).AsEnumerable().Select(x =>
                        {
                            DateTime dt;
                            return new { valid = DateTime.TryParse(x.RegisterDate, out dt), date = dt, patient = x };
                        }).OrderByDescending(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Дата реєстрації: спочатку старіші":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))).AsEnumerable().Select(x =>
                        {
                            DateTime dt;
                            if (DateTime.TryParse(x.RegisterDate, out dt) == false)
                            {
                                dt = new DateTime(2500, 1, 1);
                            }
                            return new { date = dt, patient = x };
                        }).OrderBy(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Вік: спочатку молодші":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))).AsEnumerable().Select(x =>
                        {
                            DateTime dt;
                            int birthYear;
                            if (DateTime.TryParse(x.Birthdate, out dt) == false)
                            {
                                if (Int32.TryParse(x.Birthdate, out birthYear) == false)
                                {
                                    dt = new DateTime(1800, 1, 1);
                                }
                                else
                                {
                                    dt = new DateTime(birthYear, 1, 1);
                                }
                            }
                            return new { date = dt, patient = x };
                        }).OrderByDescending(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Вік: спочатку старші":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))).AsEnumerable().Select(x =>
                        {
                            DateTime dt;
                            int birthYear;
                            if (DateTime.TryParse(x.Birthdate, out dt) == false)
                            {
                                if (Int32.TryParse(x.Birthdate, out birthYear) == false)
                                {
                                    dt = new DateTime(2500, 1, 1);
                                }
                                else
                                {
                                    dt = new DateTime(birthYear, 1, 1);
                                }
                            }
                            return new { date = dt, patient = x };
                        }).OrderBy(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                }
                result.CountPages = (int)countPages;
                PatientDTO[] patientsDTO = new PatientDTO[patientsToReturn.Length];
                for (int i = 0; i < patientsToReturn.Length; ++i)
                {
                    patientsDTO[i] = new PatientDTO(patientsToReturn[i]);
                }
                result.Patients = patientsDTO;
                return Ok(result);
            }
        }
        
        ///Getters for debtors
        [HttpGet("debtors/{sortDescription}/{page}/{patientsPerPage}/{searchText}")]
        public async Task<ActionResult<PatientsToClient>> GetDebtors(string sortDescription, int page, int patientsPerPage, string searchText)
        {

            if (page <= 0)
            {
                throw new NotValidException($"Page='{page}'; Page required to be greater than 0");
            }
            if (searchText == "<null>") //param that means no filter
                searchText = "";
            PatientsToClient result = new PatientsToClient();
            Patient[] patientsToReturn = null;
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                double countPages = await db.Patients.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed))).CountAsync() / Convert.ToDouble(patientsPerPage);
                if (countPages - Math.Truncate(countPages) != 0)
                {
                    countPages = Math.Ceiling(countPages);
                }
                switch (sortDescription)
                {
                    case "Ім'я: від А до Я":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed))).OrderBy(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Ім'я: від Я до А":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed))).OrderByDescending(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "За замовчуванням":
                        patientsToReturn = db.Patients.AsNoTracking().
                            Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)))
                            .Select(p => new
                            {
                                Patient = p,
                                LastStageTime = p.Stages.Max(s => s.StageDatetime) // Gets the date of the latest stage
                            })
                            .OrderByDescending(p => p.LastStageTime) // Orders by the date of the latest stage
                            .Select(p => p.Patient) // Gets only the patient objects
                            .ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "За замовчуванням навпаки":
                        patientsToReturn = db.Patients.AsNoTracking().
                            Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)))
                            .Select(p => new
                            {
                                Patient = p,
                                LastStageTime = p.Stages.Max(s => s.StageDatetime) // Gets the date of the latest stage
                            })
                            .OrderBy(p => p.LastStageTime) // Orders by the date of the latest stage
                            .Select(p => p.Patient) // Gets only the patient objects
                            .ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Дата реєстрації: спочатку недавні":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)))).AsEnumerable().Select(x =>
                        {
                            DateTime dt;
                            return new { valid = DateTime.TryParse(x.RegisterDate, out dt), date = dt, patient = x };
                        }).OrderByDescending(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Дата реєстрації: спочатку старіші":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)))).AsEnumerable().Select(x =>
                        {
                            DateTime dt;
                            if (DateTime.TryParse(x.RegisterDate, out dt) == false)
                            {
                                dt = new DateTime(2500, 1, 1);
                            }
                            return new { date = dt, patient = x };
                        }).OrderBy(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Вік: спочатку молодші":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)))).AsEnumerable().Select(x =>
                        {
                            DateTime dt;
                            int birthYear;
                            if (DateTime.TryParse(x.Birthdate, out dt) == false)
                            {
                                if (Int32.TryParse(x.Birthdate, out birthYear) == false)
                                {
                                    dt = new DateTime(1800, 1, 1);
                                }
                                else
                                {
                                    dt = new DateTime(birthYear, 1, 1);
                                }
                            }
                            return new { date = dt, patient = x };
                        }).OrderByDescending(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Вік: спочатку старші":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)))).AsEnumerable().Select(x =>
                        {
                            DateTime dt;
                            int birthYear;
                            if (DateTime.TryParse(x.Birthdate, out dt) == false)
                            {
                                if (Int32.TryParse(x.Birthdate, out birthYear) == false)
                                {
                                    dt = new DateTime(2500, 1, 1);
                                }
                                else
                                {
                                    dt = new DateTime(birthYear, 1, 1);
                                }
                            }
                            return new { date = dt, patient = x };
                        }).OrderBy(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                }
                result.CountPages = (int)countPages;
                PatientDTO[] patientsDTO = new PatientDTO[patientsToReturn.Length];
                for (int i = 0; i < patientsToReturn.Length; ++i)
                {
                    patientsDTO[i] = new PatientDTO(patientsToReturn[i]);
                }
                result.Patients = patientsDTO;
                return Ok(result);
            }
        }
        [HttpGet("debtors/{sortDescription}/{page}/{patientsPerPage}/{searchText}/{doctorId}")]
        public async Task<ActionResult<PatientsToClient>> GetDebtors(string sortDescription, int page, int patientsPerPage, string searchText, int doctorId)
        {
            if (page <= 0)
            {
                throw new NotValidException($"Page='{page}'; Page required to be greater than 0");
            }
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Doctor doctor = await db.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Id == doctorId);
                if (doctor == null)
                {
                    throw new NotFoundException($"Doctor with id={doctorId} cannot be found");
                }
                if (searchText == "<null>") //param that means no filter
                    searchText = "";
                PatientsToClient result = new PatientsToClient();
                Patient[] patientsToReturn = null;

                double countPages = db.Patients.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any())).Count() / Convert.ToDouble(patientsPerPage);
                if (countPages - Math.Truncate(countPages) != 0)
                {
                    countPages = Math.Ceiling(countPages);
                }
                switch (sortDescription)
                {
                    case "Ім'я: від А до Я":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any())).OrderBy(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Ім'я: від Я до А":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any())).OrderByDescending(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "За замовчуванням":
                        patientsToReturn = db.Patients.AsNoTracking().
                            Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))
                            .Select(p => new
                            {
                                Patient = p,
                                LastStageTime = p.Stages.Where(s => s.DoctorId == doctorId).Max(s => s.StageDatetime) // Gets the date of the latest stage
                            })
                            .OrderByDescending(p => p.LastStageTime) // Orders by the date of the latest stage
                            .Select(p => p.Patient) // Gets only the patient objects
                            .ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "За замовчуванням навпаки":
                        patientsToReturn = db.Patients.AsNoTracking().
                            Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))
                            .Select(p => new
                            {
                                Patient = p,
                                LastStageTime = p.Stages.Where(s => s.DoctorId == doctorId).Max(s => s.StageDatetime) // Gets the date of the latest stage
                            })
                            .OrderBy(p => p.LastStageTime) // Orders by the date of the latest stage
                            .Select(p => p.Patient) // Gets only the patient objects
                            .ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Дата реєстрації: спочатку недавні":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))).AsEnumerable().Select(x =>
                        {
                            DateTime dt;
                            return new { valid = DateTime.TryParse(x.RegisterDate, out dt), date = dt, patient = x };
                        }).OrderByDescending(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Дата реєстрації: спочатку старіші":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))).AsEnumerable().Select(x =>
                        {
                            DateTime dt;
                            if (DateTime.TryParse(x.RegisterDate, out dt) == false)
                            {
                                dt = new DateTime(2500, 1, 1);
                            }
                            return new { date = dt, patient = x };
                        }).OrderBy(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Вік: спочатку молодші":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))).AsEnumerable().Select(x =>
                        {
                            DateTime dt;
                            int birthYear;
                            if (DateTime.TryParse(x.Birthdate, out dt) == false)
                            {
                                if (Int32.TryParse(x.Birthdate, out birthYear) == false)
                                {
                                    dt = new DateTime(1800, 1, 1);
                                }
                                else
                                {
                                    dt = new DateTime(birthYear, 1, 1);
                                }
                            }
                            return new { date = dt, patient = x };
                        }).OrderByDescending(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Вік: спочатку старші":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))).AsEnumerable().Select(x =>
                        {
                            DateTime dt;
                            int birthYear;
                            if (DateTime.TryParse(x.Birthdate, out dt) == false)
                            {
                                if (Int32.TryParse(x.Birthdate, out birthYear) == false)
                                {
                                    dt = new DateTime(2500, 1, 1);
                                }
                                else
                                {
                                    dt = new DateTime(birthYear, 1, 1);
                                }
                            }
                            return new { date = dt, patient = x };
                        }).OrderBy(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                }
                result.CountPages = (int)countPages;
                PatientDTO[] patientsDTO = new PatientDTO[patientsToReturn.Length];
                for (int i = 0; i < patientsToReturn.Length; ++i)
                {
                    patientsDTO[i] = new PatientDTO(patientsToReturn[i]);
                }
                result.Patients = patientsDTO;
                return Ok(result);
            }
        }
        [HttpGet("byImageId/{imageId}/{status}/{sortDescription}/{page}/{patientsPerPage}/{searchText}")]
        public async Task<ActionResult<PatientsToClient>> GetPatientsByImageId(int imageId,string status,string sortDescription, int page, int patientsPerPage, string searchText)
        {
            if (page <= 0)
            {
                throw new NotValidException($"Page='{page}'; Page required to be greater than 0");
            }
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Image image = await db.Images.AsNoTracking().Where(i => i.Id == imageId).FirstOrDefaultAsync();
                if (image == null)
                {
                    throw new NotFoundException($"Image with id={imageId} cannot be found");
                }
                double countPages = db.Patients.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any())).Count() / Convert.ToDouble(patientsPerPage);
                if (countPages - Math.Truncate(countPages) != 0)
                {
                    countPages = Math.Ceiling(countPages);
                }
                if (searchText == "<null>") //param that means no filter
                    searchText = "";
                PatientsToClient result = new PatientsToClient();
                Patient[] patientsToReturn = null;
                switch (sortDescription)
                {
                    case "Ім'я: від А до Я":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any())).OrderBy(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Ім'я: від Я до А":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any())).OrderByDescending(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "За замовчуванням":
                        patientsToReturn = db.Patients.AsNoTracking().
                            Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any()))
                            .Select(p => new
                            {
                                Patient = p,
                                LastStageTime = p.Stages.Max(s => s.StageDatetime) // Gets the date of the latest stage
                            })
                            .OrderByDescending(p => p.LastStageTime) // Orders by the date of the latest stage
                            .Select(p => p.Patient) // Gets only the patient objects
                            .ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "За замовчуванням навпаки":
                        patientsToReturn = db.Patients.AsNoTracking().
                            Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any()))
                            .Select(p => new
                            {
                                Patient = p,
                                LastStageTime = p.Stages.Max(s => s.StageDatetime) // Gets the date of the latest stage
                            })
                            .OrderBy(p => p.LastStageTime) // Orders by the date of the latest stage
                            .Select(p => p.Patient) // Gets only the patient objects
                            .ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Дата реєстрації: спочатку недавні":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any()))).AsEnumerable().Select(x =>
                        {
                            DateTime dt;
                            return new { valid = DateTime.TryParse(x.RegisterDate, out dt), date = dt, patient = x };
                        }).OrderByDescending(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Дата реєстрації: спочатку старіші":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any()))).AsEnumerable().Select(x =>
                        {
                            DateTime dt;
                            if (DateTime.TryParse(x.RegisterDate, out dt) == false)
                            {
                                dt = new DateTime(2500, 1, 1);
                            }
                            return new { date = dt, patient = x };
                        }).OrderBy(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Вік: спочатку молодші":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any()))).AsEnumerable().Select(x =>
                        {
                            DateTime dt;
                            int birthYear;
                            if (DateTime.TryParse(x.Birthdate, out dt) == false)
                            {
                                if (Int32.TryParse(x.Birthdate, out birthYear) == false)
                                {
                                    dt = new DateTime(1800, 1, 1);
                                }
                                else
                                {
                                    dt = new DateTime(birthYear, 1, 1);
                                }
                            }
                            return new { date = dt, patient = x };
                        }).OrderByDescending(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                    case "Вік: спочатку старші":
                        patientsToReturn = db.Patients.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any()))).AsEnumerable().Select(x =>
                        {
                            DateTime dt;
                            int birthYear;
                            if (DateTime.TryParse(x.Birthdate, out dt) == false)
                            {
                                if (Int32.TryParse(x.Birthdate, out birthYear) == false)
                                {
                                    dt = new DateTime(2500, 1, 1);
                                }
                                else
                                {
                                    dt = new DateTime(birthYear, 1, 1);
                                }
                            }
                            return new { date = dt, patient = x };
                        }).OrderBy(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                        break;
                }
                result.CountPages = (int)countPages;
                PatientDTO[] patientsDTO = new PatientDTO[patientsToReturn.Length];
                for (int i = 0; i < patientsToReturn.Length; ++i)
                {
                    patientsDTO[i] = new PatientDTO(patientsToReturn[i]);
                }
                result.Patients = patientsDTO;
                return Ok(result);
            }
        }
    }
    
}
