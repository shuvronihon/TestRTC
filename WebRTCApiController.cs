using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Http;
using WebRTC.Core.Entities;
using WebRTC.Data.Abstracts;
using WebRTC.Models;

namespace WebRTC.Controllers
{
    /// <summary>
    /// WebRTCApiController
    /// </summary>
    [Authorize]
    public class WebRTCApiController : BaseApiController
    {
        private readonly IWebRTCRoomRepository _roomRepository;
        private readonly IUserRepository _userRepository;
        private readonly IWebRTCSDPMessageRepository _sdpMessageRepository;
        private readonly IWebRTCCandidatesTableRepository _candidateRepository;
        private readonly IChatMessageRepository _chatMessageRepsitory;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="roomRepository"></param>
        /// <param name="userRepository"></param>
        /// <param name="sdpMessageRepository"></param>
        /// <param name="candidateRepository"></param>
        /// <param name="chatMessageRepsitory"></param>
        public WebRTCApiController(
            IWebRTCRoomRepository roomRepository,
            IUserRepository userRepository,
            IWebRTCSDPMessageRepository sdpMessageRepository,
            IWebRTCCandidatesTableRepository candidateRepository,
            IChatMessageRepository chatMessageRepsitory)
        {
            _roomRepository = roomRepository;
            _userRepository = userRepository;
            _sdpMessageRepository = sdpMessageRepository;
            _candidateRepository = candidateRepository;
            _chatMessageRepsitory = chatMessageRepsitory;
        }

        /// <summary>
        /// This api will Make one to one Call
        /// </summary>
        /// <param name="callee_user_id">Callee User Id</param>
        /// <returns></returns>
        [SwaggerOperation("MakeACall")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("MakeACall")]
        public IHttpActionResult MakeACall(string callee_user_id)
        {
            var callee = UserManager.FindById(callee_user_id);
            string[] parts = new string[] { UserName, callee.UserName };

            string token = Guid.NewGuid().ToString();
            var room = new WebRTCRoom
            {
                Id = token,
                Token = token,
                Name = "Single-Call-Room",
                SharedWith = "public",
                Status = "available",
                LastUpdated = DateTime.Now,
                OwnerName = UserName,
                OwnerToken = UserId,
                ParticipantName = callee.UserName,
                ParticipantToken = callee.Id,
                Participants = string.Join(",", parts),
            };

            _roomRepository.Save(room);

            return Ok(room);
        }

        /// <summary>
        /// This api will make Many to Many call 
        /// </summary>
        /// <param name="callee_user_id_list"></param>
        /// <returns></returns>
        [SwaggerOperation("MakeMMCall")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("MakeMMCall")]
        public IHttpActionResult MakeMMCall(string callee_user_id_list)
        {
            string[] callee_id_list = callee_user_id_list.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            var callee_list = _userRepository.GetAllUsers(callee_id_list).ToList();

            string[] parts = new string[callee_list.Count + 1];
            parts[0] = UserName;
            for (int i = 1; i <= callee_list.Count; i++)
            {
                parts[i] = callee_list[i - 1].UserName;
            }

            string token = Guid.NewGuid().ToString();
            var rooms = new List<WebRTCRoom>();
            foreach (var callee in callee_list)
            {
                var item = new WebRTCRoom
                {
                    Token = token,
                    Name = "MM-Call-Room",
                    SharedWith = "public",
                    Status = "available",
                    LastUpdated = DateTime.Now,
                    OwnerName = UserName,
                    OwnerToken = UserId,
                    ParticipantName = callee.UserName,
                    ParticipantToken = callee.Id,
                    Participants = string.Join(",", parts),
                };

                rooms.Add(item);
            }

            _roomRepository.AddMany(rooms);

            return Ok(new { roomtoken = token });
        }

