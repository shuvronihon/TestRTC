using AutoMapper;
using Microsoft.AspNet.Identity;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using WebRTC.Data.Abstracts;
using WebRTC.Data.Dtos;
using WebRTC.Infrastructure.Helpers;
using WebRTC.Models;

namespace WebRTC.Controllers
{
    /// <summary>
    /// GroTeaApiController
    /// </summary>
    [Authorize]
    public class GroTeaApiController : BaseApiController
    {
        private readonly IGroupRepository _groupRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly IGTContactMessageRepository _gtContactMessageRepository;
        private readonly IUserAssignGroupRepository _userGroupRepository;
        private readonly IUserAssignTeamRepository _userTeamRepository;
        private readonly IPushNotificationHelper _pushNotificationHelper;
        private readonly IMapper _mapper;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="groupReposotory"></param>
        /// <param name="teamRepository"></param>
        /// <param name="userGroupRepository"></param>
        /// <param name="userTeamRepository"></param>
        /// <param name="gtContactMessageRepository"></param>
        /// <param name="pushNotificationHelper"></param>
        /// <param name="mapper"></param>
        public GroTeaApiController(
            IGroupRepository groupReposotory,
            ITeamRepository teamRepository,
            IUserAssignGroupRepository userGroupRepository,
            IUserAssignTeamRepository userTeamRepository,
            IGTContactMessageRepository gtContactMessageRepository,
            IPushNotificationHelper pushNotificationHelper,
            IMapper mapper)
        {
            _groupRepository = groupReposotory;
            _teamRepository = teamRepository;
            _gtContactMessageRepository = gtContactMessageRepository;
            _userGroupRepository = userGroupRepository;
            _userTeamRepository = userTeamRepository;
            _pushNotificationHelper = pushNotificationHelper;
            _mapper = mapper;
        }

        /// <summary>
        /// This api will return a Group
        /// </summary>
        /// <param name="id">Group Id</param>
        /// <returns></returns>
        [SwaggerOperation("GetGroup")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet]
        public IHttpActionResult GetGroup(string id)
        {
            var group = _groupRepository.GetSingle(id);
            return Ok(group);
        }

        /// <summary>
        /// this api will return a Team 
        /// </summary>
        /// <param name="id">Team Id</param>
        /// <returns></returns>
        [SwaggerOperation("UploadCameraFile")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet]
        public IHttpActionResult GetTeam(string id)
        {
            var team = _teamRepository.GetSingle(id);
            return Ok(team);
        }

        /// <summary>
        /// This api will return list of contact messages
        /// </summary>
        /// <param name="Id">Contact Message Id</param>
        /// <param name="tryCount"></param>
        /// <returns></returns>
        [SwaggerOperation("UploadCameraFile")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("GetChatMessage")]
        public IHttpActionResult GetChatMessage(string Id, int tryCount)
        {
            var messages = new List<Core.Entities.GTContactMessage>();

            if (tryCount == 1)
                messages = _gtContactMessageRepository.FindBy(x => x.GTID == Id).OrderBy(x => x.CreateOn).Take(50).ToList();
            else
                messages = _gtContactMessageRepository.FindBy(x => x.GTID == Id && x.MessageStatus.Contains(UserName + ",,,")).OrderBy(x => x.CreateOn).ToList();

            var messagesToUpdate = new List<Core.Entities.GTContactMessage>();
            var contactMessages = new List<GTContactMessageViewModel>();
            foreach (var item in messages)
            {
                if (!item.MessageStatus.Contains(UserName + ",,,"))
                {
                    item.MessageStatus += UserName + ",,,";
                    messagesToUpdate.Add(item);
                }

                var contactMessage = _mapper.Map<Core.Entities.GTContactMessage, GTContactMessageViewModel>(item);
                if (item.MessageType == "File" && item.MessageContent.Contains(","))
                {
                    string[] sss = item.MessageContent.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                    contactMessage.MessageContent = sss[0];
                    contactMessage.Key1 = sss[1];
                }
                contactMessages.Add(contactMessage);
            }

            _gtContactMessageRepository.AddMany(messagesToUpdate);

            return Ok(contactMessages);
        }

        /// <summary>
        /// This api will send Messages
        /// </summary>
        /// <param name="Id">Group Team Id</param>
        /// <param name="GTType">Group Team Type</param>
        /// <param name="MessageType">Message type  is "File or Text or Audio Type</param>
        /// <param name="MessageContent">Message Count</param>
        /// <returns></returns>
        [SwaggerOperation("SendMessage")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("SendMessage")]
        public IHttpActionResult SendMessage(string Id, string GTType, string MessageType, string MessageContent)
        {
            string token = Guid.NewGuid().ToString();

            if (MessageType == "Text")
            {
                for (var i = 1; i <= 30; i++)
                {
                    string ii = i >= 10 ? i.ToString() : "0" + i;
                    MessageContent = MessageContent.Replace("/e" + ii, "<img src='http://ozrtc.com/Content/emoticons/e" + ii + ".png' style=''/>");
                }
            }

            var message = new Core.Entities.GTContactMessage
            {
                UserID = UserId,
                UserName = UserName,
                GTID = Id,
                GTType = GTType,
                Role = "Sender",
                MessageType = MessageType,
                MessageContent = MessageContent,
                MessageStatus = UserName + ",,,",
                Token = token
            };

            _gtContactMessageRepository.Save(message);

            var deviceTokens = new List<PushNotificationIds>();

            if (GTType == "Group")
            {
                var items = _userGroupRepository.GetPushNotificationIds(Id, UserId);
                deviceTokens = _mapper.Map<List<PushNotificationIdDTO>, List<PushNotificationIds>>(items);
            }
            else
            {
                var items = _userTeamRepository.GetPushNotificationIds(Id, UserId);
                deviceTokens = _mapper.Map<List<PushNotificationIdDTO>, List<PushNotificationIds>>(items);
            }

            var currentUser = UserManager.FindById(UserId);

            string type = "";
            if (GTType == "Group")
            {
                var group = _groupRepository.GetSingle(Id);
                type = group.Name;
            }
            else
            {
                var team = _teamRepository.GetSingle(Id);
                type = team.Name;
            }

            var messageToSend = _mapper.Map<Core.Entities.GTContactMessage, GTContactMessageViewModel>(message);
            if (MessageType == "File")
            {
                string[] sss = MessageContent.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                messageToSend.MessageContent = sss[0];
                messageToSend.Key1 = sss[1];
                var data = new
                {
                    Title = "Tucana",
                    Id = Id,
                    GTCType = GTType,
                    GTCName = type,
                    Msg = currentUser.UserName + " @ " + type + " : Send a file"
                };

                _pushNotificationHelper.SendNotification(data, deviceTokens);
            }
            else
            {
                var data = new
                {
                    Title = "Tucana",
                    Id = Id,
                    GTCType = GTType,
                    GTCName = type,
                    Msg = currentUser.UserName + " @ " + type + " : " + MessageContent
                };

                _pushNotificationHelper.SendNotification(data, deviceTokens);
            }

            return Ok(messageToSend);
        }

        /// <summary>
        /// This api will Return number of Unread messages
        /// </summary>
        /// <returns></returns>
        [SwaggerOperation("GetUnread")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("GetUnRead")]
        public IHttpActionResult GetUnRead()
        {
            var ids =
                _gtContactMessageRepository.FindBy(x => !x.MessageStatus.Contains(UserId + ",,,"))
                .Select(x => x.GTID)
                .ToList();

            return Ok(new { Ids = ids });
        }
    }
}