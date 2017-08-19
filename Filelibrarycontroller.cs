using System;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Web.Http;
using WebRTC.Data.Abstracts;
using WebRTC.Infrastructure.Helpers;
using AutoMapper;
using WebRTC.Core.Entities;
using System.Web;
using System.Linq;
using System.Collections.Generic;
using WebRTC.Core;
using System.Threading.Tasks;
using System.Net.Http;
using WebRTC.Models;
using Swashbuckle.Swagger.Annotations;

namespace WebRTC.Controllers
{
    /// <summary>
    /// FileLibraryApiController
    /// </summary>
    [Authorize]
    public class FileLibraryApiController : BaseApiController
    {
        private readonly IFileRepository _fileRepository;
        private readonly IGTFileRepository _gtFileRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly IGTFolderRepository _gtFolderRepository;
        private readonly ICommonHelper _commonHelper;
        private readonly IMapper _mapper;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileRepository"></param>
        /// <param name="gtFileRepository"></param>
        /// <param name="groupRepository"></param>
        /// <param name="teamRepository"></param>
        /// <param name="folderRepository"></param>
        /// <param name="gtFolderRepository"></param>
        /// <param name="commonHelper"></param>
        /// <param name="mapper"></param>
        public FileLibraryApiController(
            IFileRepository fileRepository,
            IGTFileRepository gtFileRepository,
            IGroupRepository groupRepository,
            ITeamRepository teamRepository,
            IFolderRepository folderRepository,
            IGTFolderRepository gtFolderRepository,
            ICommonHelper commonHelper,
            IMapper mapper)
        {
            _fileRepository = fileRepository;
            _gtFileRepository = gtFileRepository;
            _groupRepository = groupRepository;
            _teamRepository = teamRepository;
            _folderRepository = folderRepository;
            _gtFolderRepository = gtFolderRepository;
            _commonHelper = commonHelper;
            _mapper = mapper;
        }

        /// <summary>
        /// Save new folder
        /// </summary>
        /// <param name="Name">Folder Name</param>
        /// <param name="ParentID">Parent Folder Id</param>
        /// <param name="Id">Group or Team Id</param>
        /// <param name="GTType">Group or Team Type</param>
        /// <returns></returns>
        [HttpPost, ActionName("NewFolder")]
        public IHttpActionResult NewFolder(string Name, string ParentID, string Id, string GTType)
        {
            if (GTType == "File")
            {
                var folder = new Folder
                {
                    UserID = UserId,
                    Name = Name,
                    ParentID = ParentID
                };

                _folderRepository.Save(folder);
            }
            else
            {
                var folder = new GTFolder
                {
                    UserID = UserId,
                    UserName = UserName,
                    GTID = Id,
                    GTType = GTType,
                    Name = Name,
                    ParentID = ParentID
                };

                _gtFolderRepository.Save(folder);
            }

            return Ok();
        }

        /// <summary>
        /// this api will return a Folder
        /// </summary>
        /// <param name="folderId"> Folder Id</param>
        /// <param name="gtid">Group or Team  Id</param>
        /// <param name="GTType">Group or Team Type</param>
        /// <returns></returns>
        [HttpGet, ActionName("Get")]
        public IHttpActionResult Get(string folderId, string gtid, string GTType)
        {
            if (WebRTCConstants.GTTypeFile.Equals(GTType, StringComparison.OrdinalIgnoreCase))
            {
                var folders = _folderRepository.FindBy(x => x.UserID == UserId && x.ParentID == folderId).ToList();
                var files = _fileRepository.FindBy(x => x.UserID == UserId && x.FolderID == folderId).ToList();

                var navFolders = new List<Folder>();
                for (int i = 0; i < 10; i++)
                {
                    if (string.IsNullOrEmpty(folderId))
                    {
                        navFolders.Add(new Folder { Name = "All Files", Id = "", Discriminator = "" });
                        break;
                    }
                    else
                    {
                        var folder = _folderRepository.GetSingle(folderId);
                        navFolders.Add(folder);
                        folderId = folder.ParentID;
                    }
                }
                navFolders[0].Discriminator = "LastOne";

                return Ok(new { Folders = folders, Files = files, NavFolders = navFolders });
            }
            else
            {
                var folders = _gtFolderRepository.FindBy(x => x.ParentID == folderId && x.GTID == gtid).ToList();
                var files = _gtFileRepository.FindBy(x => x.GTID == gtid && x.GTType == GTType && x.FolderID == folderId).ToList();

                var navFolders = new List<GTFolder>();
                for (int i = 0; i < 10; i++)
                {
                    if (string.IsNullOrEmpty(folderId))
                    {
                        navFolders.Add(new GTFolder { Name = "All Files", Id = "", Discriminator = "" });
                        break;
                    }
                    else
                    {
                        var folder = _gtFolderRepository.GetSingle(folderId);
                        navFolders.Add(folder);

                        folderId = folder.ParentID;
                    }
                }
                navFolders[0].Discriminator = "LastOne";

                return Ok(new { Folders = folders, Files = files, NavFolders = navFolders });
            }
        }

