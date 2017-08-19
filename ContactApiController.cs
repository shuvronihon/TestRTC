using AutoMapper;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using WebRTC.Core.Entities;
using WebRTC.Data.Abstracts;
using WebRTC.Data.Dtos;
using WebRTC.Infrastructure.Helpers;
using WebRTC.Models;

namespace WebRTC.Controllers
{
     /// <summary>
    /// ContactsApiController
    /// </summary>
    [Authorize]
    public class ContactsApiController : BaseApiController
    {
        private readonly IContactRepository _contactRepository;
        private readonly IContactMessageRepository _contactMessageRepository;
        private readonly IPushNotificationHelper _pushNotificationHelper;
        private readonly IMapper _mapper; 
    
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="contactRepository"></param>
        /// <param name="contactMessageRepository"></param>
        /// <param name="pushNotificationHelper"></param>
        /// <param name="mapper"></param>
        public ContactsApiController(
            IContactRepository contactRepository,
            IContactMessageRepository contactMessageRepository,
            IPushNotificationHelper pushNotificationHelper,
            IMapper mapper)
        {
            _contactRepository = contactRepository;
            _contactMessageRepository = contactMessageRepository;
            _pushNotificationHelper = pushNotificationHelper;
            _mapper = mapper;
        }

        /// <summary>
        /// This pi will Return Contact
        /// </summary>
        /// <returns></returns>
        [SwaggerOperation("GetContacts")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("GetContacts")]
        public IHttpActionResult GetContacts()
        {
            var contacts = new List<ContactDTO>();
            //If user is superadmin, show all of users
            if (User.IsInRole("SuperAdmin"))
            {
                contacts = _contactRepository.GetSuperAdminContacts();
                return Ok(contacts);
            }

            contacts = _contactRepository.GetUserContacts(UserId);

            var messages = _contactMessageRepository.FindBy(x => x.UserID == UserId && x.MessageStatus == "UnRead").ToList();

            foreach (var contact in contacts)
            {
                contact.UnReadMessageCount = messages.FindAll(t => t.ContactUserID == contact.ContactUserID).Count;

                if (contact.UserOnLineStatus == "Online")
                {
                    if (contact.LastUpdateTime == null)
                        contact.UserOnLineStatus = "Offline";
                    else
                    {
                        TimeSpan ts = DateTime.Now - contact.LastUpdateTime.Value;
                        if (ts.TotalMinutes > 30)
                            contact.UserOnLineStatus = "Offline";
                        else
                            contact.UserOnLineStatus = "Online";
                    }
                }
            }

            return Ok(contacts);
        }

        /// <summary>
        /// This api will Delete Contact
        /// </summary>
        /// <param name="contactUserID">Contact User ID</param>
        /// <returns></returns>
        [SwaggerOperation("DeleteContact")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("DeleteContact")]
        public IHttpActionResult DeleteContact(string contactUserID)
        {
            _contactRepository.DeleteWhere(x => (x.UserID == UserId && x.ContactUserID == contactUserID) || (x.ContactUserID == UserId && x.UserID == contactUserID));
            return Ok();
        }

        /// <summary>
        /// This api will Return Contact message
        /// </summary>
        /// <param name="contactUserID">ContactUserID</param>
        /// <param name="tryCount">Value can be 1 or an Integer value</param>
        /// <returns></returns>
        [SwaggerOperation("GetContactMessage")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("GetContactMessage")]
        public IHttpActionResult GetContactMessage(string contactUserID, int tryCount)
        {
            var messages = new List<ContactMessage>();

            if (tryCount == 1)
            {
                messages =
                    _contactMessageRepository.FindBy(x => x.UserID == UserId && x.ContactUserID == contactUserID)
                    .OrderBy(x => x.CreateOn)
                    .ToList();
            }
            else
            {
                messages =
                    _contactMessageRepository.FindBy(x => x.UserID == UserId && x.ContactUserID == contactUserID && x.MessageStatus == "UnRead")
                    .OrderBy(x => x.CreateOn)
                    .ToList();
            }

            var messagesToUpdate = new List<ContactMessage>();
            var messageViewModels = new List<ContactMessageViewModel>();
            foreach (var item in messages)
            {
                if (item.MessageStatus == "UnRead")
                {
                    item.MessageStatus = "Read";
                    messagesToUpdate.Add(item);

                    if (!string.IsNullOrEmpty(item.Token))
                    {
                        var message = _contactMessageRepository.GetSingle(x => x.Token == item.Token && x.UserID == item.ContactUserID);
                        if (message != null)
                        {
                            message.HasBeenReadStatus = "Read";
                            messagesToUpdate.Add(message);
                        }
                    }
                }

                var messageViewModel = _mapper.Map<ContactMessage, ContactMessageViewModel>(item);
                if ((item.MessageType == "File" || item.MessageType == "Audio") && item.MessageContent.Contains(","))
                {
                    string[] sss = item.MessageContent.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    messageViewModel.MessageContent = sss[0];
                    messageViewModel.Key1 = sss[1];
                }

                messageViewModels.Add(messageViewModel);
            }

            _contactMessageRepository.UpdateMany(messagesToUpdate);

            return Ok(messageViewModels);
        }

