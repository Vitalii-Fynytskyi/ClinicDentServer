using Microsoft.EntityFrameworkCore;
namespace ClinicDentServer.Models
{
    public class ClinicDentUsersContext :DbContext
    {
        public DbSet<DoctorUser> Doctors { get; set; }
        string connectionString;
        public ClinicDentUsersContext(string connString)
        {
            connectionString = connString;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder dbContextOptionsBuilder)
        {
            dbContextOptionsBuilder.UseSqlServer(connectionString);
        }
    }
}
