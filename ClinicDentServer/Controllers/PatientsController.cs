using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClinicDentServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

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
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            Patient patient = await db.Patients.FirstOrDefaultAsync(x => x.Id == id);
            db.Dispose();
            if (patient == null)
            {
                return NotFound();
            }
            return Ok(new PatientDTO(patient));
        }
        
        //POST api/patients
        [HttpPost]
        public async Task<ActionResult<PatientDTO>> Post(PatientDTO patient)
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            Patient patientFromDTO = new Patient(patient);
            db.Patients.Add(patientFromDTO);

            await db.SaveChangesAsync();
            db.Dispose();
            patient.Id = patientFromDTO.Id;
            return Ok(patient);
        }
        //PUT api/patients
        [HttpPut]
        public async Task<ActionResult<PatientDTO>> Put(PatientDTO patient)
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            Patient patientFromDTO = new Patient(patient);
            db.Patients.Update(patientFromDTO);
            await db.SaveChangesAsync();
            db.Dispose();
            return Ok(patient);
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult<PatientDTO>> Delete(int id)
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            Patient patient = db.Patients.FirstOrDefault(x => x.Id == id);
            if (patient == null)
            {
                db.Dispose();
                return NotFound();
            }
            db.Patients.Remove(patient);
            await db.SaveChangesAsync();
            db.Dispose();
            return Ok(new PatientDTO(patient));
        }
        [HttpPut("changeCurePlan")]
        public async Task<ActionResult> ChangeCurePlan(ChangeCurePlanRequest r)
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            db.Database.ExecuteSqlRaw($"UPDATE [Patients] SET [CurePlan]='{r.CurePlan}' WHERE [Id]={r.PatientId}");
            await db.SaveChangesAsync();
            db.Dispose();
            return Ok();
        }


        [HttpGet("{status}/{sortDescription}/{page}/{patientsPerPage}/{searchText}")]
        public async Task<ActionResult<PatientsToClient>> Get(string status, string sortDescription, int page, int patientsPerPage, string searchText)
        {
            if (page <= 0)
            {
                return BadRequest();
            }
            if (searchText == "<null>") //param that means no filter
                searchText = "";
            PatientsToClient result = new PatientsToClient();
            Patient[] patientsToReturn = null;

            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            double countPages = await db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status))).CountAsync() / Convert.ToDouble(patientsPerPage);
            if (countPages - Math.Truncate(countPages) != 0)
            {
                countPages = Math.Ceiling(countPages);
            }
            switch (sortDescription)
            {
                case "Ім'я: від А до Я":
                    patientsToReturn = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status))).OrderBy(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "Ім'я: від Я до А":
                    patientsToReturn = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status))).OrderByDescending(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "За замовчуванням":
                    patientsToReturn = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status))).OrderByDescending(p => p.Id).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "За замовчуванням навпаки":
                    patientsToReturn = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status))).OrderBy(p => p.Id).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "Дата реєстрації: спочатку недавні":
                    patientsToReturn = db.Patients.Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)))).AsEnumerable().Select(x =>
                    {
                        DateTime dt;
                        return new { valid = DateTime.TryParse(x.RegisterDate, out dt), date = dt, patient = x };
                    }).OrderByDescending(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "Дата реєстрації: спочатку старіші":
                    patientsToReturn = db.Patients.Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)))).AsEnumerable().Select(x =>
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
                    patientsToReturn = db.Patients.Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)))).AsEnumerable().Select(x =>
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
                    patientsToReturn = db.Patients.Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)))).AsEnumerable().Select(x =>
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
            db.Dispose();
            return Ok(result);
        }
        [HttpGet("{status}/{sortDescription}/{page}/{patientsPerPage}/{searchText}/{doctorId}")]
        public async Task<ActionResult<PatientsToClient>> Get(string status, string sortDescription, int page, int patientsPerPage, string searchText, int doctorId)
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            Doctor doctor = await db.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
            if (page <= 0)
            {
                db.Dispose();
                return BadRequest();
            }
            if (searchText == "<null>") //param that means no filter
                searchText = "";
            PatientsToClient result = new PatientsToClient();
            Patient[] patientsToReturn = null;

            double countPages = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any())).Count() / Convert.ToDouble(patientsPerPage);
            if (countPages - Math.Truncate(countPages) != 0)
            {
                countPages = Math.Ceiling(countPages);
            }
            switch (sortDescription)
            {
                case "Ім'я: від А до Я":
                    patientsToReturn = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any())).OrderBy(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "Ім'я: від Я до А":
                    patientsToReturn = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any())).OrderByDescending(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "За замовчуванням":
                    patientsToReturn = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any())).OrderByDescending(p => p.Id).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "За замовчуванням навпаки":
                    patientsToReturn = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any())).OrderBy(p => p.Id).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "Дата реєстрації: спочатку недавні":
                    patientsToReturn = db.Patients.Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))).AsEnumerable().Select(x =>
                    {
                        DateTime dt;
                        return new { valid = DateTime.TryParse(x.RegisterDate, out dt), date = dt, patient = x };
                    }).OrderByDescending(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "Дата реєстрації: спочатку старіші":
                    patientsToReturn = db.Patients.Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))).AsEnumerable().Select(x =>
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
                    patientsToReturn = db.Patients.Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))).AsEnumerable().Select(x =>
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
                    patientsToReturn = db.Patients.Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))).AsEnumerable().Select(x =>
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
            db.Dispose();
            return Ok(result);
        }
        
        ///Getters for debtors
        [HttpGet("debtors/{sortDescription}/{page}/{patientsPerPage}/{searchText}")]
        public async Task<ActionResult<PatientsToClient>> GetDebtors(string sortDescription, int page, int patientsPerPage, string searchText)
        {

            if (page <= 0)
            {
                return BadRequest();
            }
            if (searchText == "<null>") //param that means no filter
                searchText = "";
            PatientsToClient result = new PatientsToClient();
            Patient[] patientsToReturn = null;

            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            double countPages = await db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed))).CountAsync() / Convert.ToDouble(patientsPerPage);
            if (countPages - Math.Truncate(countPages) != 0)
            {
                countPages = Math.Ceiling(countPages);
            }
            switch (sortDescription)
            {
                case "Ім'я: від А до Я":
                    patientsToReturn = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed))).OrderBy(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "Ім'я: від Я до А":
                    patientsToReturn = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed))).OrderByDescending(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "За замовчуванням":
                    patientsToReturn = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed))).OrderByDescending(p => p.Id).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "За замовчуванням навпаки":
                    patientsToReturn = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed))).OrderBy(p => p.Id).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "Дата реєстрації: спочатку недавні":
                    patientsToReturn = db.Patients.Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)))).AsEnumerable().Select(x =>
                    {
                        DateTime dt;
                        return new { valid = DateTime.TryParse(x.RegisterDate, out dt), date = dt, patient = x };
                    }).OrderByDescending(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "Дата реєстрації: спочатку старіші":
                    patientsToReturn = db.Patients.Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)))).AsEnumerable().Select(x =>
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
                    patientsToReturn = db.Patients.Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)))).AsEnumerable().Select(x =>
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
                    patientsToReturn = db.Patients.Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)))).AsEnumerable().Select(x =>
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
            db.Dispose();
            return Ok(result);
        }
        [HttpGet("debtors/{sortDescription}/{page}/{patientsPerPage}/{searchText}/{doctorId}")]
        public async Task<ActionResult<PatientsToClient>> GetDebtors(string sortDescription, int page, int patientsPerPage, string searchText, int doctorId)
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            Doctor doctor = await db.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
            if (User.Identity.Name != doctor.Email || page <= 0)
            {
                db.Dispose();
                return BadRequest();
            }
            if (searchText == "<null>") //param that means no filter
                searchText = "";
            PatientsToClient result = new PatientsToClient();
            Patient[] patientsToReturn = null;

            double countPages = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any())).Count() / Convert.ToDouble(patientsPerPage);
            if (countPages - Math.Truncate(countPages) != 0)
            {
                countPages = Math.Ceiling(countPages);
            }
            switch (sortDescription)
            {
                case "Ім'я: від А до Я":
                    patientsToReturn = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any())).OrderBy(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "Ім'я: від Я до А":
                    patientsToReturn = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any())).OrderByDescending(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "За замовчуванням":
                    patientsToReturn = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any())).OrderByDescending(p => p.Id).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "За замовчуванням навпаки":
                    patientsToReturn = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any())).OrderBy(p => p.Id).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "Дата реєстрації: спочатку недавні":
                    patientsToReturn = db.Patients.Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))).AsEnumerable().Select(x =>
                    {
                        DateTime dt;
                        return new { valid = DateTime.TryParse(x.RegisterDate, out dt), date = dt, patient = x };
                    }).OrderByDescending(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "Дата реєстрації: спочатку старіші":
                    patientsToReturn = db.Patients.Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))).AsEnumerable().Select(x =>
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
                    patientsToReturn = db.Patients.Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))).AsEnumerable().Select(x =>
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
                    patientsToReturn = db.Patients.Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))).AsEnumerable().Select(x =>
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
            db.Dispose();
            return Ok(result);
        }
        [HttpGet("byImageId/{imageId}/{status}/{sortDescription}/{page}/{patientsPerPage}/{searchText}")]
        public async Task<ActionResult<PatientsToClient>> GetPatientsByImageId(int imageId,string status,string sortDescription, int page, int patientsPerPage, string searchText)
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            Image image = await db.Images.Where(i => i.Id == imageId).FirstOrDefaultAsync();
            if (page <= 0 || image==null)
            {
                db.Dispose();
                return BadRequest();
            }
            double countPages = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any())).Count() / Convert.ToDouble(patientsPerPage);
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
                    patientsToReturn = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any())).OrderBy(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "Ім'я: від Я до А":
                    patientsToReturn = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any())).OrderByDescending(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "За замовчуванням":
                    patientsToReturn = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any())).OrderByDescending(p => p.Id).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "За замовчуванням навпаки":
                    patientsToReturn = db.Patients.Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any())).OrderBy(p => p.Id).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "Дата реєстрації: спочатку недавні":
                    patientsToReturn = db.Patients.Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any()))).AsEnumerable().Select(x =>
                    {
                        DateTime dt;
                        return new { valid = DateTime.TryParse(x.RegisterDate, out dt), date = dt, patient = x };
                    }).OrderByDescending(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "Дата реєстрації: спочатку старіші":
                    patientsToReturn = db.Patients.Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any()))).AsEnumerable().Select(x =>
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
                    patientsToReturn = db.Patients.Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any()))).AsEnumerable().Select(x =>
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
                    patientsToReturn = db.Patients.Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any()))).AsEnumerable().Select(x =>
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
            db.Dispose();
            return Ok(result);
        }
    }
    public class ChangeCurePlanRequest
    {
        public int PatientId { get; set; }
        public string CurePlan { get; set;}
    }
}
