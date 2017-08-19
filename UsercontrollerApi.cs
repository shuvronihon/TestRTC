using AutoMapper;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using WebRTC.Core;
using WebRTC.Core.Entities;
using WebRTC.Data.Abstracts;
using WebRTC.Infrastructure.Helpers;
using WebRTC.Models;

namespace WebRTC.Controllers
{
    /// <summary>
    /// UserApiController
    /// </summary>
    [Authorize]
    public class UserApiController : BaseApiController
    {
        private readonly IGroupRepository _groupRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly IUserAssignGroupRepository _userGroupRepository;
        private readonly IFileRepository _fileRepossitory;
        private readonly IUserAssignTeamRepository _userTeamRepository;
        private readonly IContactRepository _contactRepository;
        private readonly IMapper _mapper;
        private readonly ICommonHelper _commonHelper;

/// <summary>
/// Constructor
/// </summary>
/// <param name="groupRepository"></param>
/// <param name="teamRepository"></param>
/// <param name="userGroupRepository"></param>
/// <param name="filerepository"></param>
/// <param name="userTeamRepository"></param>
/// <param name="contactRepository"></param>
/// <param name="mapper"></param>
/// <param name="commonHelper"></param>
        public UserApiController(
            IGroupRepository groupRepository,
            ITeamRepository teamRepository,
            IUserAssignGroupRepository userGroupRepository,
            IFileRepository filerepository,
            IUserAssignTeamRepository userTeamRepository,
            IContactRepository contactRepository,
            IMapper mapper,
            ICommonHelper commonHelper)
        {
            _groupRepository = groupRepository;
            _teamRepository = teamRepository;
            _userGroupRepository = userGroupRepository;
            _fileRepossitory = filerepository;
            _userTeamRepository = userTeamRepository;
            _contactRepository = contactRepository;
            _mapper = mapper;
            _commonHelper = commonHelper;
        }

        /// <summary>
        /// This api will Change user password
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [SwaggerOperation("SubmitChangePassword")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost]
        public IHttpActionResult SubmitChangePassword(ChangePasswordBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("<br>", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return BadRequest(errors);
            }

            var user = UserManager.Find(UserName, model.OldPassword);
            if (user == null)
                return BadRequest("Password not correct!");

            var resetPasswordToken = UserManager.GeneratePasswordResetToken(UserId);
            var result = UserManager.ResetPassword(UserId, resetPasswordToken, model.NewPassword);

            if (result.Succeeded)
                return Ok(new { Successful = true, Message = "Your successfully reset your password." });

            return Ok(new { Successful = false, Message = "Your password reset was not successful. Please try again." });
        }

        /// <summary>
        /// This api will Edit profile
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [SwaggerOperation("EditProfile")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("EditProfile")]
        public IHttpActionResult EditProfile(EditProfileViewModel model)
        {       
            var user = UserManager.FindById(UserId);
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.EmailAddress;
            user.AgeRange = model.AgeRange;
            user.Gender = model.Gender;
            user.TimeZone = model.TimeZone;
            user.DisplayName = model.DisplayName;
            user.ShowWhatName = model.ShowWhatName;

            UserManager.Update(user);

            return Ok();
        }

        /// <summary>
        /// This api will save Profile photo
        /// </summary>
        /// <param name="Photo">Profile Photo</param>
        /// <returns></returns>
        [SwaggerOperation("EditProfilePhoto")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("EditProfilePhoto")]
        public IHttpActionResult EditProfilePhoto(string Photo)
        {
            var user = UserManager.FindById(UserId);
            user.Photo = Photo;

            UserManager.Update(user);

            return Ok(true);
        }

        /// <summary>
        /// This api will Return a user profile
        /// </summary>
        /// <returns></returns>
        [SwaggerOperation("Getprofile")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("GetProfile")]
        public IHttpActionResult GetProfile()
        {
            var user = UserManager.FindById(UserId);
            return Ok(user);
        }

        /// <summary>
        /// This api will Add a group
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [SwaggerOperation("AddGroup")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost]
        public IHttpActionResult AddGroup(GroupBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("<br>", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return BadRequest(errors);
            }

            var group = new Group
            {
                Name = model.Name,
                Description = model.Description,
                UserID = UserId,
                IsPublic = false,
                Active = true
            };

            var userAssignedGroup = new UserAssignGroup
            {
                UserID = UserId,
                GroupID = group.Id,
                Role = WebRTCConstants.Admin
            };

            _groupRepository.Save(group);
            _userGroupRepository.Save(userAssignedGroup);

            return Ok();
        }

