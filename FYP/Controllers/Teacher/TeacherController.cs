using ExcelDataReader;
using FYP.Models;
using System;
using System.Data;
using System.Web;
using System.Web.Http;
using FYP.Models;

namespace FYP.Controllers.Teacher
{
    [RoutePrefix("api/teacher")]
    public class TeacherController : ApiController
    {
        FYPEntities db = new FYPEntities();

        // POST api/teacher/upload
        [HttpPost]
        [Route("upload")]
        public IHttpActionResult Upload()
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
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                            {
                                UseHeaderRow = true
                            }
                        });

                        var dataTable = result.Tables[0];

                        foreach (DataRow row in dataTable.Rows)
                        {
                            // Hardcoded UserID for all teachers
                            //string hardcodedUserId = "3"; // replace with actual UserID from Users table

                            FYP.Models.Teacher teacher = new FYP.Models.Teacher
                            {
                                userID = row["Userid"].ToString(), // foreign key
                                name = row["Name"].ToString(),
                                department = row["Department"].ToString()
                            };

                            db.Teacher.Add(teacher);
                        }

                        db.SaveChanges();
                    }
                }

                return Ok("File uploaded and data saved successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }



        [HttpGet]
        [Route("ping")]
        public IHttpActionResult Ping()
        {
            return Ok("API is alive");
        }

    }
}
