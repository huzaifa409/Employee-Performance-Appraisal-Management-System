using ExcelDataReader;
using FYP.Models;
using System;
using System.Data;
using System.Web;
using System.Web.Http;

namespace FYP.Controllers
{
    

    // STUDENT API
    [RoutePrefix("api/student")]
    public class StudentController : ApiController
    {
        FYPEntities db = new FYPEntities();

        [HttpPost]
        [Route("upload")]
        public IHttpActionResult UploadStudent()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                if (httpRequest.Files.Count == 0)
                    return BadRequest("No file uploaded.");

                var file = httpRequest.Files[0];
                if (file == null || file.ContentLength == 0)
                    return BadRequest("Empty file.");

                // Needed for ExcelDataReader
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                using (var stream = file.InputStream)
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                    });

                    var dataTable = result.Tables[0];

                    int insertedCount = 0;
                    int skippedInvalidSession = 0;
                    int skippedDuplicate = 0;

                    foreach (DataRow row in dataTable.Rows)
                    {
                        // Validate required columns
                        if (row["UserID"] == DBNull.Value || row["Name"] == DBNull.Value || row["AdmissionSessionId"] == DBNull.Value)
                            continue;

                        string userId = row["UserID"].ToString().Trim();
                        string name = row["Name"].ToString().Trim();
                        int sessionId;

                        // Try parse AdmissionSessionId
                        if (!int.TryParse(row["AdmissionSessionId"].ToString(), out sessionId))
                            continue;

                        // Check if student already exists
                        if (db.Student.Find(userId) != null)
                        {
                            skippedDuplicate++;
                            continue;
                        }

                        // Check if session exists
                        if (db.Session.Find(sessionId) == null)
                        {
                            skippedInvalidSession++;
                            continue;
                        }

                        // Add student
                        db.Student.Add(new FYP.Models.Student
                        {
                            userID = userId,
                            name = name,
                            admissionSessionID = sessionId
                        });

                        insertedCount++;
                    }

                    db.SaveChanges();

                    string message = $"{insertedCount} students uploaded successfully.";
                    if (skippedDuplicate > 0) message += $" {skippedDuplicate} duplicate(s) skipped.";
                    if (skippedInvalidSession > 0) message += $" {skippedInvalidSession} row(s) skipped due to invalid session ID.";

                    return Ok(message);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        [HttpGet]
        [Route("ping")]
        public IHttpActionResult PingStudent() => Ok("Student API is alive");
    }
}