        /// <summary>
        /// This api will Add a Team
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [SwaggerOperation("AddTeam")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("AddTeam")]
        public IHttpActionResult AddTeam(TeamBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("<br>", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return BadRequest(errors);
            }

            var team = new Team
            {
                Name = model.Name,
                Description = model.Description,
                UserID = UserId,
                GroupID = model.GroupId,
                Active = true
            };

            var userAssignedTeam = new UserAssignTeam
            {
                UserID = UserId,
                TeamID = team.Id,
                GroupID = model.GroupId
            };

            _teamRepository.Save(team);
            _userTeamRepository.Save(userAssignedTeam);

            return Ok(team);
        }

        /// <summary>
        /// This api will Return groups
        /// </summary>
        /// <returns></returns>
        [SwaggerOperation("Getgroups")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("GetGroups")]
        public IHttpActionResult GetGroups()
        {
            var items = _groupRepository.FindBy(x => x.UserID == UserId).ToList();
            var groups = _mapper.Map<List<Group>, List<GroupViewModel>>(items);
            foreach (var group in groups)
            {
                var teams = _teamRepository.FindBy(x => x.GroupID == group.Id).ToList();
                var groupTeams = _mapper.Map<List<Team>, List<TeamViewModel>>(teams);
                group.Teams.AddRange(groupTeams);
            }
            return Ok(groups);
        }

        /// <summary>
        /// This api will Return Assign To group
        /// </summary>
        /// <returns></returns>
        [SwaggerOperation("GetAssignToGroup ")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("GetAssignToGroup")]
        public IHttpActionResult GetAssignToGroup()
        {
            var groups = _userGroupRepository.GetUserAssignedGroups(UserId);
            return Ok(groups);
        }

        // api/UserApi/UploadPhoto
        //[HttpPost, ActionName("UploadPhoto")]
        //public IHttpActionResult UploadPhoto()
        //{
        //    HttpPostedFileBase file = Request.Files["file"];
        //    if (file != null)
        //    {
        //        string filename = Guid.NewGuid().ToString() + Path.GetFileName(file.FileName);
        //        string filepath = Path.Combine(HttpContext.Current.Server.MapPath("~/Content/ProfilePhoto"), filename);
        //        file.SaveAs(filepath);
        //        return Ok(new { Successful = true, FileName = filename });
        //    }
        //    else
        //    {
        //        return Ok(new { Successful = false });
        //    }
        //}


        // api/UserApi/UploadAttachment
        //[HttpPost, ActionName("UploadAttachment")]
        //public IHttpActionResult UploadAttachment()
        //{
        //    HttpPostedFileBase file = Request.Files["file"];
        //    if (file != null)
        //    {
        //        string guid = Guid.NewGuid().ToString();
        //        string filename = guid + Path.GetFileName(file.FileName);
        //        filename = filename.Replace(" ", "");
        //        string filepath = Path.Combine(HttpContext.Current.Server.MapPath("~/Content/UploatAttachment"), filename);
        //        file.SaveAs(filepath);

        //        string filetype = "Other";
        //        string lowerFileName = filename.ToLower();
        //        if (lowerFileName.EndsWith(".jpg") || lowerFileName.EndsWith(".png"))
        //            filetype = "Image";
        //        else if (lowerFileName.EndsWith(".pdf"))
        //            filetype = "PDF";

        //        List<string> ImagesList = null;

        //        if (filetype == "PDF")
        //        {
        //            Directory.CreateDirectory(HttpContext.Current.Server.MapPath("~/Content/UploatAttachment/" + guid));
        //            _commonHelper.PDFToImage("D:\\Program Files\\gs\\gs9.19\\bin\\gswin64.exe",
        //                HttpContext.Current.Server.MapPath("~/Content/UploatAttachment") + "/" + guid + "/image_%d.jpg", filepath);

        //            string imagefile = _commonHelper.MergeMultiImage(HttpContext.Current.Server.MapPath("~/Content/UploatAttachment/" + guid), Color.White);

        //            filename = guid + "/" + imagefile;
        //            filetype = "Image";