        /// <summary>
        /// Get GTFile
        /// </summary>
        /// <param name="Id">Group Team Id</param>
        /// <param name="GTType">Group or Team Type</param>
        /// <returns></returns>
        [SwaggerOperation("GetGT")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpGet, ActionName("GetGT")]
        public IHttpActionResult GetGT(string Id, string GTType)
        {
            var files = _gtFileRepository.FindBy(x => x.GTID == Id && x.GTType == GTType).ToList();
            return Ok(new { Files = files });
        }

        /// <summary>
        /// Upload camera file
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [SwaggerOperation("UploadCameraFile")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("UploadCameraFile")]
        public IHttpActionResult UploadCameraFile(UploadCameraFileBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("<br>", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return BadRequest(errors);
            }

            Random generator = new Random();
            string filename = generator.Next(0, 1000000).ToString("D6") + ".png";
            string finalName = Guid.NewGuid().ToString() + filename;
            string path = HttpContext.Current.Server.MapPath("~/Content/UploatAttachment/") + "\\" + finalName;
            
            _commonHelper.Base64toImage(model.Base64, path);
            
            return Ok(new { Successful = true, FileName = filename, SaveFileName = finalName });
        }

        /// <summary>
        /// This api will save Image Size
        /// </summary>
        /// <param name="fileid">File Id</param>
        /// <returns></returns>
        [SwaggerOperation("SetImageSize")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("SetImageSize")]
        public IHttpActionResult SetImageSize(string fileid)
        {
            var file = _fileRepository.GetSingle(fileid);
            string filepath = Path.Combine(HttpContext.Current.Server.MapPath("~/Content/UploatAttachment"), file.SaveFileName);
            Image img = Image.FromFile(filepath);

            file.Width = img.Width;
            file.Height = img.Height;
            file.EntityState = EntityState.Modified;
            _fileRepository.Save(file);

            return Ok(file);
        }

        /// <summary>
        /// This api will upload File
        /// </summary>
        /// <returns></returns>
        [SwaggerOperation("UploadFile")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost]
        public async Task<IHttpActionResult> UploadFile()
        {
            if (!Request.Content.IsMimeMultipartContent())
                return BadRequest("Unsupported media type.");

            var uploadPath = HttpContext.Current.Server.MapPath("~/Content/UploatAttachment");
            var provider = new UploadMultipartFormProvider(uploadPath);

            var streamcontent = _commonHelper.GetNormalizedStreamContent(Request);
            // Read the form data and return an async task.
            await streamcontent.ReadAsMultipartAsync(provider);

            var contentLength = streamcontent.Headers.ContentLength;
            var fileData = provider.FileData.First();

            var fileName = fileData.Headers.ContentDisposition.FileName?.Replace("\"", "");
            var lowerFileName = fileName.ToLower();
            var mimeType = MimeMapping.GetMimeMapping(lowerFileName);

            if (mimeType.Contains("image") && contentLength >= 250 * 1024)
            {
                string filepath = Path.Combine(HttpContext.Current.Server.MapPath("~/Content/UploatAttachment"), fileName);
                string saveFilename = _commonHelper.ResizeImage(filepath, HttpContext.Current.Server.MapPath("~/Content/UploatAttachment"), fileName, 500);
                return Ok(new { Successful = true, successful = true, FileName = fileName, SaveFileName = saveFilename, file_path = "/Content/UploatAttachment/" + saveFilename });
            }

            if(mimeType.Contains("pdf") && contentLength >= 4 * 1024 * 1024)
                return Ok(new { Successful = false, Message = "Failed, PDF is big than 4MB." });

            return Ok(new { Successful = true, successful = true, FileName = fileName, SaveFileName = fileName, file_path = "/Content/UploatAttachment/" + fileName });
        }