        /// <summary>
        /// This api will Call Group or Team
        /// </summary>
        /// <param name="id">Group or Team Id</param>
        /// <param name="type">Type is either Group or Team</param>
        /// <returns></returns>
        [SwaggerOperation("CallGroupOrTeam")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("CallGroupOrTeam")]
        public IHttpActionResult CallGroupOrTeam(string id, string type)
        {
            var users = _userRepository.GetAssignUsers(type, id);

            if (users.Any())
            {
                string[] parts = new string[users.Count + 1];
                parts[0] = UserName;
                for (int i = 1; i <= users.Count; i++)
                {
                    parts[i] = users[i - 1].UserName;
                }

                string token = Guid.NewGuid().ToString();
                var rooms = new List<WebRTCRoom>();
                foreach (var callee in users)
                {
                    var item = new WebRTCRoom
                    {
                        Token = token,
                        Name = "Multi-Call-Room",
                        SharedWith = "public",
                        Status = "available",
                        LastUpdated = DateTime.Now,
                        OwnerName = UserName,
                        OwnerToken = UserId,
                        ParticipantName = callee.UserName,
                        ParticipantToken = callee.Id,
                        Participants = string.Join(",", parts),
                    };

                    rooms.Add(item);
                }

                _roomRepository.AddMany(rooms);

                return Ok(new { Successful = true, roomtoken = token });
            }

            return Ok(new { Successful = false, Message = "Haven't found any user, Please invite user first." });
        }

        /// <summary>
        /// This api will create a Chat Room
        /// </summary>
        /// <param name="id">Chat Room Id</param>
        /// <returns></returns>
        [SwaggerOperation("Room")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet]
        public IHttpActionResult Room(string id)
        {
            var room = _roomRepository.GetSingle(id);
            var rooms = new List<WebRTCRoom>();
            rooms.Add(room);            

            if (room.ParticipantToken == UserId)
            {
                room.Status = "active";
                room.EntityState = EntityState.Modified;

                _roomRepository.Save(room);
            }

            var photo = UserManager.FindById(UserId).Photo;
            var response = new { Rooms = rooms, RoomToken = id, Photo = photo };

            return Ok(response);
        }

        /// <summary>
        /// This api will create Multi Call Room
        /// </summary>
        /// <param name="id">Multi Call Room Id</param>
        /// <returns></returns>
        [SwaggerOperation("MulticallRoom")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet]
        public IHttpActionResult MultiCallRoom(string id)
        {
            var room = _roomRepository.GetSingle(t => t.Token == id && t.ParticipantToken == UserId);

            var rooms = new List<WebRTCRoom>();
            if (room != null)
            {                
                rooms.Add(room);
                room.Status = "active";
                room.EntityState = EntityState.Modified;
                _roomRepository.Save(room);
            }
            else
            {
                room = _roomRepository.GetSingle(x => x.Token == id);
                rooms.Add(room);
            }

            var photo = UserManager.FindById(UserId).Photo;
            var response = new { Rooms = rooms, RoomToken = id, Photo = photo };

            return Ok(response);
        }

        /// <summary>
        /// This api will save Many to Many Call Room
        /// </summary>
        /// <param name="id">Many to Many call Id</param>
        /// <returns></returns>
        [SwaggerOperation("MMCallRoom")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet]
        public IHttpActionResult MMCallRoom(string id)
        {
            var room = _roomRepository.GetSingle(t => t.Token == id && t.ParticipantToken == UserId);

            var rooms = new List<WebRTCRoom>();
            if (room != null)
            {                
                rooms.Add(room);
                room.Status = "active";
                room.EntityState = EntityState.Modified;
                _roomRepository.Save(room);                
            }
            else
            {
                room = _roomRepository.GetSingle(x => x.Token == id);
                rooms.Add(room);
            }

            var photo = UserManager.FindById(UserId).Photo;
            var response = new { Rooms = rooms, RoomToken = id, Photo = photo };

            return Ok(response);
        }

        /// <summary>
        /// This api will Return Participant
        /// </summary>
        /// <param name="roomname">Roomname</param>
        /// <returns></returns>
        [SwaggerOperation("GetParticipant")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("GetParticipant")]
        public IHttpActionResult GetParticipant(string roomname)
        {
            var room = _roomRepository.GetSingle(roomname);
            if (room.Status != "active")
                return Ok(false);
            else
            {
                var userPart = UserManager.FindById(room.ParticipantToken);
                return Ok(new { participant = userPart.UserName, partPhoto = userPart.Photo });
            }
        }

        /// <summary>
        /// This api will save SDP message
        /// </summary>
        /// <param name="sdp">SDP </param>
        /// <param name="roomToken">Roomtoken</param>
        /// <param name="userToken">Usertoken</param>
        /// <returns></returns>
        [SwaggerOperation("PostSDP")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("PostSDP")]
        public IHttpActionResult PostSDP(string sdp, string roomToken, string userToken)
        {
            var sdpmessage = new WebRTCSDPMessage
            {
                SDP = sdp,
                IsProcessed = false,
                RoomToken = roomToken,
                Sender = userToken
            };

            _sdpMessageRepository.Save(sdpmessage);

            return Ok();
        }