        //            DirectoryInfo dirInfo = new DirectoryInfo(HttpContext.Current.Server.MapPath("~/Content/UploatAttachment/" + guid));
        //            FileInfo[] files = dirInfo.GetFiles();
        //            ImagesList = new List<string>();
        //            foreach (var f in files)
        //            {
        //                if (f.Name.StartsWith("image_") == true)
        //                    ImagesList.Add(guid + "/" + f.Name);
        //            }
        //        }

        //        int width = 0, height = 0;
        //        if (filetype == "Image")
        //        {
        //            Image image = Image.FromFile(filepath);
        //            width = image.Width;
        //            height = image.Height;
        //            image.Dispose();
        //        }

        //        return Ok(new
        //        {
        //            Successful = true,
        //            FileName = filename,
        //            OriginalFileName = Path.GetFileName(file.FileName),
        //            FileType = filetype,
        //            Width = width,
        //            Height = height,
        //            ImagesList = ImagesList,
        //        });
        //    }

        //    return Ok(new { Successful = false });
        //}

        /// <summary>
        /// This api will save Ghost PDF
        /// </summary>
        /// <param name="filename">Name of File</param>
        /// <param name="savefilename">File name to Saved</param>
        /// <returns></returns>
        [SwaggerOperation("GhostPDF")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("GhostPDF")]
        public IHttpActionResult GhostPDF(string filename, string savefilename)
        {
            string guid = savefilename.Substring(0, 36);
            string path = HttpContext.Current.Server.MapPath("~/Content/UploatAttachment/" + guid);
            string filepath = Path.Combine(HttpContext.Current.Server.MapPath("~/Content/UploatAttachment"), savefilename);

            DirectoryInfo dirInfo = new DirectoryInfo(path);
            if (dirInfo.Exists == false)
            {
                Directory.CreateDirectory(path);
                _commonHelper.PDFToImage("D:\\Program Files\\gs\\gs9.19\\bin\\gswin64.exe",
                    path + "/image_%d.jpg", filepath);

                string imagefile = _commonHelper.MergeMultiImage(HttpContext.Current.Server.MapPath("~/Content/UploatAttachment/" + guid), Color.White);
                filename = guid + "/" + imagefile;
            }

            FileInfo[] files = dirInfo.GetFiles();
            List<string> ImagesList = new List<string>();
            foreach (var f in files)
            {
                if (f.Name.StartsWith("image_") == true)
                    ImagesList.Add(guid + "/" + f.Name);
            }

            int width = 0, height = 0;
            Image image = Image.FromFile(Path.Combine(HttpContext.Current.Server.MapPath("~/Content/UploatAttachment"), ImagesList[0]));
            width = image.Width;
            height = image.Height;
            image.Dispose();

            return Ok(new
            {
                Successful = true,
                Width = width,
                Height = height,
                ImagesList = ImagesList,
            });
        }

        // api/UserApi/SaveCanvasArea
        //[HttpPost, ActionName("SaveCanvasArea")]
        //public IHttpActionResult SaveCanvasArea()
        //{
        //    string strBase64 = Request["strBase64"];
        //    string openImage = Request["openImage"];
        //    string imagename = Request["imagename"];
        //    string type = Request["type"];

        //    string endfilename = "";

        //    string[] strBase64List = strBase64.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
        //    string[] openImageList = openImage.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

        //    //There are three conditions
        //    if (openImageList.Length == 0)
        //    {
        //        string filename0 = _commonHelper.Base64toImage(strBase64List[0], HttpContext.Current.Server.MapPath("~/Content/UploatAttachment"), 0);
        //        endfilename = filename0;
        //    }
        //    else if (openImageList.Length == 1)
        //    {
        //        byte[] arr = Convert.FromBase64String(strBase64);
        //        string filename = Guid.NewGuid().ToString() + ".png";
        //        string filepath = Path.Combine(HttpContext.Current.Server.MapPath("~/Content/UploatAttachment"), filename);

        //        MemoryStream ms = new MemoryStream(arr);
        //        Bitmap bmp = new Bitmap(ms);
        //        bmp.Save(filepath, System.Drawing.Imaging.ImageFormat.Png);

        //        ms.Close();

        //        string newfile = _commonHelper.MergeTwoImage(
        //                filepath,
        //                HttpContext.Current.Server.MapPath("~/Content/UploatAttachment/" + Request["openImage"]),
        //                 HttpContext.Current.Server.MapPath("~/Content/UploatAttachment")
        //            );

