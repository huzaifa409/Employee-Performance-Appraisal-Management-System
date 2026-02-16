using FYP.Models;
using FYP.Models.DTO;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;

namespace FYP.Controllers.Teacher
{
    [RoutePrefix("api/TeacherDashboard")]
    public class TeacherDashboardController : ApiController
    {
        FYPEntities db = new FYPEntities();

        // GET: api/TeacherDashboard/GetActiveQuestionnaire
        [HttpGet]
        [Route("GetActiveQuestionnaire/{type}")]
        public IHttpActionResult GetActiveQuestionnaire(string type)
        {
            try
            {
                // Get Questionnaire where flag = '1'
                var questionnaire = db.Questionare
                    .Include(q => q.Questions)
                    .Where(q => q.flag == "1" && q.type == type)
                    .Select(q => new
                    {
                        QuestionareID = q.id,
                        Type = q.type,
                        Flag = q.flag,
                        Questions = q.Questions.Select(ques => new
                        {
                            ques.QuestionID,
                            ques.QuestionText
                        }).ToList()
                    })
                    .FirstOrDefault();

                if (questionnaire == null)
                    return Ok(new { Message = "No active questionnaire found" });

                return Ok(questionnaire);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        // GET: api/TeacherDashboard/GetTeachersWithCourses
        [HttpGet]
        [Route("GetTeachersWithCourses")]
        public IHttpActionResult GetTeachersWithCourses()
        {
            var data = db.Enrollment
                .GroupBy(e => e.teacherID)
                .Select(g => new
                {
                    TeacherID = g.Key,
                    TeacherName = db.Teacher
                        .Where(t => t.userID == g.Key)
                        .Select(t => t.name)
                        .FirstOrDefault(),

                    Courses = g
                        .Select(x => x.courseCode)
                        .Distinct()
                        .ToList()
                })
                .ToList();

            return Ok(data);
        }


        [HttpGet]
        [Route("IsEvaluator")]
        public IHttpActionResult IsEvaluator(int userId)
        {
            // Convert userId to string to match teacherID type
            var exists = db.PeerEvaluator.Any(e => e.teacherID == userId.ToString());

            return Ok(new
            {
                isEvaluator = exists
            });
        }


        [HttpPost]
        [Route("SubmitEvaluation")]
        public IHttpActionResult SubmitEvaluation([FromBody] List<PeerEvaluation> evaluations)
        {
            if (evaluations == null || !evaluations.Any())
                return BadRequest("Invalid submission");

            foreach (var eval in evaluations)
            {
                var record = new PeerEvaluation
                {
                    evaluatorID = eval.evaluatorID,
                    evaluateeID = eval.evaluateeID,
                    questionID = eval.questionID,
                    courseCode = eval.courseCode,
                    score = eval.score
                };

                db.PeerEvaluation.Add(record);
            }

            db.SaveChanges();

            return Ok(new { success = true });
        }




        [HttpGet]
        [Route("GetSubmittedEvaluations")]
        public IHttpActionResult GetSubmittedEvaluations(int evaluatorID)
        {
            // fetch all submitted evaluations for this evaluator
            var submitted = db.PeerEvaluation
                .Where(p => p.evaluatorID == evaluatorID)
                .Select(p => new
                {
                    TeacherID = p.evaluateeID, // if your evaluateeID is int, adjust type
                    CourseCode = p.courseCode
                })
                .Distinct() // one entry per course per teacher
                .ToList();

            return Ok(submitted);
        }




        





    }
}
