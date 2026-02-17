using FYP.Models;
using System.Collections.Generic;
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



        [HttpPost]
        [Route("SubmitStudentEvaluation")]
        public IHttpActionResult SubmitStudentEvaluation(
    [FromBody] List<StudentEvaluation> evaluations)
        {
            if (evaluations == null || !evaluations.Any())
                return BadRequest("Invalid submission");

            foreach (var e in evaluations)
            {
                db.StudentEvaluation.Add(new StudentEvaluation
                {
                    enrollmentID = e.enrollmentID,
                    questionID = e.questionID,
                    score = e.score,
                    StudentId = e.StudentId
                });
            }

            db.SaveChanges();

            return Ok(new { success = true });
        }




        [HttpGet]
        [Route("GetSubmittedStudentEvaluations/{studentId}")]
        public IHttpActionResult GetSubmittedStudentEvaluations(string studentId)
        {
            var submitted = db.StudentEvaluation
                .Where(se => se.StudentId.Trim().ToLower() == studentId.Trim().ToLower())
                .Select(se => se.enrollmentID)
                .Distinct()
                .ToList();

            return Ok(submitted);
        }

    }

}