        /// <summary>
        /// This api will save uploaded file
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [SwaggerOperation("SaveUploadFile")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost]
        public IHttpActionResult SaveUploadFile(SaveUploadFileBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("<br>", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return BadRequest(errors);
            }

            string filetype = "Other";
            string lowerFileName = model.FileName.ToLower();
            if (lowerFileName.EndsWith(".jpg") || lowerFileName.EndsWith(".png"))
                filetype = "Image";
            else if (lowerFileName.EndsWith(".pdf"))
                filetype = "PDF";

            string filepath = Path.Combine(HttpContext.Current.Server.MapPath("~/Content/UploatAttachment"), model.SaveFileName);
            FileInfo fileInfo = new FileInfo(filepath);
            string fileSize = _commonHelper.FormatBytes(fileInfo.Length);

            int width = 0, height = 0;
            if (filetype == "Image")
            {
                Image img = Image.FromFile(filepath);
                width = img.Width;
                height = img.Height;
            }

            if (WebRTCConstants.GTTypeFile.Equals(model.GTType, StringComparison.OrdinalIgnoreCase))
            {
                var file = new Core.Entities.File
                {
                    UserID = UserId,
                    FileName = model.FileName,
                    SaveFileName = model.SaveFileName,
                    FolderID = model.FolderID,
                    FileType = filetype,
                    FileSize = fileSize,
                    Width = width,
                    Height = height,
                };

                _fileRepository.Save(file);

                return Ok(file);
            }
            else
            {
                var file = new GTFile
                {
                    UserID = UserId,
                    UserName = UserName,
                    GTID = model.GTId,
                    GTType = model.GTType,
                    FolderID = model.FolderID,
                    FileName = model.FileName,
                    SaveFileName = model.SaveFileName,
                    FileType = filetype,
                    FileSize = fileSize,
                    Width = width,
                    Height = height,
                };

                _gtFileRepository.Save(file);

                return Ok(file);
            }
        }

        /// <summary>
        /// This api will save uploaded GT File
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [SwaggerOperation("SaveUploadFileGT")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost]
        public IHttpActionResult SaveUploadFileGT(SaveUploadFileBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("<br>", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return BadRequest(errors);
            }

            string filetype = "Other";
            string lowerFileName = model.FileName.ToLower();
            if (lowerFileName.EndsWith(".jpg") || lowerFileName.EndsWith(".png"))
                filetype = "Image";
            else if (lowerFileName.EndsWith(".pdf"))
                filetype = "PDF";

            string filepath = Path.Combine(HttpContext.Current.Server.MapPath("~/Content/UploatAttachment"), model.SaveFileName);
            FileInfo fileInfo = new FileInfo(filepath);
            string fileSize = _commonHelper.FormatBytes(fileInfo.Length);

            var file = new GTFile
            {
                UserID = UserId,
                UserName = UserName,
                GTID = model.GTId,
                GTType = model.GTType,
                FileName = model.FileName,
                SaveFileName = model.SaveFileName,
                FileType = filetype,
                FileSize = fileSize
            };

            _gtFileRepository.Save(file);

            return Ok(file);
        }

        /// <summary>
        /// This api will delete a File or folder
        /// </summary>
        /// <param name="type">This type id either "File" or "Folder"</param>
        /// <param name="id">This is either Folder or File Id</param>
        /// <returns></returns>
        [SwaggerOperation("Delete")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost]
        public IHttpActionResult Delete(string type, string id)
        {
            if (WebRTCConstants.GTTypeFolder.Equals(type, StringComparison.OrdinalIgnoreCase))
            {
                var folder = _folderRepository.GetSingle(id);
                folder.EntityState = EntityState.Deleted;
                _folderRepository.Save(folder);
            }
            else if (WebRTCConstants.GTTypeFile.Equals(type, StringComparison.OrdinalIgnoreCase))
            {
                var file = _fileRepository.GetSingle(id);
                file.EntityState = EntityState.Deleted;
                _fileRepository.Save(file);
            }

            return Ok();
        }

        /// <summary>
        /// This api will delete a File 
        /// </summary>
        /// <param name="id">File Id</param>
        /// <returns></returns>
        [SwaggerOperation("DeleteGT")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost]
        public IHttpActionResult DeleteGT(string id)
        {
            var file = _gtFileRepository.GetSingle(id);
            file.EntityState = EntityState.Deleted;
            _gtFileRepository.Save(file);

            return Ok();
        }

        /// <summary>
        /// This api will Rename File
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [SwaggerOperation("Rename")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost]
        public IHttpActionResult Rename(RenameFileBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("<br>", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return BadRequest(errors);
            }

            if (WebRTCConstants.GTTypeFile.Equals(model.GTType, StringComparison.OrdinalIgnoreCase))
            {
                if (WebRTCConstants.GTTypeFolder.Equals(model.Type, StringComparison.OrdinalIgnoreCase))
                {
                    var folder = _folderRepository.GetSingle(model.Id);
                    folder.Name = model.NewName;
                    folder.EntityState = EntityState.Modified;
                    _folderRepository.Save(folder);
                }

                if (WebRTCConstants.GTTypeFile.Equals(model.Type, StringComparison.OrdinalIgnoreCase))
                {
                    var file = _fileRepository.GetSingle(model.Id);
                    file.FileName = model.NewName;
                    file.EntityState = EntityState.Modified;
                    _fileRepository.Save(file);
                }
            }
            else
            {
                if (WebRTCConstants.GTTypeFolder.Equals(model.Type, StringComparison.OrdinalIgnoreCase))
                {
                    var folder = _gtFolderRepository.GetSingle(model.Id);
                    folder.Name = model.NewName;
                    folder.EntityState = EntityState.Modified;
                    _gtFolderRepository.Save(folder);
                }

                if (WebRTCConstants.GTTypeFile.Equals(model.Type, StringComparison.OrdinalIgnoreCase))
                {
                    var file = _gtFileRepository.GetSingle(model.Id);
                    file.FileName = model.NewName;
                    file.EntityState = EntityState.Modified;
                    _gtFileRepository.Save(file);
                }
            }

            return Ok();
        }

