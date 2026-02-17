using FYP.Models;
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
            var enrollments = db.Enrollment
                .Where(e => e.studentID == studentID)
                .Select(e => new
                {
                    EnrollmentID = e.id,

                    CourseCode = e.courseCode,
                    CourseTitle = e.Course.title,      

                    TeacherID = e.teacherID,
                    TeacherName = e.Teacher.name,  

                    SessionID = e.sessionID,
                    SessionName = e.Session.name       
                })
                .ToList();

            if (!enrollments.Any())
            {
                return NotFound();
            }

            return Ok(enrollments);
        }



        [HttpGet]
        [Route("GetStudentName/{studentId}")]
        public IHttpActionResult GetStudentName(string studentId)
        {
            var student = db.Student.FirstOrDefault(s => s.userID == studentId);
            if (student == null)
                return NotFound();
            return Ok(student.name);
        }
    }
}