        //        endfilename = newfile;
        //    }
        //    else
        //    {
        //        string guid = Guid.NewGuid().ToString();
        //        string path = HttpContext.Current.Server.MapPath("~/Content/UploatAttachment/" + guid);
        //        Directory.CreateDirectory(path);

        //        int index = 0;
        //        foreach (var base64 in strBase64List)
        //        {
        //            _commonHelper.Base64toImage(base64, path, index);
        //            index++;
        //        }

        //        path = path + "\\" + _commonHelper.MergeMultiImage(path, Color.Transparent);

        //        string path2 = HttpContext.Current.Server.MapPath("~/Content/UploatAttachment/" + openImageList[0].Substring(0, 36));
        //        FileInfo[] files = new DirectoryInfo(path2).GetFiles();
        //        string oriFile = "";
        //        foreach (var file in files)
        //        {
        //            if (file.Name.StartsWith("image_") == false)
        //            {
        //                oriFile = file.FullName;
        //                break;
        //            }
        //        }

        //        string newfile2 = _commonHelper.MergeTwoImage(path, oriFile, HttpContext.Current.Current.Server.MapPath("~/Content/UploatAttachment"));

        //        endfilename = newfile2;
        //    }

        //    if (type == "FileLibrary")
        //    {
        //        string filepath = Path.Combine(HttpContext.Current.Server.MapPath("~/Content/UploatAttachment"), endfilename);
        //        FileInfo fileInfo = new FileInfo(filepath);
        //        string fileSize = _commonHelper.FormatBytes(fileInfo.Length);
        //        Image img = Image.FromFile(filepath);

        //        var file = new Core.Entities.File
        //        {
        //            UserID = UserId,
        //            FileName = string.IsNullOrEmpty(imagename) ? DateTime.Now.ToString("yyyyMMddHHmmssffff") : imagename,
        //            SaveFileName = endfilename,
        //            FolderID = "",
        //            FileType = "Image",
        //            FileSize = fileSize,
        //            Width = img.Width,
        //            Height = img.Height,
        //        };
        //        _fileRepossitory.Save(file);

        //        img.Dispose();
        //    }

        //    return Ok(endfilename.Replace(".jpg", "").Replace(".png", ""));
        //}

        /// <summary>
        /// This api will Return Group Members
        /// </summary>
        /// <param name="groupid">Group members</param>
        /// <returns></returns>
        [SwaggerOperation("GetGroupMembers")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("GetGroupMembers")]
        public IHttpActionResult GetGroupMembers(string groupid)
        {
            var groupMembers = _userGroupRepository.GetGroupMembers(groupid);
            return Ok(groupMembers);
        }

        /// <summary>
        /// This api will return Team Members
        /// </summary>
        /// <param name="teamid">Team members</param>
        /// <returns></returns>
        [SwaggerOperation("GetTeamMembers")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("GetTeamMembers")]
        public IHttpActionResult GetTeamMembers(string teamid)
        {
            var teamMembers = _userTeamRepository.GetTeamMembers(teamid);
            return Ok(teamMembers);
        }

        /// <summary>
        /// This api will Add a user to group
        /// </summary>
        /// <param name="type">Group type</param>
        /// <param name="id">Group Id</param>
        /// <param name="uids">User Ids</param>
        /// <returns></returns>
        [SwaggerOperation("sumitAddUserToGroup")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("submitAddUserToGroup")]
        public IHttpActionResult submitAddUserToGroup(string type, string id, string uids)
        {
            var contactIds = _contactRepository.FindBy(x => x.UserID == UserId).Select(x => x.ContactUserID).ToArray();
            var uidArray = uids.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            if (type == "group")
            {
                _userGroupRepository.DeleteWhere(x => x.GroupID == id && contactIds.Contains(x.UserID));

                var userGroups = new List<UserAssignGroup>();
                foreach (var uid in uidArray)
                {
                    var item = new UserAssignGroup
                    {
                        GroupID = id,
                        UserID = uid,
                        Role = ""
                    };

                    userGroups.Add(item);
                }

                _userGroupRepository.AddMany(userGroups);
            }

            if (type == "team")
            {
                _userTeamRepository.DeleteWhere(x => x.TeamID == id && contactIds.Contains(x.UserID));

                var team = _teamRepository.GetSingle(id);

                var userTeams = new List<UserAssignTeam>();
                foreach (var uid in uidArray)
                {
                    var item = new UserAssignTeam
                    {
                        TeamID = team.Id,
                        GroupID = team.GroupID,
                        UserID = uid
                    };

                    userTeams.Add(item);
                }

                _userTeamRepository.AddMany(userTeams);
            }

            return Ok(new { Successful = true });
        }

