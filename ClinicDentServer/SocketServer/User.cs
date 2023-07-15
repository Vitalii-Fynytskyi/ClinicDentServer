﻿using ClinicDentServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Threading;

namespace ClinicDentServer.SocketServer
{
    public class User
    {
        const string DateStringPattern = "yyyy-MM-dd";
        public Server Server { get; set; }
        public Socket Socket { get; set; }
        public Models.DoctorUser user { get; set; }
        public string ConnectionString { get; set; }
        Decoder utf8Decoder = Encoding.UTF8.GetDecoder();

        static string commandDelimeter;
        static string packetDeleimeter;
        static User()
        {
            commandDelimeter = new string((char)1, 1);
            packetDeleimeter = new string((char)2, 1);
        }

        /// used in client-server code
        byte[] bytesIn;
        int bytesRec;
        string stringIn;
        string[] presplitArray;
        string[] splStr;
        System.Text.Encoding encoding;

        private object socketLocker;
        Thread thread;

        public bool IsTrustedConnection { get; set; } = false;
        public System.Timers.Timer timerUntilDisconnect { get; set; }
        public User(Server server, Socket socket)
        {
            bytesIn = new byte[1024];
            socketLocker = new object();
            Server = server;
            Socket = socket;
            thread = new Thread(userStartInit);
            timerUntilDisconnect = new System.Timers.Timer();
            timerUntilDisconnect.Interval = 4000;
            timerUntilDisconnect.AutoReset = false;
            timerUntilDisconnect.Elapsed += TimerUntilDisconnect_Elapsed;

            thread.Start();
            timerUntilDisconnect.Start();
        }

        private void TimerUntilDisconnect_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            leaveDetected();
        }

        public void userStartInit()
        {
            encoding = Encoding.UTF8;
            while (true)
            {
                try
                {
                    if (Socket == null)
                    {
                        return;
                    }
                    bytesRec = Socket.Receive(bytesIn);
                }
                catch
                {
                    Server.PrintMessage("User disconnected");
                    leaveDetected();
                    return;
                }
                if (bytesRec <= 0)
                {
                    leaveDetected();
                    return;
                }
                // Decode the received bytes into characters
                int charCount = utf8Decoder.GetCharCount(bytesIn, 0, bytesRec);
                char[] chars = new char[charCount];
                utf8Decoder.GetChars(bytesIn, 0, bytesRec, chars, 0);
                stringIn += new string(chars);

                if (stringIn.Length > 2000)
                {
                    leaveDetected();
                    return;
                }
                if (stringIn.EndsWith(packetDeleimeter))
                {
                    presplitArray = stringIn.Split(new char[] { packetDeleimeter[0] }, StringSplitOptions.RemoveEmptyEntries);
                    stringIn = "";
                    foreach (string packet in presplitArray)
                    {
                        recodeStringInit(packet);
                    }
                }
                if (IsTrustedConnection == true)
                    break;
            }
            userDataReceiveListener();
        }
        public void userDataReceiveListener()
        {
            while (true)
            {
                try
                {
                    if (Socket == null)
                    {
                        return;
                    }

                    bytesRec = Socket.Receive(bytesIn);
                }
                catch
                {
                    Server.PrintMessage("User disconnected");
                    leaveDetected();
                    return;
                }
                if (bytesRec <= 0)
                {
                    leaveDetected();
                    return;
                }
                // Decode the received bytes into characters

                int charCount = utf8Decoder.GetCharCount(bytesIn, 0, bytesRec);
                char[] chars = new char[charCount];
                utf8Decoder.GetChars(bytesIn, 0, bytesRec, chars, 0);
                stringIn += new string(chars);

                if (stringIn.EndsWith(packetDeleimeter))
                {
                    presplitArray = stringIn.Split(new char[] { packetDeleimeter[0] }, StringSplitOptions.RemoveEmptyEntries);
                    stringIn = "";
                    foreach (string packet in presplitArray)
                    {
                        recodeString(packet);
                    }
                }
            }
        }
        public void recodeStringInit(string recodingString)
        {
            //Server.PrintMessage($" -> {recodingString}");
            splStr = recodingString.Split(commandDelimeter[0]);
            if (splStr[splStr.Length - 1] != Server.RequiredClientVersion)
            {
                answerClientIsOutOfDate();
                return;

            }
            switch (splStr[0])
            {
                case "login":
                    logining(splStr[1]);
                    break;
                default:
                    leaveDetected();
                    break;

            }
        }
        public void recodeString(string recodingString)
        {
            //Server.PrintMessage($" -> {recodingString}");
            splStr = recodingString.Split(commandDelimeter[0]);
            if (splStr[splStr.Length - 1] != Server.RequiredClientVersion)
            {
                answerClientIsOutOfDate();
                return;
            }
            switch (splStr[0])
            {
                case "login":
                    logining(splStr[1]);
                    break;
                case "scheduleAddRecord":
                    answerScheduleAddRecord(splStr[1], splStr[2], splStr[3], splStr[4], splStr[5], splStr[6], splStr[7], splStr[8], splStr[9], splStr[10]);
                    break;
                case "scheduleDeleteRecord":
                    answerScheduleDeleteRecord(splStr[1]);
                    break;
                case "scheduleUpdateRecord":
                    answerScheduleUpdateRecord(splStr[1], splStr[2], splStr[3], splStr[4], splStr[5], splStr[6], splStr[7], splStr[8], splStr[9], splStr[10]);
                    break;
                case "scheduleUpdateRecordState":
                    answerScheduleUpdateRecordState(splStr[1], splStr[2]);
                    break;
                case "scheduleUpdateRecordComment":
                    answerScheduleUpdateRecordComment(splStr[1], splStr[2]);
                    break;
                default:
                    leaveDetected();
                    break;

            }
        }