        /// <summary>
        /// This api will Send message
        /// </summary>
        /// <param name="ContactUserID">Contact User Id</param>
        /// <param name="MessageType">Messge type can be Audio,Text and File </param>
        /// <param name="MessageContent"></param>
        /// <returns></returns>
        [SwaggerOperation("SendMessage")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("SendMessage")]
        public IHttpActionResult SendMessage(string ContactUserID, string MessageType, string MessageContent)
        {
            string token = Guid.NewGuid().ToString();

            if (MessageType == "Text")
            {
                for (var i = 1; i <= 30; i++)
                {
                    string ii = i >= 10 ? i.ToString() : "0" + i;
                    MessageContent = MessageContent.Replace("/e" + ii, "<img src='/Content/emoticons/e" + ii + ".png' style=''/>");
                }
            }

            var messageToSend = new ContactMessage
            {
                UserID = UserId,
                ContactUserID = ContactUserID,
                Role = "Sender",
                MessageType = MessageType,
                MessageContent = MessageContent,
                MessageStatus = "Read",
                Token = token,
                HasBeenReadStatus = "UnRead",
            };

            var messageToReceive = new ContactMessage
            {
                UserID = ContactUserID,
                ContactUserID = UserId,
                Role = "Receiver",
                MessageType = MessageType,
                MessageContent = MessageContent,
                MessageStatus = "UnRead",
                Token = token,
                HasBeenReadStatus = "Read",
            };

            var messages = new List<ContactMessage>();
            messages.Add(messageToSend);
            messages.Add(messageToReceive);
            _contactMessageRepository.AddMany(messages);

            var sender = UserManager.FindById(UserId);
            var receiver = UserManager.FindById(ContactUserID);

            List<PushNotificationIds> deviceTokens = new List<PushNotificationIds>();
            var notificationIdModel = new PushNotificationIds
            {
                DeviceToken = receiver.DeviceToken,
                DeviceType = receiver.DeviceType
            };
            deviceTokens.Add(notificationIdModel);

            var messageToSendViewModel = _mapper.Map<ContactMessage, ContactMessageViewModel>(messageToSend);
            if (MessageType == "File")
            {
                string[] sss = MessageContent.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                messageToSendViewModel.MessageContent = sss[0];
                messageToSendViewModel.Key1 = sss[1];

                var data = new
                {
                    Title = "Tucana",
                    Id = sender.Id,
                    GTCType = "Contact",
                    GTCName = sender.UserName,
                    Msg = sender.UserName + " : Send you file"
                };
                _pushNotificationHelper.SendNotification(data, deviceTokens);
            }
            else
            {
                var data = new
                {
                    Title = "Tucana",
                    Id = sender.Id,
                    GTCType = "Contact",
                    GTCName = sender.UserName,
                    Msg = sender.UserName + " : " + MessageContent
                };
                _pushNotificationHelper.SendNotification(data, deviceTokens);
            }

            return Ok(messageToSendViewModel);
        }

        /// <summary>
        /// This api will Return Unread Messages
        /// </summary>
        /// <returns></returns>
        [SwaggerOperation("GetUnReadMessage")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("GetUnReadMessages")]
        public IHttpActionResult GetUnReadMessages()
        {
            var totalUnreadMessage =
                _contactMessageRepository.FindBy(x => x.UserID == UserId && x.MessageStatus == "UnRead")
                .Count();
            return Ok(new { Count = totalUnreadMessage });
        }
    }
}