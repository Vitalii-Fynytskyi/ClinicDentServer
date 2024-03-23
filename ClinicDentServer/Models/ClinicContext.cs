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
        public DbSet<ToothUnderObservation> ToothUnderObservations { get; set; }

        public DbSet<Cabinet> Cabinets { get; set; }
        public DbSet<CabinetComment> CabinetComments { get; set; }


        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<StageAsset> StageAssets { get; set; }

        string connectionString;

        public ClinicContext(DbContextOptions<ClinicContext> options) : base(options)
        {
            Database.EnsureCreated();
        }
        public ClinicContext(string connString)
        {
            connectionString = connString;
            if(Database.EnsureCreated() == true)
            {
                Database.ExecuteSqlRaw(File.ReadAllText(Environment.CurrentDirectory + @"\sql queries\CreateTrigger_CabinetDeleted.sql"));
                Database.ExecuteSqlRaw(File.ReadAllText(Environment.CurrentDirectory + @"\sql queries\CreateTrigger_DoctorDeleted.sql"));
                Database.ExecuteSqlRaw(File.ReadAllText(Environment.CurrentDirectory + @"\sql queries\CreateTrigger_PatientDeleted.sql"));
                Database.ExecuteSqlRaw(File.ReadAllText(Environment.CurrentDirectory + @"\sql queries\CreateTrigger_StageAssetDeleted.sql"));
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region StageForeignKeys
            //patient
            modelBuilder.Entity<Stage>()
                .HasOne(s => s.Patient)
                .WithMany(s => s.Stages)
                .HasForeignKey(s => s.PatientId)
                .OnDelete(DeleteBehavior.NoAction);

            //doctor
            modelBuilder.Entity<Stage>()
                .HasOne(s => s.Doctor)
                .WithMany(s => s.Stages)
                .HasForeignKey(s => s.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);

            //ToothUnderObservation
            modelBuilder.Entity<Stage>()
        .HasOne(e => e.ToothUnderObservation)
            .WithOne(e => e.Stage)
        .HasForeignKey<ToothUnderObservation>(e => e.StageId)
        .IsRequired();
            #endregion

            #region ScheduleForeignKeys
            //patient
            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Patient)
                .WithMany(s => s.Schedules)
                .HasForeignKey(s => s.PatientId)
                .OnDelete(DeleteBehavior.NoAction);
            //doctor
            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Doctor)
                .WithMany(s => s.Schedules)
                .HasForeignKey(s => s.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);
            //cabinet
            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Cabinet)
                .WithMany(s => s.Schedules)
                .HasForeignKey(s => s.CabinetId)
                .OnDelete(DeleteBehavior.NoAction);
            #endregion

            #region CabinetCommentForeignKeys
            //cabinet
            modelBuilder.Entity<CabinetComment>()
                .HasOne(s => s.Cabinet)
                .WithMany(s => s.CabinetComments)
                .HasForeignKey(s => s.CabinetId)
                .OnDelete(DeleteBehavior.NoAction);
            #endregion

            #region ImageForeignKey
            //doctor
            modelBuilder.Entity<Image>()
                .HasOne(s => s.Doctor)
                .WithMany(s => s.Images)
                .HasForeignKey(s => s.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);
            #endregion

            Patient patient = new Patient();
            patient.Id = 1;
            patient.Name = "Demo Patient";
            patient.Birthdate = "1999";
            patient.RegisterDate = "16.09.2021";
            patient.Statuses = "Новий|";
            patient.Phone = "0963411125";
            patient.Notes = "NotesText";
            patient.Illness = "IllnessText";
            patient.Gender = "Чол";
            patient.Address = "Lviv";
            patient.CurePlan = "План лікування";

            Doctor doctor = new Doctor();
            doctor.Id = 1;
            doctor.Name = "Demo Doctor";
            doctor.Email = "test";
            doctor.Password = "123";

            Cabinet cabinet1 = new Cabinet();
            cabinet1.Id = 1;
            cabinet1.CabinetName = "Крісло 1";


            modelBuilder.UseCollation("Cyrillic_General_CI_AS");
            modelBuilder.Entity<Patient>().HasData(patient);
            modelBuilder.Entity<Doctor>().HasData(doctor);
            modelBuilder.Entity<Cabinet>().HasData(cabinet1);
        }
        protected override void OnConfiguring(DbContextOptionsBuilder dbContextOptionsBuilder)
        {
            dbContextOptionsBuilder.UseSqlServer(connectionString);
        }
    }
}
