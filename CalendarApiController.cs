using AutoMapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using WebRTC.Core;
using WebRTC.Core.Entities;
using WebRTC.Data.Abstracts;
using WebRTC.Models;

namespace WebRTC.Controllers
{
    /// <summary>
    /// CalenderApiController
    /// </summary>
    [Authorize]
    public class CalendarApiController : BaseApiController
    {
        private readonly IGroupRepository _groupRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly IPresetDateListRepository _presetListRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IGTAppointmentRepository _gtAppointmentRepository;
        private readonly IAppointmentReminderRepository _appointmentReminderRepository;
        private readonly IMapper _mapper;
        /// <summary>
        /// Contructor
        /// </summary>
        /// <param name="groupRepository"></param>
        /// <param name="teamRepository"></param>
        /// <param name="presetListRepository"></param>
        /// <param name="appointmentRepository"></param>
        /// <param name="gtAppointmentRepository"></param>
        /// <param name="appointmentReminderRepository"></param>
        /// <param name="mapper"></param>
        public CalendarApiController(
            IGroupRepository groupRepository,
            ITeamRepository teamRepository,
            IPresetDateListRepository presetListRepository,
            IAppointmentRepository appointmentRepository,
            IGTAppointmentRepository gtAppointmentRepository,
            IAppointmentReminderRepository appointmentReminderRepository,
            IMapper mapper)
        {
            _groupRepository = groupRepository;
            _teamRepository = teamRepository;
            _presetListRepository = presetListRepository;
            _appointmentRepository = appointmentRepository;
            _gtAppointmentRepository = gtAppointmentRepository;
            _appointmentReminderRepository = appointmentReminderRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// This api will Return Appointment
        /// </summary>
        /// <param name="gtId">GroupTeamId</param>
        /// <param name="gtType">Group Team Type</param>
        /// <returns></returns>
        [SwaggerOperation("GetAll")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("GetAll")]
        public IHttpActionResult GetAll(string gtId, string gtType)
        {
            if (gtType == "File")
            {
                var records =
                    _appointmentRepository
                    .FindBy(x => x.UserID == UserId && (string.IsNullOrEmpty(x.AcceptStatus) || x.AcceptStatus == "accept"))
                    .ToList();
                return Ok(records);
            }
            else
            {
                var records = _gtAppointmentRepository.FindBy(x => x.GTID == gtId).ToList();
                return Ok(records);
            }
        }

        /// <summary>
        /// This api will Return Id and Group Type
        /// </summary>
        /// <param name="id">Single Id</param>
        /// <param name="gtType">Group team Type</param>
        /// <returns></returns>
        [SwaggerOperation("Get")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("Get")]
        public IHttpActionResult Get(string id, string gtType)
        {
            var appointment = new AppointmentViewModel();
            if (WebRTCConstants.GTTypeFile == gtType)
                appointment = _mapper.Map<Appointment, AppointmentViewModel>(_appointmentRepository.GetSingle(id));
            else
                appointment = _mapper.Map<GTAppointment, AppointmentViewModel>(_gtAppointmentRepository.GetSingle(id));

            if (appointment == null)
                appointment = new AppointmentViewModel();
            else appointment.entity_state = EntityState.Modified;

            return Ok(appointment);
        }

        /// <summary>
        /// This api will save Appointment
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [SwaggerOperation("Save")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("Save")]
        public IHttpActionResult Save(AppointmentBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("<br>", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return BadRequest(errors);
            }

            if(WebRTCConstants.GTTypeFile.Equals(model.gttype, StringComparison.OrdinalIgnoreCase))
            {
                var appointment = _mapper.Map<AppointmentBindingModel, Appointment>(model);
                _appointmentRepository.Save(appointment);
            }
            else
            {
                var appointment = _mapper.Map<AppointmentBindingModel, GTAppointment>(model);
                _gtAppointmentRepository.Save(appointment);
            }

            return Ok(model);
        }

        /// <summary>
        /// This api will delete Single Id and Group team type
        /// </summary>
        /// <param name="id">Single Id</param>
        /// <param name="gtType">Group team Type</param>
        /// <returns></returns>
        [SwaggerOperation("Delete")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("Delete")]
        public IHttpActionResult Delete(string id, string gtType)
        {
            if(WebRTCConstants.GTTypeFile == gtType)
            {
                var appointment = _appointmentRepository.GetSingle(id);

                if (appointment == null)
                    return BadRequest("No appointment found.");

                appointment.EntityState = EntityState.Deleted;
                _appointmentRepository.Save(appointment);
            }
            else
            {
                var appointment = _gtAppointmentRepository.GetSingle(id);

                if (appointment == null)
                    return BadRequest("No appointment found.");

                appointment.EntityState = EntityState.Deleted;
                _gtAppointmentRepository.Save(appointment);
            }

            return Ok();
        }

        /// <summary>
        /// This api will Return Sharing
        /// </summary>
        /// <returns></returns>
        [SwaggerOperation("GetSharing")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("GetSharing")]
        public IHttpActionResult GetSharing()
        {
            var records = _appointmentRepository.FindBy(x => x.UserID == UserId && x.AcceptStatus == "Sharing").ToList();
            return Ok(new { Records = records, Count = records.Count() });
        }

        /// <summary>
        /// This api will Share a calendar appointment
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [SwaggerOperation("Share")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("Share")]
        public IHttpActionResult Share(SharingAppointmentBindingModel model)
        {
            string[] gidsList = model.gids.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            string[] tidsList = model.tids.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            string[] cidsList = model.cids.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            var appointment = new SharingAppoinmentViewModel();
            var gtAppointmentList = new List<GTAppointment>();
            var appointmentList = new List<Appointment>();

            if (WebRTCConstants.GTTypeFile.Equals(model.GTType, StringComparison.OrdinalIgnoreCase))
            {
                var tempAppointment = _appointmentRepository.GetSingle(model.canid);
                appointment.Title = tempAppointment.Title;
                appointment.Description = tempAppointment.Description;
                appointment.Date = tempAppointment.Date;
                appointment.EndDate = tempAppointment.EndDate;
            }
            else
            {
                var tempAppointment = _gtAppointmentRepository.GetSingle(model.canid);
                appointment.Title = tempAppointment.Title;
                appointment.Description = tempAppointment.Description;
                appointment.Date = tempAppointment.Date;
                appointment.EndDate = tempAppointment.EndDate;
            }

            foreach (string gid in gidsList)
            {
                var item = new GTAppointment
                {
                    UserID = UserId,
                    UserName = UserName,
                    GTID = gid,
                    GTType = "Group",
                    Title = appointment.Title,
                    Description = appointment.Description,
                    Date = appointment.Date,
                    EndDate = appointment.EndDate
                };
                gtAppointmentList.Add(item);
            }
            foreach (string tid in tidsList)
            {
                var item = new GTAppointment
                {
                    UserID = UserId,
                    UserName = UserName,
                    GTID = tid,
                    GTType = "Team",
                    Title = appointment.Title,
                    Description = appointment.Description,
                    Date = appointment.Date,
                    EndDate = appointment.EndDate
                };
                gtAppointmentList.Add(item);
            }
            foreach (string cid in cidsList)
            {
                var item = new Appointment
                {
                    UserID = cid,
                    Title = appointment.Title,
                    Description = appointment.Description,
                    Date = appointment.Date,
                    AcceptStatus = "Sharing",
                    SharerUserID = UserId,
                    SharerUserName = UserName,
                    EndDate = appointment.EndDate
                };
                appointmentList.Add(item);
            }

            _appointmentRepository.AddMany(appointmentList);
            _gtAppointmentRepository.AddMany(gtAppointmentList);

            return Ok();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="preIds">Comma separated Preset Ids</param>
        /// <param name="GTId">Group Team Id</param>
        /// <param name="GTType">Group Team Type</param>
        /// <returns></returns>
        [SwaggerOperation("ChoosePresets")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost]
        public IHttpActionResult ChoosePresets(string preIds, string GTId, string GTType)
        {
            string[] preIdList = preIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            var gtAppintments = _gtAppointmentRepository.FindBy(x => preIdList.Contains(x.Id)).ToList();

            if (WebRTCConstants.GTTypeFile.Equals(GTType, StringComparison.OrdinalIgnoreCase))
            {
                var appointmentList = new List<Appointment>();

                foreach (var appointment in gtAppintments)
                {
                    var item = new Appointment
                    {
                        UserID = UserId,
                        Title = appointment.Title,
                        Description = appointment.Description,
                        Date = appointment.Date,
                        EndDate = appointment.EndDate
                    };
                    appointmentList.Add(item);
                }

                _appointmentRepository.AddMany(appointmentList);
            }
            else
            {
                var gtAppointmentList = new List<GTAppointment>();
                foreach (var appointment in gtAppintments)
                {
                    var item = new GTAppointment
                    {
                        UserID = UserId,
                        UserName = UserName,
                        GTID = GTId,
                        GTType = GTType,
                        Title = appointment.Title,
                        Description = appointment.Description,
                        Date = appointment.Date,
                        EndDate = appointment.EndDate
                    };
                    gtAppointmentList.Add(item);
                }
                _gtAppointmentRepository.AddMany(gtAppointmentList);
            }

            return Ok();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="calendar_id">Calender Id</param>
        /// <param name="type">Either Refuse or Accept Type</param>
        /// <returns></returns>
        [SwaggerOperation("Options")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("Options")]
        public IHttpActionResult Options(string calendar_id, string type)
        {
            var appointment = _appointmentRepository.GetSingle(calendar_id);
            appointment.EntityState = EntityState.Modified;

            if (type == "refuse")
            {
                appointment.AcceptStatus = "refuse";
            }
            else if (type == "accept")
            {
                appointment.AcceptStatus = "accept";
            }

            _appointmentRepository.Save(appointment);

            return Ok();
        }

        /// <summary>
        /// This api will Return Preset list
        /// </summary>
        /// <returns></returns>
        [SwaggerOperation("GetPresetList")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("GetPresetList")]
        public IHttpActionResult GetPresetList()
        {
            var pList = _presetListRepository.GetAll();
            return Ok(pList);
        }

        /// <summary>
        /// This api will save Preset List
        /// </summary>
        /// <param name="id">Single or Null Id</param>
        /// <param name="name">Name</param>
        /// <returns></returns>
        [SwaggerOperation("AddPresetList")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("AddPresetList")]
        public IHttpActionResult AddPresetList(string id, string name)
        {
            if (string.IsNullOrEmpty(id))
            {
                var exists = _presetListRepository.Exists(x => x.Name == name);
                if (exists)
                    return Ok(new { Successful = false, Msg = "This name is exists." });

                var preList = new PresetDateList { Name = name };
                _presetListRepository.Save(preList);
            }
            else
            {
                var preList = _presetListRepository.GetSingle(id);
                preList.Name = name;
                preList.EntityState = EntityState.Modified;

                _presetListRepository.Save(preList);
            }

            return Ok(new { Successful = true });
        }

        /// <summary>
        /// This api will Delete Preset List
        /// </summary>
        /// <param name="Id">Preset List Id</param>
        /// <returns></returns>
        [SwaggerOperation("DeletePresetList")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("DeletePresetList")]
        public IHttpActionResult DeletePresetList(string Id)
        {
            var presetList = _presetListRepository.GetSingle(Id);
            if (presetList != null)
            {
                var appointments = _gtAppointmentRepository.FindBy(x => x.ListFor == Id).ToList();
                if (appointments != null && appointments.Count > 0)
                    appointments.ForEach(a => a.ListFor = "");

                presetList.EntityState = EntityState.Deleted;
                _presetListRepository.Save(presetList);
                _gtAppointmentRepository.UpdateMany(appointments);

                return Ok(new { Successful = true });
            }

            return Ok(new { Successful = false });
        }

        #region AppointmentReminders

        /// <summary>
        /// This api will return appointment reminders
        /// </summary>
        /// <returns></returns>
        [SwaggerOperation("GetAppointmentReminders")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("GetAppointmentReminders")]
        public IHttpActionResult GetAppointmentReminders()
        {
            var items = _appointmentReminderRepository.GetAll();
            return Ok(items);
        }

        // api/CalendarApi/GetAppointmentReminder?appointmentId=(string)
        [HttpGet, ActionName("GetAppointmentReminder")]
        public IHttpActionResult GetAppointmentReminder(string appointmentId)
        {
            var item = _appointmentReminderRepository.GetSingle(x => x.AppointmentId == appointmentId, true);
            if (!string.IsNullOrEmpty(item.AppointmentId))
                item.EntityState = EntityState.Modified;

            var reminder = new AppointmentReminderViewModel
            {
                id = item.Id,
                appointment_id = item.AppointmentId,
                appointment_type = item.AppointmentType,
                weeks = item.Weeks.ToString(),
                days = item.Days.ToString(),
                hours = item.Hours.ToString(),
                minutes = item.Minutes.ToString(),
                created_on = JsonConvert.SerializeObject(item.CreatedOn),
                created_on_str = item.CreatedOnStr,
                user_id = UserId,
                entity_state = item.EntityState
            };

            return Ok(reminder);
        }

        /// <summary>
        /// This api will save Appointment Reminders
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
         [SwaggerOperation("SaveAppointmentReminders")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("SaveAppointmentReminder")]
        public IHttpActionResult SaveAppointmentReminder(AppointmentReminderBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("<br>", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return BadRequest(errors);
            }

            var reminder = new AppointmentReminder
            {
                Id = model.id,
                AppointmentId = model.appointment_id,
                AppointmentType = model.appointment_type,
                Weeks = model.weeks,
                Days = model.days,
                Hours = model.hours,
                Minutes = model.minutes,
                CreatedOn = (DateTime)JsonConvert.DeserializeObject(model.created_on),
                CreatedOnStr = model.created_on_str,
                EntityState = model.entity_state
            };

            _appointmentReminderRepository.Save(reminder);

            return Ok(model);
        }
        #endregion
    }
}