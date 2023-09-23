using ClinicDentServer.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ClinicDentServer.Models
{
    public class Stage
    {
        public Stage()
        {
            Images = new List<Image>();
        }
        public Stage(StageDTO s)
        {
            Id = s.Id;
            PatientId = s.PatientId;
            DoctorId = s.DoctorId;
            Title = s.Title;
            IsSentViaViber = s.IsSentViaViber;
            bool isValid = DateTime.TryParseExact(s.StageDatetime, Options.DateTimePattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result);
            if (isValid)
            {
                StageDatetime = result;
            }
            else
            {
                throw new NotValidException($"'{s.StageDatetime}' datetime is not in correct format");
            }
            OperationId = s.Operation;
            BondId = s.Bond;
            DentinId = s.Dentin;
            EnamelId = s.Enamel;
            CanalMethodId = s.CanalMethod;
            SealerId = s.Sealer;
            CementId = s.Cement;
            TechnicianId = s.Technician;
            PinId=s.Pin;
            CalciumId = s.Calcium;

            Payed = s.Payed;
            Price = s.Price;
            Expenses = s.Expenses;
            CommentText = s.CommentText;
            Images = new List<Image>();

        }
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }


        public string Title { get; set; }
        public DateTime StageDatetime { get; set; }
        public bool IsSentViaViber { get; set; }
        public int? OperationId { get; set; }
        public StageAsset Operation { get; set; } //'Реставрація' 'Плом. каналів' 'Цементування коронок'

        //***********************FOR RESTORATION********************
        public int? BondId { get; set; }

        public StageAsset Bond { get; set; }
        public int? DentinId { get; set; }

        public StageAsset Dentin { get; set; }
        public int? EnamelId { get; set; }

        public StageAsset Enamel { get; set; }


        //**********************FOR CANALS**************************
        public int? CanalMethodId { get; set; }

        public StageAsset CanalMethod { get; set; }
        public int? SealerId { get; set; }

        public StageAsset Sealer { get; set; }
        public int? CalciumId { get; set; }

        public StageAsset Calcium { get; set; }



        //**********************ЦЕМЕНТУВАННЯ КОРОНОК****************
        public int? CementId { get; set; }

        public StageAsset Cement { get; set; }
        public int? TechnicianId { get; set; }

        public StageAsset Technician { get; set; }
        public int? PinId { get; set; }
        public StageAsset Pin { get; set; }

        public int Payed { get; set; }
        public int Price { get; set; }
        public int Expenses { get; set; }
        public string CommentText { get; set; }
        public virtual Patient Patient { get; set; }
        public virtual Doctor Doctor { get; set; }
        public virtual ICollection<Image> Images { get; set; }

    }
    public class StageDTO
    {
        public StageDTO() { }
        public StageDTO(Stage s)
        {
            Id = s.Id;
            PatientId = s.PatientId;
            DoctorId = s.DoctorId;
            Title = s.Title;
            IsSentViaViber = s.IsSentViaViber;
            StageDatetime = s.StageDatetime.ToString(Options.DateTimePattern);
            Operation = s.OperationId;
            Bond = s.BondId;
            Dentin = s.DentinId;
            Enamel = s.EnamelId;
            CanalMethod = s.CanalMethodId;
            Sealer = s.SealerId;
            Cement = s.CementId;
            Calcium = s.CalciumId;

            Technician = s.TechnicianId;
            Payed = s.Payed;
            OldPayed = s.Payed;
            Price = s.Price;
            OldPrice = s.Price;
            Expenses=s.Expenses;
            OldExpenses = s.Expenses;

            CommentText = s.CommentText;
            Pin = s.PinId;
            DoctorName = s.Doctor.Name;
        }
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }

        public bool IsSentViaViber { get; set; }
        public string Title { get; set; }
        public string StageDatetime { get; set; }
        public int? Operation { get; set; } //'Реставрація' 'Плом. каналів' 'Цементування коронок'

        //***********************FOR RESTORATION********************
        public int? Bond { get; set; }
        public int? Dentin { get; set; }
        public int? Enamel { get; set; }

        //**********************FOR CANALS**************************
        public int? CanalMethod { get; set; }
        public int? Sealer { get; set; }
        public int? Pin { get; set; }


        //**********************ЦЕМЕНТУВАННЯ КОРОНОК****************
        public int? Cement { get; set; }
        public int? Calcium { get; set; }
        public int? Technician { get; set; }


        public int Payed { get; set; }
        public int OldPayed { get; set; }

        public int Price { get; set; }
        public int OldPrice { get; set; }

        public int Expenses { get; set; }
        public int OldExpenses { get; set; }


        public string CommentText { get; set; }
        public string DoctorName { get; set; }
    }
}
