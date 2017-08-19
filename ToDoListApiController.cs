using AutoMapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using WebRTC.Core.Entities;
using WebRTC.Data.Abstracts;
using WebRTC.Data.Dtos;
using WebRTC.Models;

namespace WebRTC.Controllers
{
    /// <summary>
    /// ToDoListApiController
    /// </summary>
    [Authorize]
    public class ToDoListApiController : BaseApiController
    {
        private readonly IToDoListRepository _todoListRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly ITaskReminderRepository _taskReminderRepository;
        private readonly IMapper _mapper;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="todoListRepository"></param>
        /// <param name="appointmentRepository"></param>
        /// <param name="taskReminderRepository"></param>
        /// <param name="mapper"></param>
        public ToDoListApiController(
            IToDoListRepository todoListRepository, 
            IAppointmentRepository appointmentRepository,
            ITaskReminderRepository taskReminderRepository,
            IMapper mapper)
        {
            _todoListRepository = todoListRepository;
            _appointmentRepository = appointmentRepository;
            _taskReminderRepository = taskReminderRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// This api will Add To do List
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [SwaggerOperation("Add")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost]
        public IHttpActionResult Add(TodoListBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("<br>", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return BadRequest(errors);
            }

            var todoList = new ToDoList
            {
                UserID = UserId,
                Title = model.title.Trim(),
                Description = model.description.Trim(),
                PriorityLevel = model.priority_level,
                StartDate = model.start_date,
                EndDate = model.end_date
            };

            _todoListRepository.Save(todoList);

            return Ok(todoList);
        }

        /// <summary>
        /// This api will Edit To do List
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [SwaggerOperation("Edit")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost]
        public IHttpActionResult Edit(TodoListBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("<br>", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return BadRequest(errors);
            }

            var todolist = _todoListRepository.GetSingle(model.id);
            todolist.Title = model.title.Trim();
            todolist.Description = model.description.Trim();
            todolist.PriorityLevel = model.priority_level;
            todolist.StartDate = model.start_date;
            todolist.EndDate = model.end_date;
            todolist.Status = model.status;
            todolist.EntityState = EntityState.Modified;

            _todoListRepository.Save(todolist);

            return Ok(todolist);
        }

        /// <summary>
        /// This api will Delete To Do List
        /// </summary>
        /// <param name="id">Comma separated Id</param>
        /// <returns></returns>
        [SwaggerOperation("Delete")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost]
        public IHttpActionResult Delete(string id)
        {
            var todolist = _todoListRepository.GetSingle(id);
            todolist.EntityState = EntityState.Deleted;

            _todoListRepository.Save(todolist);

            return Ok();
        }

        /// <summary>
        /// This api will Add todo List Calendar
        /// </summary>
        /// <param name="id">Comma separated Id</param>
        /// <param name="date">Appointment Date</param>
        /// <returns></returns>
        [SwaggerOperation("AddToCalendar")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost]
        public IHttpActionResult AddToCalendar(string id, string date)
        {
            var todolist = _todoListRepository.GetSingle(id);

            var appointment = new Appointment
            {
                UserID = UserId,
                Title = todolist.Title,
                Description = todolist.Description,
                Date = date
            };

            _appointmentRepository.Save(appointment);

            return Ok();
        }

        /// <summary>
        /// This api will Return TopCount to Calendar
        /// </summary>
        /// <param name="topcount">TopCount</param>
        /// <returns></returns>
        [SwaggerOperation("Get")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet]
        public IHttpActionResult Get(int topcount)
        {
            var items = _todoListRepository.GetTodListSummary(topcount, UserId);
            var todoList = _mapper.Map<IEnumerable<TodoListSummaryDTO>, IEnumerable<TodoListViewModel>>(items);

            return Ok(todoList);
        }

        /// <summary>
        /// This api will Share ToDo list
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [SwaggerOperation("Share")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost]
        public IHttpActionResult Share(SharingAppointmentBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("<br>", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return BadRequest(errors);
            }

            string[] cidsList = model.cids.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            var todolist = _todoListRepository.GetSingle(model.canid);

            var itemList = new List<ToDoList>();
            foreach (string cid in cidsList)
            {
                var item = new ToDoList
                {
                    UserID = cid,
                    Title = todolist.Title,
                    Description = todolist.Description,
                    PriorityLevel = todolist.PriorityLevel,
                    StartDate = todolist.StartDate,
                    EndDate = todolist.EndDate,
                    AcceptStatus = "Sharing",
                    SharerUserID = UserId,
                    SharerUserName = UserName
                };

                itemList.Add(item);
            }

            _todoListRepository.AddMany(itemList);

            return Ok();
        }

        /// <summary>
        /// This api will Return Sharing
        /// </summary>
        /// <returns></returns>
        [HttpGet, ActionName("GetSharing")]
        public IHttpActionResult GetSharing()
        {
            var records = _todoListRepository.FindBy(x => x.UserID == UserId && x.AcceptStatus == "Sharing").ToList();
            return Ok(new { Records = records, Count = records.Count() });
        }

        /// <summary>
        /// This api will save todo list Options
        /// </summary>
        /// <param name="cid">calendar Id</param>
        /// <param name="type">Either Refuse or Accept</param>
        /// <returns></returns>
        [SwaggerOperation("Options")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("Options")]
        public IHttpActionResult Options(string cid, string type)
        {
            var todolist = _todoListRepository.GetSingle(cid);
            todolist.EntityState = EntityState.Modified;

            if (type == "refuse")
            {
                todolist.AcceptStatus = "refuse";
            }
            else if (type == "accept")
            {
                todolist.AcceptStatus = "accept";
            }

            _todoListRepository.Save(todolist);

            return Ok();
        }

        #region TaskReminders
        /// <summary>
        /// This api will Return Task Reminders
        /// </summary>
        /// <returns></returns>
        [SwaggerOperation("GetTaskReminders")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        public IHttpActionResult GetTaskReminders()
        {
            var items = _taskReminderRepository.GetAll().ToList();
            return Ok(items);
        }

        // api/TodoListApi/GetTaskReminder?id=(string)&todoId=(string)
        public IHttpActionResult GetTaskReminder(string id, string todoId)
        {
            var item = _taskReminderRepository.GetSingle(x => x.ToDoId == todoId, true);
            if (!string.IsNullOrEmpty(item.ToDoId))
                item.EntityState = EntityState.Modified;

            var reminder = new TaskReminderViewModel
            {
                id = item.Id,
                todo_id = item.EntityState == EntityState.Added ? todoId : item.ToDoId,
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
        /// This api will Save Task Reminder
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [SwaggerOperation("SaveTaskReminders")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost]
        public IHttpActionResult SaveTaskReminder(TaskReminderBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("<br>", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return BadRequest(errors);
            }

            var reminder = new TaskReminder
            {
                Id = model.id,
                ToDoId = model.todo_id,
                Weeks = model.weeks,
                Days = model.days,
                Hours = model.hours,
                Minutes = model.minutes,
                CreatedOn = (DateTime)JsonConvert.DeserializeObject(model.created_on),
                CreatedOnStr = model.created_on_str,
                EntityState = model.entity_state
            };

            _taskReminderRepository.Save(reminder);

            return Ok(model);
        }
        #endregion
    }
}