        /// <summary>
        /// This api will Activate or Deactivate group
        /// </summary>
        /// <param name="id">Group Id</param>
        /// <param name="active">Bool "True or false" </param>
        /// <returns></returns>
        [SwaggerOperation("ActiveGroup")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("ActiveGroup")]
        public IHttpActionResult ActiveGroup(string id, bool active)
        {
            var group = _groupRepository.GetSingle(id);
            group.Active = active;
            group.EntityState = EntityState.Modified;

            _groupRepository.Save(group);

            return Ok(true);
        }

        /// <summary>
        /// This api will Activate or Deactivate Team
        /// </summary>
        /// <param name="id">Team Id</param>
        /// <param name="active">Boll "true or false"</param>
        /// <returns></returns>
        [SwaggerOperation("ActiveTeam")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("ActiveTeam")]
        public IHttpActionResult ActiveTeam(string id, bool active)
        {
            var team = _teamRepository.GetSingle(id);
            team.Active = active;
            team.EntityState = EntityState.Modified;

            _teamRepository.Save(team);

            return Ok();
        }

        /// <summary>
        /// This api will Update a team
        /// </summary>
        /// <param name="Id">Team id</param>
        /// <param name="Name">Team name</param>
        /// <param name="Description">Team</param>
        /// <param name="Active">Boll "true or false"</param>
        /// <returns></returns>
        [SwaggerOperation("EditTeam")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("EditTeam")]
        public IHttpActionResult EditTeam(string Id, string Name, string Description, bool Active)
        {
            var team = _teamRepository.GetSingle(Id);

            if (team == null)
                return BadRequest("Team not found");

            team.Name = Name;
            team.Description = Description;
            team.Active = Active;
            team.EntityState = EntityState.Modified;

            _teamRepository.Save(team);

            return Ok();
        }

       /// <summary>
       /// This api will Update a Group
       /// </summary>
       /// <param name="Id">Group Id</param>
       /// <param name="Name">Group Name</param>
       /// <param name="Description">Group</param>
       /// <returns></returns>
        [SwaggerOperation("EditGroup")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("EditGroup")]
        public IHttpActionResult EditGroup(string Id, string Name, string Description)
        {
            var group = _groupRepository.GetSingle(Id);

            if (group == null)
                return BadRequest("Group not found.");

            group.Name = Name;
            group.Description = Description;
            group.EntityState = EntityState.Modified;

            _groupRepository.Save(group);

            return Ok();
        }

        /// <summary>
        /// This api will Delete a group
        /// </summary>
        /// <param name="groupid">Group Id</param>
        /// <returns></returns>
        [SwaggerOperation("DeleteGroup")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("DeleteGroup")]
        public IHttpActionResult DeleteGroup(string groupid)
        {
            _groupRepository.DeleteGroup(groupid);
            return Ok();
        }

        /// <summary>
        /// This api will Delete a team
        /// </summary>
        /// <param name="teamid">Team Id</param>
        /// <returns></returns>
        [SwaggerOperation("DeleteTeam")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("DeleteTeam")]
        public IHttpActionResult DeleteTeam(string teamid)
        {
            _teamRepository.DeleteTeam(teamid);
            return Ok();
        }

        // api/UserApi/UpdateUserOnLineStatus
        [HttpPost, ActionName("UpdateUserOnLineStatus")]
        public IHttpActionResult UpdateUserOnLineStatus(string status)
        {
            var user = UserManager.FindById(UserId);
            user.UserOnLineStatus = status;

            UserManager.Update(user);

            return Ok();
        }

        /// <summary>
        /// This api Download Image
        /// </summary>
        /// <param name="filename">File Name</param>
        /// <returns></returns>
        [AllowAnonymous]
        [SwaggerOperation("DownloadImage")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet]
        public IHttpActionResult DownloadImage(string filename)
        {
            var filePath = "~/Content/UploatAttachment";
            var info = new DownloadFileInfo(filePath);

            return new FileActionResult(info);
        }
    }
}