        /// <summary>
        /// This api will rename a Group or Team
        /// </summary>
        /// <param name="newName">New Name</param>
        /// <param name="id">Group Team File Id</param>
        /// <returns></returns>
        [SwaggerOperation("RenameGT")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost]
        public IHttpActionResult RenameGT(string newName, string id)
        {

            var file = _gtFileRepository.GetSingle(id);
            file.FileName = newName;
            file.EntityState = EntityState.Modified;
            _gtFileRepository.Save(file);

            return Ok();
        }

        /// <summary>
        /// This api will get or choose a file
        /// </summary>
        /// <param name="GtId">Group Team Id</param>
        /// <param name="GTType">Group Team Type</param>
        /// <param name="ids">Comma separated File Id </param>
        /// <returns></returns>
        [SwaggerOperation("ChooseFileGT")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost]
        public IHttpActionResult ChooseFileGT(string GtId, string GTType, string ids)
        {
            string[] idList = ids.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            var files = _fileRepository.FindBy(x => idList.Contains(x.Id)).ToList();

            int exists = 0;
            int added = 0;

            var gtFiles = new List<GTFile>();
            foreach (var file in files)
            {
                var gt = _gtFileRepository.GetSingle(x => x.SaveFileName == file.SaveFileName && x.GTID == GtId);
                if (gt != null)
                {
                    exists++;
                    continue;
                }

                var gtfile = new GTFile
                {
                    UserID = UserId,
                    UserName = UserName,
                    GTID = GtId,
                    GTType = GTType,
                    FileName = file.FileName,
                    SaveFileName = file.SaveFileName,
                    FileType = file.FileType,
                    FileSize = file.FileSize
                };

                gtFiles.Add(gtfile);
                added++;
            }

            _gtFileRepository.AddMany(gtFiles);

            return Ok(new { exists = exists, added = added });
        }

        /// <summary>
        /// This api will share File Binding Model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [SwaggerOperation("Share")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost]
        public IHttpActionResult Share(ShareFileBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("<br>", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return BadRequest(errors);
            }

            string[] gidsList = model.GroupIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            string[] tidsList = model.TeamIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            string[] cidsList = model.CandidateIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            var file = new FileCommonViewModel();
            if (WebRTCConstants.GTTypeFile.Equals(model.GTType, StringComparison.OrdinalIgnoreCase) && WebRTCConstants.GTTypeFile.Equals(model.Type, StringComparison.OrdinalIgnoreCase))
                file = _mapper.Map<Core.Entities.File, FileCommonViewModel>(_fileRepository.GetSingle(model.FileId));
            else if (!WebRTCConstants.GTTypeFile.Equals(model.GTType, StringComparison.OrdinalIgnoreCase) && WebRTCConstants.GTTypeFile.Equals(model.Type, StringComparison.OrdinalIgnoreCase))
                file = _mapper.Map<GTFile, FileCommonViewModel>(_gtFileRepository.GetSingle(model.FileId));

            var gtFiles = new List<GTFile>();
            var files = new List<Core.Entities.File>();
            foreach (string gid in gidsList)
            {
                var item = new GTFile
                {
                    UserID = UserId,
                    UserName = UserName,
                    GTID = gid,
                    GTType = "Group",
                    FileName = file.FileName,
                    SaveFileName = file.SaveFileName,
                    FileType = file.FileType,
                    FileSize = file.FileSize
                };
                gtFiles.Add(item);
            }
            foreach (string tid in tidsList)
            {
                var item = new GTFile
                {
                    UserID = UserId,
                    UserName = UserName,
                    GTID = tid,
                    GTType = "Team",
                    FileName = file.FileName,
                    SaveFileName = file.SaveFileName,
                    FileType = file.FileType,
                    FileSize = file.FileSize
                };
                gtFiles.Add(item);
            }
            foreach (string cid in cidsList)
            {
                var item = new Core.Entities.File
                {
                    UserID = cid,
                    FileName = file.FileName,
                    SaveFileName = file.SaveFileName,
                    FileType = file.FileType,
                    FileSize = file.FileSize
                };
                files.Add(item);
            }

            _fileRepository.AddMany(files);
            _gtFileRepository.AddMany(gtFiles);

            return Ok();
        }
    }
}