using ClinicDentServer.Interfaces.Repositories;
using ClinicDentServer.Interfaces.Services;
using ClinicDentServer.Models;
using ClinicDentServer.RequestCustomAnswers;
using ClinicDentServer.Requests;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicDentServer.Services
{
    public class StagesService : IStagesService
    {
        Lazy<IStageRepository<Stage>> stageRepository;
        Lazy<IDefaultRepository<Tooth>> teethRepository;
        public StagesService(Lazy<IStageRepository<Stage>> stageRepositoryToSet, Lazy<IDefaultRepository<Tooth>> teethRepositoryToSet)
        {
            stageRepository = stageRepositoryToSet;
            teethRepository = teethRepositoryToSet;
        }
        public async Task<PutStagesRequestAnswer> PutMany(PutStagesRequest putStagesRequest)
        {
            DateTime now = DateTime.Now;
            List<int> stageIds = putStagesRequest.stageDTO.Select(dto => dto.Id).ToList();
            List<Stage> existingStages = new List<Stage>();
            try
            {
                // Load existing stages including Teeth associations
                existingStages = await stageRepository.Value.dbSet
                    .Include(s => s.Teeth)
                    .Include(s => s.Doctor)
                    .Where(s => stageIds.Contains(s.Id))
                    .ToListAsync();

                List<int> conflictedStageIds = new List<int>();

                foreach (var dto in putStagesRequest.stageDTO)
                {
                    var stage = existingStages.FirstOrDefault(s => s.Id == dto.Id);
                    if (stage != null)
                    {
                        // Check for concurrency conflict
                        if (stage.LastModifiedDateTime > DateTime.ParseExact(dto.LastModifiedDateTime, Options.ExactDateTimePattern, CultureInfo.InvariantCulture))
                        {
                            conflictedStageIds.Add(stage.Id);
                            continue; // Skip this stage
                        }

                        // Update stage properties
                        stage.UpdateFromDTO(dto);

                        // Update Teeth associations
                        var toothIds = dto.TeethNumbers ?? new List<byte>();
                        var teeth = await teethRepository.Value.dbSet.Where(t => toothIds.Contains((byte)t.Id)).ToListAsync();

                        // Clear existing associations
                        stage.Teeth.Clear();

                        // Add new associations
                        foreach (var tooth in teeth)
                        {
                            stage.Teeth.Add(tooth);
                        }

                        // Update LastModifiedDateTime
                        stage.LastModifiedDateTime = now;
                        await stageRepository.Value.Update(stage);
                    }
                }


                if (conflictedStageIds.Count > 0)
                {
                    return new PutStagesRequestAnswer()
                    {
                        ConflictedStagesIds = conflictedStageIds,
                        NewLastModifiedDateTime = now.ToString(Options.ExactDateTimePattern)
                    };
                }
                else
                {
                    return new PutStagesRequestAnswer()
                    {
                        NewLastModifiedDateTime = now.ToString(Options.ExactDateTimePattern)
                    };
                }
            }
            finally
            {
                StringBuilder stringBuilder = new StringBuilder(32);
                for (int i = 0; i < existingStages.Count; i++)
                {
                    if (putStagesRequest.stageDTO[i].OldPrice != putStagesRequest.stageDTO[i].Price || putStagesRequest.stageDTO[i].Payed != putStagesRequest.stageDTO[i].OldPayed || putStagesRequest.stageDTO[i].Expenses != putStagesRequest.stageDTO[i].OldExpenses)
                    {
                        int expensesDifference = putStagesRequest.stageDTO[i].Expenses - putStagesRequest.stageDTO[i].OldExpenses;
                        int priceDifference = putStagesRequest.stageDTO[i].Price - putStagesRequest.stageDTO[i].OldPrice;
                        int payedDifference = putStagesRequest.stageDTO[i].Payed - putStagesRequest.stageDTO[i].OldPayed;

                        stringBuilder.Append($"{putStagesRequest.stageDTO[i].PatientId},{putStagesRequest.stageDTO[i].StageDatetime},{priceDifference},{payedDifference},{putStagesRequest.stageDTO[i].DoctorId},{expensesDifference}");
                    }
                }
                Program.TcpServer.SendToAll("stagePayInfoUpdated", stringBuilder.ToString());

            }
        }
    }
}
