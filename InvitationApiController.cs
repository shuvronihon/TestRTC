using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using WebRTC.Core.Entities;
using WebRTC.Data.Abstracts;

namespace WebRTC.Controllers
{
    /// <summary>
    /// GroInvitationApiController
    /// </summary>
    [Authorize]
    public class InvitationApiController : BaseApiController
    {
        private readonly IInvitationRepository _invitationRepository;
        private readonly IContactRepository _contactReposotory;
        private readonly IUserAssignGroupRepository _userGroupRepository;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="invitationRepository"></param>
        /// <param name="contactReposotory"></param>
        /// <param name="userGroupRepository"></param>
        public InvitationApiController(
            IInvitationRepository invitationRepository,
            IContactRepository contactReposotory,
            IUserAssignGroupRepository userGroupRepository)
        {
            _invitationRepository = invitationRepository;
            _contactReposotory = contactReposotory;
            _userGroupRepository = userGroupRepository;
        }

        /// <summary>
        /// This api will Return Records 
        /// </summary>
        /// <returns>Records</returns>
        [SwaggerOperation("GetSent")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("GetSent")]
        public IHttpActionResult GetSent()
        {
            var records = _invitationRepository.GetSentInvitations(UserId);
            return Ok(records);
        }

        /// <summary>
        /// This api will Return received records
        /// </summary>
        /// <returns></returns>
        [SwaggerOperation("GetReceived")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("GetReceived")]
        public IHttpActionResult GetReceived()
        {
            var records = _invitationRepository.GetReceivedInvitations(UserId);
            return Ok(records);
        }

        /// <summary>
        /// This api will Return Unread received messages
        /// </summary>
        /// <returns></returns>
        [SwaggerOperation("GetReceivedUnread")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("GetReceivedUnRead")]
        public IHttpActionResult GetReceivedUnRead()
        {
            var invitations = _invitationRepository.FindBy(t => t.InviteUserID == UserId && t.Status == "sent");
            return Ok(new { Count = invitations.Count() });
        }

        /// <summary>
        /// this api will save either refuse or accept Invitation
        /// </summary>
        /// <param name="invite_id">User Id</param>
        /// <param name="type">Group Type</param>
        /// <returns></returns>
        [HttpPost, ActionName("Options")]
        public IHttpActionResult Options(string invite_id, string type)
        {
            var invitation = _invitationRepository.GetSingle(invite_id);

            if (type == "refuse")
            {
                invitation.Status = "refuse";
            }

            if (type == "accept")
            {
                var contacts = new List<Contact>();

                var sender = new Contact
                {
                    UserID = invitation.UserID,
                    ContactUserID = UserId,
                    AssignGroupID = ""
                };

                var accepter = new Contact
                {
                    UserID = UserId,
                    ContactUserID = invitation.UserID,
                    AssignGroupID = ""
                };

                contacts.Add(sender);
                contacts.Add(accepter);
                _contactReposotory.AddMany(contacts);

                //If Group is not null
                if (!string.IsNullOrEmpty(invitation.AssignGroupID))
                {
                    var userAssignGroup = new UserAssignGroup
                    {
                        UserID = invitation.UserID,
                        GroupID = invitation.AssignGroupID,
                    };
                    _userGroupRepository.Save(userAssignGroup);
                }

                invitation.Status = "accept";
            }

            invitation.EntityState = System.Data.Entity.EntityState.Modified;
            _invitationRepository.Save(invitation);

            return Ok();
        }
    }
}