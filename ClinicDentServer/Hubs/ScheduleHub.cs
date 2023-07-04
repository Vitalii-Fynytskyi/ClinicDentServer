using Microsoft.AspNetCore.SignalR;
using ClinicDentServer.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ClinicDentServer.Hubs
{
    public class ScheduleHub :Hub
    {
        const string DateStringPattern= "yyyy-MM-dd";
        public async Task AddRecord(ScheduleDTO recordToAdd)
        {
            Schedule scheduleFromDTO = new Schedule(recordToAdd);
            HttpContext httpContext = Context.GetHttpContext();
            ClaimsPrincipal  user = httpContext.User;
            ClinicContext db = new ClinicContext(user.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            db.Schedules.Add(scheduleFromDTO);
            await db.SaveChangesAsync();

            recordToAdd.Id = scheduleFromDTO.Id;
            await Clients.All.SendAsync("RecordAdded", recordToAdd);
            db.Dispose();
        }
        public async Task DeleteRecord(int recordIdToDelete)
        {
            ClinicContext db = new ClinicContext(Context.GetHttpContext().User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            Schedule schedule = db.Schedules.FirstOrDefault(x => x.Id == recordIdToDelete);
            if (schedule == null)
            {
                db.Dispose();
                return;
            }
            db.Schedules.Remove(schedule);
            await db.SaveChangesAsync();
            await Clients.Others.SendAsync("RecordDeleted", recordIdToDelete, schedule.StartDatetime.ToString(DateStringPattern), schedule.CabinetId);
            db.Dispose();

        }
        public async Task UpdateRecord(ScheduleDTO recordToUpdate)
        {
            ClinicContext db = new ClinicContext(Context.GetHttpContext().User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            Schedule schedule = new Schedule(recordToUpdate);
            db.Schedules.Update(schedule);
            await db.SaveChangesAsync();
            await Clients.Others.SendAsync("RecordUpdated", recordToUpdate);
            db.Dispose();
        }
        public async Task UpdateRecordState(int recordId, SchedulePatientState newState)
        {
            ClinicContext db = new ClinicContext(Context.GetHttpContext().User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);
            Schedule schedule= await db.Schedules.FirstOrDefaultAsync(s=>s.Id == recordId);
            schedule.State = newState;
            db.SaveChanges();
            await Clients.Others.SendAsync("RecordStateUpdated", recordId, newState, schedule.StartDatetime.ToString(DateStringPattern), schedule.CabinetId);
            db.Dispose();
        }
        public async Task UpdateRecordComment(int recordId, string newComment)
        {
            ClinicContext db = new ClinicContext(Context.GetHttpContext().User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);
            Schedule schedule = await db.Schedules.FirstOrDefaultAsync(s => s.Id == recordId);
            schedule.Comment = newComment;
            db.SaveChanges();
            await Clients.Others.SendAsync("RecordCommentUpdated", recordId, newComment, schedule.StartDatetime.ToString(DateStringPattern), schedule.CabinetId);
            db.Dispose();
        }
    }
}
