using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace ClinicDentServer.Models
{
    public class ClinicContext : DbContext
    {
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Stage> Stages { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Cabinet> Cabinets { get; set; }

        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<StageAsset> StageAssets { get; set; }

        string connectionString;
        static string CreateTrigger_StageAssetDeleted;
        static ClinicContext()
        {
            CreateTrigger_StageAssetDeleted = File.ReadAllText(Environment.CurrentDirectory + @"\sql queries\CreateTrigger_StageAssetDeleted.sql");
        }

        public ClinicContext(DbContextOptions<ClinicContext> options) : base(options)
        {
            Database.EnsureCreated();
        }
        public ClinicContext(string connString)
        {
            connectionString = connString;
            if(Database.EnsureCreated() == true) // if Db just created
            {
                Database.ExecuteSqlRaw(CreateTrigger_StageAssetDeleted);
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Patient patient = new Patient();
            patient.Id = 1;
            patient.Name = "Радченко Антон";
            patient.Birthdate = "1999";
            patient.RegisterDate = "16.09.2021";
            patient.Statuses = "Новий|";
            patient.Phone = "0963411125";
            patient.Notes = "NotesText";
            patient.Illness = "IllnessText";
            patient.Gender = "Чол";
            patient.Address = "Lviv";
            patient.CurePlan = "План лікування";
            modelBuilder.UseCollation("Cyrillic_General_CI_AS");
            modelBuilder.Entity<Patient>().HasData(patient);
        }
        protected override void OnConfiguring(DbContextOptionsBuilder dbContextOptionsBuilder)
        {
            dbContextOptionsBuilder.UseSqlServer(connectionString);
        }
    }
}
