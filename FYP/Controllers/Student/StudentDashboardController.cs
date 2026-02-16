using FYP.Models;
using FYP.Models.DTO;
using System.Linq;
using System.Web.Http;

namespace FYP.Controllers.Student
{
    [RoutePrefix("api/studentDashboard")]
    public class StudentDashboardController : ApiController
    {
        FYPEntities db = new FYPEntities();

        // GET api/studentDashboard/enrollments/STU001
        [HttpGet]
        [Route("enrollments/{studentID}")]
        public IHttpActionResult GetEnrollmentsByStudent(string studentID)
        {
            // Fetch all enrollment records for the given studentID
            var enrollments = db.Enrollment
                                .Where(e => e.studentID == studentID)
                                .Select(e => new
                                {
                                    e.id,
                                    e.studentID,
                                    e.teacherID,
                                    e.courseCode,
                                    e.sessionID
                                })
                                .ToList();

            if (enrollments == null || enrollments.Count == 0)
            {
                return NotFound(); // 404 if no records found
            }

            return Ok(enrollments); // 200 OK with JSON data
        }



      
    }
}
