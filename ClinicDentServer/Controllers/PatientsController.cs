using ClinicDentServer.Exceptions;
using ClinicDentServer.Interfaces.Repositories;
using ClinicDentServer.Models;
using ClinicDentServer.RequestCustomAnswers;
using ClinicDentServer.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicDentServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PatientsController : ControllerBase
    {
        Lazy<IDefaultRepository<Patient>> patientRepository;
        Lazy<IImageRepository<Image>> imageRepository;
        Lazy<IDefaultRepository<Doctor>> doctorRepository;

        public PatientsController(Lazy<IDefaultRepository<Patient>> patientRepositoryToSet, Lazy<IImageRepository<Image>> imageRepositoryToSet, Lazy<IDefaultRepository<Doctor>> doctorRepositoryToSet)
        {
            patientRepository = patientRepositoryToSet;
            imageRepository = imageRepositoryToSet;
            doctorRepository = doctorRepositoryToSet;

        }
        [HttpGet("{id}")]
        public async Task<ActionResult<PatientDTO>> Get(int id)
        {
            Patient patient = await patientRepository.Value.dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (patient == null)
            {
                throw new NotFoundException($"Patient with id={id} cannot be found");
            }
            return Ok(new PatientDTO(patient));
        }
        
        //POST api/patients
        [HttpPost]
        public async Task<ActionResult<PatientDTO>> Post(PatientDTO patient)
        {
            Patient patientFromDTO = new Patient(patient);
            DateTime now = DateTime.Now;
            patientFromDTO.CreatedDateTime = now;
            patientFromDTO.LastModifiedDateTime = now;
            await patientRepository.Value.Add(patientFromDTO);
            patient.Id = patientFromDTO.Id;
            patient.CreatedDateTime = patientFromDTO.CreatedDateTime.ToString(Options.ExactDateTimePattern);
            patient.LastModifiedDateTime = patientFromDTO.LastModifiedDateTime.ToString(Options.ExactDateTimePattern);
            return Ok(patient);
        }
        //PUT api/patients
        [HttpPut]
        [Produces("text/plain")]
        public async Task<ActionResult> Put(PatientDTO patient)
        {
            Patient patientFromDTO = new Patient(patient);
            Patient existingPatient = await patientRepository.Value.dbSet.AsNoTracking().FirstOrDefaultAsync(p => p.Id == patient.Id);
            if (existingPatient == null)
            {
                throw new NotFoundException($"Patient with id={patient.Id} cannot be found");
            }
            if (existingPatient.LastModifiedDateTime > patientFromDTO.LastModifiedDateTime)
            {
                throw new ConflictException("Another process have updated the patient");
            }
            patientFromDTO.LastModifiedDateTime = DateTime.Now;
            await patientRepository.Value.Update(patientFromDTO);
            return Ok(patientFromDTO.LastModifiedDateTime.ToString(Options.ExactDateTimePattern));
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            Patient patient = await patientRepository.Value.dbSet.FirstOrDefaultAsync(x => x.Id == id);
            if (patient == null)
            {
                throw new NotFoundException($"Patient with id={id} cannot be found");
            }
            await patientRepository.Value.Remove(patient);
            return NoContent();
        }
        [HttpPut("changeCurePlan")]
        [Produces("text/plain")]
        public async Task<ActionResult> ChangeCurePlan(ChangeCurePlanRequest r)
        {
            Patient patient = await patientRepository.Value.dbSet.FirstOrDefaultAsync(p => p.Id == r.PatientId);
            if (patient == null)
            {
                throw new NotFoundException($"Patient with id={r.PatientId} cannot be found");
            }
            bool isValid = DateTime.TryParseExact(r.LastModifiedDateTime, Options.ExactDateTimePattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime lastModifiedDateTime);
            if (isValid)
            {
                if (patient.LastModifiedDateTime > lastModifiedDateTime)
                {
                    throw new ConflictException("Another process have updated the patient");
                }
                patient.CurePlan = r.CurePlan;
                patient.LastModifiedDateTime = DateTime.Now;
                await patientRepository.Value.Update(patient);
                return Ok(patient.LastModifiedDateTime.ToString(Options.ExactDateTimePattern));
            }
            throw new NotValidException($"PatientsController.ChangeCurePlan.ChangeCurePlanRequest.LastModifiedDateTime ={r.LastModifiedDateTime} and is not valid string and cannot be converted to DateTime");
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
            double countPages = await patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status))).CountAsync() / Convert.ToDouble(patientsPerPage);
            if (countPages - Math.Truncate(countPages) != 0)
            {
                countPages = Math.Ceiling(countPages);
            }
            switch (sortDescription)
            {
                case "Ім'я: від А до Я":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status))).OrderBy(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "Ім'я: від Я до А":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status))).OrderByDescending(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "За замовчуванням":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking()
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
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking()
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
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)))).AsEnumerable().Select(x =>
                    {
                        DateTime dt;
                        return new { valid = DateTime.TryParse(x.RegisterDate, out dt), date = dt, patient = x };
                    }).OrderByDescending(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "Дата реєстрації: спочатку старіші":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)))).AsEnumerable().Select(x =>
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
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)))).AsEnumerable().Select(x =>
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
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)))).AsEnumerable().Select(x =>
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
        [HttpGet("{status}/{sortDescription}/{page}/{patientsPerPage}/{searchText}/{doctorId}")]
        public async Task<ActionResult<PatientsToClient>> Get(string status, string sortDescription, int page, int patientsPerPage, string searchText, int doctorId)
        {
            if (page <= 0)
            {
                throw new NotValidException($"Page='{page}'; Page required to be greater than 0");
            }
            Doctor doctor = await doctorRepository.Value.dbSet.AsNoTracking().FirstOrDefaultAsync(d => d.Id == doctorId);
            if (doctor == null)
            {
                throw new NotFoundException($"Doctor with id={doctorId} cannot be found");
            }
            if (searchText == "<null>") //param that means no filter
                searchText = "";
            PatientsToClient result = new PatientsToClient();
            Patient[] patientsToReturn = null;

            double countPages = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any())).Count() / Convert.ToDouble(patientsPerPage);
            if (countPages - Math.Truncate(countPages) != 0)
            {
                countPages = Math.Ceiling(countPages);
            }
            switch (sortDescription)
            {
                case "Ім'я: від А до Я":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any())).OrderBy(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "Ім'я: від Я до А":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any())).OrderByDescending(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "За замовчуванням":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking()
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
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking()
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
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))).AsEnumerable().Select(x =>
                    {
                        DateTime dt;
                        return new { valid = DateTime.TryParse(x.RegisterDate, out dt), date = dt, patient = x };
                    }).OrderByDescending(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "Дата реєстрації: спочатку старіші":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))).AsEnumerable().Select(x =>
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
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))).AsEnumerable().Select(x =>
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
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Doctor.Id == doctorId).Any()))).AsEnumerable().Select(x =>
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
        
        ///Getters for debtors
        [HttpGet("debtors/{sortDescription}/{page}/{patientsPerPage}/{searchText}")]
        public async Task<ActionResult<DebtPatientsToClient>> GetDebtors(string sortDescription, int page, int patientsPerPage, string searchText)
        {

            if (page <= 0)
            {
                throw new NotValidException($"Page='{page}'; Page required to be greater than 0");
            }
            if (searchText == "<null>") //param that means no filter
                searchText = "";
            DebtPatientsToClient result = new DebtPatientsToClient();
            PatientWithDebtDTO[] patientsToReturn = null;
            double countPages = await patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (p.Stages.Any((s) => s.Price != s.Payed))).CountAsync() / Convert.ToDouble(patientsPerPage);
            if (countPages - Math.Truncate(countPages) != 0)
            {
                countPages = Math.Ceiling(countPages);
            }
            switch (sortDescription)
            {
                case "Ім'я: від А до Я":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText.ToLower()) && p.Stages.Any(s => s.Price != s.Payed))).OrderBy(pd => pd.Name).Select(p => new PatientWithDebtDTO(p, p.Stages.Sum(s => s.Price - s.Payed))).Skip(patientsPerPage * (page - 1)).Take(patientsPerPage).ToArray();
                    break;
                case "Ім'я: від Я до А":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText.ToLower()) && p.Stages.Any(s => s.Price != s.Payed))).OrderByDescending(pd => pd.Name).Select(p => new PatientWithDebtDTO(p, p.Stages.Sum(s => s.Price - s.Payed))).Skip(patientsPerPage * (page - 1)).Take(patientsPerPage).ToArray();
                    break;
                case "За замовчуванням":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().
                        Where(p => (p.Name.ToLower().Contains(searchText.ToLower()) && (p.Stages.Any((s) => s.Price != s.Payed))))
                        .Select(p => new
                        {
                            Patient = new PatientWithDebtDTO(p, p.Stages.Sum(s => s.Price - s.Payed)),
                            LastStageTime = p.Stages.Max(s => s.StageDatetime) // Gets the date of the latest stage
                        })
                        .OrderByDescending(p => p.LastStageTime) // Orders by the date of the latest stage
                        .Select(p => p.Patient).Skip(patientsPerPage * (page - 1)).Take(patientsPerPage).ToArray();
                    break;
                case "За замовчуванням навпаки":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().
                        Where(p => (p.Name.ToLower().Contains(searchText.ToLower()) && (p.Stages.Any((s) => s.Price != s.Payed))))
                        .Select(p => new
                        {
                            Patient = new PatientWithDebtDTO(p, p.Stages.Sum(s => s.Price - s.Payed)),
                            LastStageTime = p.Stages.Max(s => s.StageDatetime) // Gets the date of the latest stage
                        })
                        .OrderBy(p => p.LastStageTime) // Orders by the date of the latest stage
                        .Select(p => p.Patient).Skip(patientsPerPage * (page - 1)).Take(patientsPerPage).ToArray();
                    break;
                case "Дата реєстрації: спочатку недавні":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText.ToLower()) && (p.Stages.Any((s) => s.Price != s.Payed))))).AsEnumerable().Select(x =>
                    {
                        DateTime dt;
                        return new { valid = DateTime.TryParse(x.RegisterDate, out dt), date = dt, patient = new PatientWithDebtDTO(x, x.Stages.Sum(s => s.Price - s.Payed)) };
                    }).OrderByDescending(x => x.date).Select(x => x.patient).Skip(patientsPerPage * (page - 1)).Take(patientsPerPage).ToArray();
                    break;
                case "Дата реєстрації: спочатку старіші":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText.ToLower()) && (p.Stages.Any((s) => s.Price != s.Payed))))).AsEnumerable().Select(x =>
                    {
                        DateTime dt;
                        return new { valid = DateTime.TryParse(x.RegisterDate, out dt), date = dt, patient = new PatientWithDebtDTO(x, x.Stages.Sum(s => s.Price - s.Payed)) };
                    }).OrderBy(x => x.date).Select(x => x.patient).Skip(patientsPerPage * (page - 1)).Take(patientsPerPage).ToArray();
                    break;
                case "Вік: спочатку молодші":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText.ToLower()) && (p.Stages.Any((s) => s.Price != s.Payed)))).AsEnumerable().Select(x =>
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
                        return new { date = dt, patient = new PatientWithDebtDTO(x, x.Stages.Sum(s => s.Price - s.Payed)) };
                    }).OrderByDescending(x => x.date).Select(x => x.patient).Skip(patientsPerPage * (page - 1)).Take(patientsPerPage).ToArray();
                    break;
                case "Вік: спочатку старші":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText.ToLower()) && (p.Stages.Any((s) => s.Price != s.Payed)))).AsEnumerable().Select(x =>
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
                        return new { date = dt, patient = new PatientWithDebtDTO(x, x.Stages.Sum(s => s.Price - s.Payed)) };
                    }).OrderBy(x => x.date).Select(x => x.patient).Skip(patientsPerPage * (page - 1)).Take(patientsPerPage).ToArray();
                    break;
            }
            result.CountPages = (int)countPages;
            result.Patients = patientsToReturn;
            return Ok(result);
        }
        [HttpGet("debtors/{sortDescription}/{page}/{patientsPerPage}/{searchText}/{doctorId}")]
        public async Task<ActionResult<DebtPatientsToClient>> GetDebtors(string sortDescription, int page, int patientsPerPage, string searchText, int doctorId)
        {
            if (page <= 0)
            {
                throw new NotValidException($"Page='{page}'; Page required to be greater than 0");
            }
            Doctor doctor = await doctorRepository.Value.dbSet.AsNoTracking().FirstOrDefaultAsync(d => d.Id == doctorId);
            if (doctor == null)
            {
                throw new NotFoundException($"Doctor with id={doctorId} cannot be found");
            }
            if (searchText == "<null>") //param that means no filter
                searchText = "";
            DebtPatientsToClient result = new DebtPatientsToClient();
            PatientWithDebtDTO[] patientsToReturn = null;

            double countPages = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText.ToLower()) && p.Stages.Any(s => s.Price != s.Payed && s.Doctor.Id == doctorId))).Count() / Convert.ToDouble(patientsPerPage);
            if (countPages - Math.Truncate(countPages) != 0)
            {
                countPages = Math.Ceiling(countPages);
            }
            switch (sortDescription)
            {
                case "Ім'я: від А до Я":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText.ToLower()) && p.Stages.Any(s => s.Price != s.Payed && s.Doctor.Id == doctorId))).OrderBy(pd => pd.Name).Select(p => new PatientWithDebtDTO(p, p.Stages.Where(s => s.DoctorId == doctorId).Sum(s => s.Price - s.Payed))).Skip(patientsPerPage * (page - 1)).Take(patientsPerPage).ToArray();
                    break;
                case "Ім'я: від Я до А":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText.ToLower()) && p.Stages.Any(s => s.Price != s.Payed && s.Doctor.Id == doctorId))).OrderByDescending(pd => pd.Name).Select(p => new PatientWithDebtDTO(p, p.Stages.Where(s => s.DoctorId == doctorId).Sum(s => s.Price - s.Payed))).Skip(patientsPerPage * (page - 1)).Take(patientsPerPage).ToArray();
                    break;
                case "За замовчуванням":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText.ToLower()) && p.Stages.Any(s => s.Price != s.Payed && s.Doctor.Id == doctorId)))
                        .Select(p => new
                        {
                            Patient = new PatientWithDebtDTO(p, p.Stages.Where(s => s.DoctorId == doctorId).Sum(s => s.Price - s.Payed)),
                            LastStageTime = p.Stages.Where(s => s.DoctorId == doctorId).Max(s => s.StageDatetime) // Gets the date of the latest stage
                        })
                        .OrderByDescending(p => p.LastStageTime) // Orders by the date of the latest stage
                        .Select(p => p.Patient) // Gets only the patient objects
                        .Skip(patientsPerPage * (page - 1)).Take(patientsPerPage).ToArray();
                    break;
                case "За замовчуванням навпаки":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText.ToLower()) && p.Stages.Any(s => s.Price != s.Payed && s.Doctor.Id == doctorId)))
                        .Select(p => new
                        {
                            Patient = new PatientWithDebtDTO(p, p.Stages.Where(s => s.DoctorId == doctorId).Sum(s => s.Price - s.Payed)),
                            LastStageTime = p.Stages.Where(s => s.DoctorId == doctorId).Max(s => s.StageDatetime) // Gets the date of the latest stage
                        })
                        .OrderBy(p => p.LastStageTime) // Orders by the date of the latest stage
                        .Select(p => p.Patient) // Gets only the patient objects
                        .Skip(patientsPerPage * (page - 1)).Take(patientsPerPage).ToArray();
                    break;
                case "Дата реєстрації: спочатку недавні":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText.ToLower()) && p.Stages.Any(s => s.Price != s.Payed && s.Doctor.Id == doctorId))).AsEnumerable().Select(x =>
                    {
                        DateTime dt;
                        return new { valid = DateTime.TryParse(x.RegisterDate, out dt), date = dt, patient = new PatientWithDebtDTO(x, x.Stages.Where(s => s.DoctorId == doctorId).Sum(s => s.Price - s.Payed)) };
                    }).OrderByDescending(x => x.date).Select(x => x.patient).Skip(patientsPerPage * (page - 1)).Take(patientsPerPage).ToArray();
                    break;
                case "Дата реєстрації: спочатку старіші":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText.ToLower()) && p.Stages.Any(s => s.Price != s.Payed && s.Doctor.Id == doctorId))).AsEnumerable().Select(x =>
                    {
                        DateTime dt;
                        if (DateTime.TryParse(x.RegisterDate, out dt) == false)
                        {
                            dt = new DateTime(2500, 1, 1);
                        }
                        return new { date = dt, patient = new PatientWithDebtDTO(x, x.Stages.Where(s => s.DoctorId == doctorId).Sum(s => s.Price - s.Payed)) };
                    }).OrderBy(x => x.date).Select(x => x.patient).Skip(patientsPerPage * (page - 1)).Take(patientsPerPage).ToArray();
                    break;
                case "Вік: спочатку молодші":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText.ToLower()) && p.Stages.Any(s => s.Price != s.Payed && s.Doctor.Id == doctorId))).AsEnumerable().Select(x =>
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
                        return new { date = dt, patient = new PatientWithDebtDTO(x, x.Stages.Where(s => s.DoctorId == doctorId).Sum(s => s.Price - s.Payed)) };
                    }).OrderByDescending(x => x.date).Select(x => x.patient).Skip(patientsPerPage * (page - 1)).Take(patientsPerPage).ToArray();
                    break;
                case "Вік: спочатку старші":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText.ToLower()) && p.Stages.Any(s => s.Price != s.Payed && s.Doctor.Id == doctorId))).AsEnumerable().Select(x =>
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
                        return new { date = dt, patient = new PatientWithDebtDTO(x, x.Stages.Where(s => s.DoctorId == doctorId).Sum(s => s.Price - s.Payed)) };
                    }).OrderBy(x => x.date).Select(x => x.patient).Skip(patientsPerPage * (page - 1)).Take(patientsPerPage).ToArray();
                    break;
                case "Сума боргу: спочатку більші":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText.ToLower()) && p.Stages.Any(s => s.Price != s.Payed && s.Doctor.Id == doctorId))).OrderByDescending(pd => pd.Stages.Where(s => s.DoctorId == doctorId).Sum(s => s.Price - s.Payed)).Select(p => new PatientWithDebtDTO(p, p.Stages.Where(s => s.DoctorId == doctorId).Sum(s => s.Price - s.Payed))).Skip(patientsPerPage * (page - 1)).Take(patientsPerPage).ToArray();
                    break;
                case "Сума боргу: спочатку менші":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText.ToLower()) && p.Stages.Any(s => s.Price != s.Payed && s.Doctor.Id == doctorId))).OrderBy(pd => pd.Stages.Where(s => s.DoctorId == doctorId).Sum(s => s.Price - s.Payed)).Select(p => new PatientWithDebtDTO(p, p.Stages.Where(s => s.DoctorId == doctorId).Sum(s => s.Price - s.Payed))).Skip(patientsPerPage * (page - 1)).Take(patientsPerPage).ToArray();
                    break;
            }
            result.CountPages = (int)countPages;
            result.Patients = patientsToReturn;
            return Ok(result);
        }
        [HttpGet("byImageId/{imageId}/{status}/{sortDescription}/{page}/{patientsPerPage}/{searchText}")]
        public async Task<ActionResult<PatientsToClient>> GetPatientsByImageId(int imageId,string status,string sortDescription, int page, int patientsPerPage, string searchText)
        {
            if (page <= 0)
            {
                throw new NotValidException($"Page='{page}'; Page required to be greater than 0");
            }
            Image image = await imageRepository.Value.dbSet.AsNoTracking().Where(i => i.Id == imageId).FirstOrDefaultAsync();
            if (image == null)
            {
                throw new NotFoundException($"Image with id={imageId} cannot be found");
            }
            double countPages = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any())).Count() / Convert.ToDouble(patientsPerPage);
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
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any())).OrderBy(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "Ім'я: від Я до А":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => (p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any())).OrderByDescending(p => p.Name).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "За замовчуванням":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().
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
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().
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
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any()))).AsEnumerable().Select(x =>
                    {
                        DateTime dt;
                        return new { valid = DateTime.TryParse(x.RegisterDate, out dt), date = dt, patient = x };
                    }).OrderByDescending(x => x.date).Select(x => x.patient).ToArray()[Range.StartAt(patientsPerPage * (page - 1))].Take(patientsPerPage).ToArray();
                    break;
                case "Дата реєстрації: спочатку старіші":
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any()))).AsEnumerable().Select(x =>
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
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any()))).AsEnumerable().Select(x =>
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
                    patientsToReturn = patientRepository.Value.dbSet.AsNoTracking().Where(p => ((p.Name.ToLower().Contains(searchText) || p.Name.Contains(searchText)) && (status == "Всі статуси" || p.Statuses.Contains(status)) && (p.Stages.Where(s => s.Images.Contains(image)).Any()))).AsEnumerable().Select(x =>
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