        #region Answers
        private void answerScheduleUpdateRecordComment(string recordIdStr, string newComment)
        {
            int recordId = Int32.Parse(recordIdStr);
            ClinicContext db = new ClinicContext(ConnectionString);
            Schedule schedule = db.Schedules.FirstOrDefault(x => x.Id == recordId);
            schedule.Comment = newComment;
            db.SaveChanges();
            lock (Server.UsersLocker)
            {
                foreach (User user in Server.Users)
                {
                    if (user != this)
                        user.send("scheduleRecordCommentUpdated", recordIdStr, newComment, schedule.StartDatetime.ToString(DateStringPattern), schedule.CabinetId.ToString());
                }
            }
            db.Dispose();
        }
        private void answerScheduleUpdateRecordState(string recordIdStr, string newState)
        {
            int recordId = Int32.Parse(recordIdStr);
            ClinicContext db = new ClinicContext(ConnectionString);
            Schedule schedule = db.Schedules.FirstOrDefault(x => x.Id == recordId);
            schedule.State = (SchedulePatientState)Int32.Parse(newState);
            db.SaveChanges();
            lock (Server.UsersLocker)
            {
                foreach (User user in Server.Users)
                {
                    if (user != this)
                        user.send("scheduleRecordStateUpdated", recordIdStr, newState, schedule.StartDatetime.ToString(DateStringPattern), schedule.CabinetId.ToString());
                }
            }
            db.Dispose();
        }
        private void answerScheduleUpdateRecord(string id, string startDateTime, string endDateTime, string comment, string patientId, string doctorId, string patientName, string cabinetId, string cabinetName, string state)
        {
            ScheduleDTO scheduleDTO = new ScheduleDTO(id, startDateTime, endDateTime, comment, patientId, doctorId, patientName, cabinetId, cabinetName, state);
            Schedule schedule = new Schedule(scheduleDTO);
            ClinicContext db = new ClinicContext(ConnectionString);
            db.Schedules.Update(schedule);
            db.SaveChanges();
            string patientIdToSend = "<null>";
            if (scheduleDTO.PatientId != null)
            {
                patientIdToSend = scheduleDTO.PatientId.ToString();
            }
            lock (Server.UsersLocker)
            {
                foreach (User user in Server.Users)
                {
                    if (user != this)
                        user.send("scheduleRecordUpdated", scheduleDTO.Id.ToString(), scheduleDTO.StartDatetime, scheduleDTO.EndDatetime, scheduleDTO.Comment, patientIdToSend, scheduleDTO.DoctorId.ToString(), scheduleDTO.PatientName, scheduleDTO.CabinetId.ToString(), scheduleDTO.CabinetName, ((int)scheduleDTO.State).ToString());
                }
            }
            db.Dispose();
        }
        private void answerScheduleDeleteRecord(string recordIdStr)
        {
            int recordId = Int32.Parse(recordIdStr);
            ClinicContext db = new ClinicContext(ConnectionString);
            Schedule schedule = db.Schedules.FirstOrDefault(x => x.Id == recordId);
            if (schedule == null)
            {
                db.Dispose();
                return;
            }
            db.Schedules.Remove(schedule);
            db.SaveChanges();
            lock (Server.UsersLocker)
            {
                foreach (User user in Server.Users)
                {
                    if (user != this)
                        user.send("scheduleRecordDeleted", recordIdStr, schedule.StartDatetime.ToString(DateStringPattern), schedule.CabinetId.ToString());
                }
            }
            db.Dispose();
        }
        private void answerScheduleAddRecord(string id, string startDateTime, string endDateTime, string comment, string patientId, string doctorId, string patientName, string cabinetId, string cabinetName, string state)
        {
            ScheduleDTO scheduleDTO = new ScheduleDTO(id, startDateTime, endDateTime, comment, patientId, doctorId, patientName, cabinetId, cabinetName, state);
            Schedule schedule = new Schedule(scheduleDTO);
            ClinicContext db = new ClinicContext(ConnectionString);
            db.Schedules.Add(schedule);
            db.SaveChanges();
            scheduleDTO.Id = schedule.Id;
            string patientIdToSend = "<null>";
            if (scheduleDTO.PatientId != null)
            {
                patientIdToSend = scheduleDTO.PatientId.ToString();
            }
            lock (Server.UsersLocker)
            {
                foreach (User user in Server.Users)
                {
                    user.send("scheduleRecordAdded", scheduleDTO.Id.ToString(), scheduleDTO.StartDatetime, scheduleDTO.EndDatetime, scheduleDTO.Comment, patientIdToSend,scheduleDTO.DoctorId.ToString(), scheduleDTO.PatientName, scheduleDTO.CabinetId.ToString(), scheduleDTO.CabinetName, ((int)scheduleDTO.State).ToString());
                }
            }
            db.Dispose();
        }
        private void answerClientIsOutOfDate()
        {
            send("clientOutOfDate");
        }

