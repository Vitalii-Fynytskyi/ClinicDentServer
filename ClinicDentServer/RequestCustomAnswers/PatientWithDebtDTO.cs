using ClinicDentServer.Models;

namespace ClinicDentServer.RequestCustomAnswers
{
    public class PatientWithDebtDTO : PatientDTO
    {
        public int DebtSum { get; set; }
        public PatientWithDebtDTO(Patient p, int debtSum):base(p)
        {
            DebtSum = debtSum;
        }
    }
    public class DebtPatientsToClient
    {
        public PatientWithDebtDTO[] Patients { get; set; }
        public int CountPages { get; set; }
    }
}
