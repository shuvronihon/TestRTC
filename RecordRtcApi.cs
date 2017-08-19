using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WebRTC.Infrastructure.Helpers;
using WebRTC.Models;

namespace WebRTC.Controllers
{
    /// <summary>
    /// RecordRTCAPiController
    /// </summary>
    public class RecordRTCApiController : BaseApiController
    {
        private ICommonHelper _commonHelper;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="commonHelper"></param>
        public RecordRTCApiController(ICommonHelper commonHelper)
        {
            _commonHelper = commonHelper;
        }

        /// <summary>
        /// This api will save Post Recorded Audio and Video
        /// </summary>
        /// <returns></returns>
        [SwaggerOperation("PostRecordedAudioVideo")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost, ActionName("PostRecordedAudioVideo")]
        public async Task<IHttpActionResult> PostRecordedAudioVideo()
        {
            if (!Request.Content.IsMimeMultipartContent())
                return BadRequest("Unsupported media type.");

            var uploadPath = HttpContext.Current.Server.MapPath("~/Content/recordvideos");
            var provider = new UploadMultipartFormProvider(uploadPath);

            var streamcontent = _commonHelper.GetNormalizedStreamContent(Request);

            // Read the form data and return an async task.
            await streamcontent.ReadAsMultipartAsync(provider);

            var fileData = provider.FileData.First();

            var fileName = fileData.Headers.ContentDisposition.FileName?.Replace("\"", "");

            var response = new
            {
                Successful = true,
                FileName = fileName,
                OriginalFileName = Path.GetFileName(fileName)
            };
            return Ok(response);
        }

        /// <summary>
        /// This api will Delete File
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [SwaggerOperation("DeleteFile")]
        [SwaggerResponse(200, type: typeof(bool), description: "success")]
        [SwaggerResponse(400, type: typeof(ClientError), description: "Bad Request.")]
        [SwaggerResponse(500, type: typeof(ErrorResponse), description: "Unexpected error.")]
        [HttpPost]
        public IHttpActionResult DeleteFile(DeleteFileBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("<br>", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return BadRequest(errors);
            }

            var filePath = HttpContext.Current.Server.MapPath("~/Content/recordvideos/" + model.file_name);
            new FileInfo(filePath).Delete();
            return Ok();
        }
    }    
}