        /// <summary>
        /// This api will Return SDP Messages
        /// </summary>
        /// <param name="roomToken">RoomToken</param>
        /// <param name="userToken">UerToken</param>
        /// <returns></returns>
        [SwaggerOperation("GetSDP")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("GetSDP")]
        public IHttpActionResult GetSDP(string roomToken, string userToken)
        {
            var sdpMessage = _sdpMessageRepository.GetSingle(t => t.RoomToken == roomToken && t.Sender != userToken && !t.IsProcessed);

            if (sdpMessage == null)
                return Ok(false);

            sdpMessage.IsProcessed = true;
            sdpMessage.EntityState = EntityState.Modified;
            _sdpMessageRepository.Save(sdpMessage);

            var user = UserManager.FindById(sdpMessage.Sender);

            return Ok(new { sdp = sdpMessage.SDP, partUserName = user.UserName, partPhoto = user.Photo });
        }

        /// <summary>
        /// This api will save Ice
        /// </summary>
        /// <param name="candidate">Candidate</param>
        /// <param name="label">Label</param>
        /// <param name="roomToken">RoomToken</param>
        /// <param name="userToken">UserToken</param>
        /// <returns></returns>
        [SwaggerOperation("PostICE")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("PostICE")]
        public IHttpActionResult PostICE(string candidate, string label, string roomToken, string userToken)
        {
            var cand = new WebRTCCandidatesTable
            {
                Candidate = candidate,
                Label = label,
                RoomToken = roomToken,
                Sender = userToken,
                IsProcessed = false
            };

            _candidateRepository.Save(cand);

            return Ok();
        }

        /// <summary>
        /// This api will Return ICE
        /// </summary>
        /// <param name="roomToken">RoomToken</param>
        /// <param name="userToken">UserToken</param>
        /// <returns></returns>
        [SwaggerOperation("GetICE")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("GetICE")]
        public IHttpActionResult GetICE(string roomToken, string userToken)
        {
            var cand = _candidateRepository.GetSingle(t => t.RoomToken == roomToken && t.Sender != userToken && !t.IsProcessed);
            if (cand == null)
                return Ok(false);

            cand.IsProcessed = true;
            cand.EntityState = EntityState.Modified;

            _candidateRepository.Save(cand);

            return Ok(new { candidate = cand.Candidate, label = cand.Label });
        }

        /// <summary>
        /// This api will Save message
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [SwaggerOperation("SaveMessage")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("SaveMessage")]
        public IHttpActionResult SaveMessage(ChatMessageBindingModel model)
        {
            var message = new ChatMessage
            {
                UserID = UserId,
                RoomToken = model.RoomToken,
                MessageSender = model.MessageSender,
                MessageType = model.MessageType,
                MessageContent = model.MessageContent
            };

            _chatMessageRepsitory.Save(message);

            return Ok();
        }

        /// <summary>
        /// This api will Ignore Call
        /// </summary>
        /// <param name="id">Room Id</param>
        /// <returns></returns>
        [SwaggerOperation("Ignore")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("Ignore")]
        public IHttpActionResult Ignore(string id)
        {
            var room = _roomRepository.GetSingle(id);
            room.Status = "Ignore";
            room.EntityState = EntityState.Modified;

            _roomRepository.Save(room);

            return Ok();
        }

        /// <summary>
        /// This api will save canvas Data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [SwaggerOperation("PostCanvasData")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("PostCanvasData")]
        public IHttpActionResult PostCanvasData(string data)
        {
            string filename = Guid.NewGuid().ToString() + ".txt";
            string filePath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("~/Content/UploatAttachment"), filename);
            System.IO.FileStream fs = System.IO.File.Create(filePath);
            System.IO.StreamWriter sw = new System.IO.StreamWriter(fs);
            sw.Write(data);
            sw.Close();
            fs.Close();

            return Ok(new { FileName = filename });
        }

        /// <summary>
        /// This api will Return Canvas Data
        /// </summary>
        /// <param name="filename">File Name</param>
        /// <returns></returns>
        [HttpGet, ActionName("GetCanvasData")]
        public IHttpActionResult GetCanvasData(string filename)
        {
            string filePath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("~/Content/UploatAttachment"), filename);
            string data = System.IO.File.ReadAllText(filePath);

            System.IO.File.Delete(filePath);

            return Ok(new { Data = data });
        }
    }
}