        public void logining(string jwtToken)
        {
            ///decode jwt token, receive Email and ConnectionString
            ClaimsPrincipal claimsPrincipal = null;
            try
            {
                claimsPrincipal = DecodeJwtToken(jwtToken);
            }
            catch
            {
                send("wrongLogin");
                return;
            }

            ///From ClinicDentUsersContext receive user id
            string email = claimsPrincipal.Identity.Name;
            ClinicDentUsersContext clinicDentUsersContext = new ClinicDentUsersContext(Startup.Configuration["ConnectionStrings:AllUsers"]);
            DoctorUser doctorUser = clinicDentUsersContext.Doctors.FirstOrDefault(d=>d.Email==email);
            clinicDentUsersContext.Dispose();

            if (doctorUser == null)
            {
                send("wrongLogin");
                return;
            }
            timerUntilDisconnect.Stop();
            user = doctorUser;
            ConnectionString = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value;
            lock (Server.UsersLocker)
            {
                if (Server.Users.Contains(this) == false)
                    Server.Users.Add(this);
            }
            send("successLogin");
            IsTrustedConnection = true;
        }
        #endregion
        public void send(params string[] list)
        {
            lock (socketLocker)
            {
                if (Socket == null) return;
                string toSend = String.Join(commandDelimeter, list) + packetDeleimeter;
                byte[] bytes = encoding.GetBytes(toSend);
                try
                {
                    Socket.Send(bytes);
                }
                catch
                {

                }
            }
        }
        public void leaveDetected()
        {
            Socket?.Close();
            Socket = null;
            lock (Server.UsersLocker)
            {
                Server.Users.Remove(this);
            }
        }
        public static ClaimsPrincipal DecodeJwtToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(AuthOptions.KEY);
            var tokenSecure = handler.ReadToken(token) as SecurityToken;
            var validationParameters = new TokenValidationParameters()
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = AuthOptions.ISSUER,
                ValidateAudience = true,
                ValidAudience = AuthOptions.AUDIENCE,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var claimsPrincipal = handler.ValidateToken(token, validationParameters, out tokenSecure);

            return claimsPrincipal;
        }
    }
}
