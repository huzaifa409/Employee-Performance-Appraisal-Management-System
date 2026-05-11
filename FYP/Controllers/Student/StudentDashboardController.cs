using FYP.Models;
using FYP.Models.DTO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
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
            // 🔹 Step 1: Get latest session (descending)
            var latestSession = db.Session
                .OrderByDescending(s => s.id)
                .FirstOrDefault();

            if (latestSession == null)
                return NotFound();

            // 🔹 Step 2: Filter enrollments by student + latest session
            var enrollments = db.Enrollment
                .Where(e => e.studentID == studentID && e.sessionID == latestSession.id)
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
                return NotFound();

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

            try
            {
                // ✅ Get latest session from DB
                var latestSession = db.Session
                    .OrderByDescending(s => s.id)
                    .FirstOrDefault();

                if (latestSession == null)
                    return BadRequest("No active session found");

                foreach (var e in evaluations)
                {
                    db.StudentEvaluation.Add(new StudentEvaluation
                    {
                        enrollmentID = e.enrollmentID,
                        questionID = e.questionID,
                        score = e.score,
                        StudentId = e.StudentId,
                        SessionID = latestSession.id   // ✅ FIXED HERE
                    });
                }

                db.SaveChanges();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }




        [HttpGet]
        [Route("GetSubmittedStudentEvaluations/{studentId}")]
        public IHttpActionResult GetSubmittedStudentEvaluations(string studentId)
        {
            var latestSession = db.Session
                .OrderByDescending(s => s.id)
                .FirstOrDefault();

            if (latestSession == null)
                return Ok(new List<int>());

            var submitted = db.StudentEvaluation
                .Where(se =>
                    se.StudentId.Trim().ToLower() == studentId.Trim().ToLower()
                    && se.SessionID == latestSession.id   // ✅ FILTER BY SESSION
                )
                .Select(se => se.enrollmentID)
                .Distinct()
                .ToList();

            return Ok(submitted);
        }





        [HttpPost]
        [Route("SubmitConfidentialEvaluation")]
        public IHttpActionResult SubmitConfidentialEvaluation(
              [FromBody] ConfidentialEvaluationDto model)
        {
            if (model == null || model.Answers == null || !model.Answers.Any())
                return BadRequest("Invalid submission");

            try
            {

                var enrollment = db.Enrollment
                    .FirstOrDefault(e => e.id == model.EnrollmentId);

                if (enrollment == null)
                    return NotFound();


                var student = db.Student
                    .FirstOrDefault(s => s.userID == model.StudentId);

                var teacher = db.Teacher
                    .FirstOrDefault(t => t.userID == enrollment.teacherID);

                var course = db.Course
                    .FirstOrDefault(c => c.code == enrollment.courseCode);

                var questionIds = model.Answers.Select(a => a.questionId).ToList();

                var questions = db.Questions
                    .Where(q => questionIds.Contains(q.QuestionID))
                    .ToList();

            
                var emailObject = new
                {
                    studentId = model.StudentId,
                    teacherId = teacher?.userID,
                    session = enrollment.Session.name,
                    subjectCode = enrollment.courseCode,
                    submittedOn = DateTime.Now,
                    evaluation = model.Answers.Select(a =>
                    {
                        var question = questions
                            .FirstOrDefault(q => q.QuestionID == a.questionId);

                        return new
                        {
                            qId = a.questionId,
                            questionText = question?.QuestionText,
                            score = a.score
                        };
                    }).ToList()
                };

                //string body = Newtonsoft.Json.JsonConvert.SerializeObject(emailObject, Newtonsoft.Json.Formatting.Indented);

                string body = "START_EVAL\n" +
              JsonConvert.SerializeObject(emailObject, Formatting.Indented) +
              "\nEND_EVAL";



                SendEmail(body);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    error = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }



        private void SendEmail(string body)
        {
            var fromAddress = new MailAddress("onlyforwork015@gmail.com");
            var activeEmail = db.Email
                           .FirstOrDefault(x => x.isActive == true);
            var toAddress = new MailAddress(activeEmail.mail);

            const string fromPassword = "iiqoyebexdzwsdop";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    fromAddress.Address,
                    fromPassword)
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = "Confidential Evaluation - EPAS",
                Body = body
            })
            {
                smtp.Send(message);
            }
        }

    }

}
