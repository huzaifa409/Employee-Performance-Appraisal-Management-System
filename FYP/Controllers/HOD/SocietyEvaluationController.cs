using FYP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using FYP.Models.DTO;

namespace FYP.Controllers.HOD
{
    [RoutePrefix("api/SocietyEvaluation")]
    public class SocietyEvaluationController : ApiController
    {

        FYPEntities db = new FYPEntities();
        [HttpPost]
        [Route("Submit")]
        public IHttpActionResult SubmitSocietyEvaluation([FromBody] List<SocietyEvaluationDTO> evaluations)
        {
            if (evaluations == null || evaluations.Count == 0)
                return BadRequest("No evaluation data received");

            try
            {
                foreach (var e in evaluations)
                {
                    // ---------------- NORMALIZE INPUT ----------------
                    var evaluatorId = e.EvaluatorId?.Trim();
                    var evaluateeId = e.EvaluateeId?.Trim();
                    var evaluationType = e.EvaluationType?.Trim();

                    // ================= DEBUG (REMOVE LATER IF YOU WANT) =================
                    // This helps you SEE EXACT VALUES coming from frontend
                    var debugInfo = new
                    {
                        evaluatorRaw = e.EvaluatorId,
                        evaluateeRaw = e.EvaluateeId,
                        evaluatorClean = evaluatorId,
                        evaluateeClean = evaluateeId,
                        sessionId = e.SessionId,
                        societyId = e.SocietyId,
                        questionId = e.QuestionId
                    };

                    // ---------------- VALIDATION ----------------

                    var teacherExists =
                        db.Teacher.Any(t => t.userID == evaluatorId)
                        &&
                        db.Teacher.Any(t => t.userID == evaluateeId);

                    if (!teacherExists)
                    {
                        return Ok(new
                        {
                            success = false,
                            error = "Invalid Teacher ID(s)",
                            debug = debugInfo
                        });
                    }

                    var sessionExists = db.Session.Any(s => s.id == e.SessionId);
                    if (!sessionExists)
                    {
                        return Ok(new
                        {
                            success = false,
                            error = "Invalid SessionId",
                            debug = debugInfo
                        });
                    }

                    var societyExists = db.Societies.Any(s => s.SocietyId == e.SocietyId);
                    if (!societyExists)
                    {
                        return Ok(new
                        {
                            success = false,
                            error = "Invalid SocietyId",
                            debug = debugInfo
                        });
                    }

                    var questionExists = db.Questions.Any(q => q.QuestionID == e.QuestionId);
                    if (!questionExists)
                    {
                        return Ok(new
                        {
                            success = false,
                            error = "Invalid QuestionId",
                            debug = debugInfo
                        });
                    }

                    // ---------------- DUPLICATE CHECK ----------------

                    var exists = db.SocietyEvaluation.Any(x =>
                        x.EvaluatorId == evaluatorId &&
                        x.EvaluateeId == evaluateeId &&
                        x.SocietyId == e.SocietyId &&
                        x.QuestionId == e.QuestionId &&
                        x.SessionId == e.SessionId &&
                        x.EvaluationType == evaluationType
                    );

                    if (!exists)
                    {
                        db.SocietyEvaluation.Add(new SocietyEvaluation
                        {
                            EvaluatorId = evaluatorId,
                            EvaluateeId = evaluateeId,
                            SocietyId = e.SocietyId,
                            QuestionId = e.QuestionId,
                            Score = e.Score,
                            SessionId = e.SessionId,
                            EvaluationType = evaluationType
                        });
                    }
                }

                db.SaveChanges();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    error = ex.Message,
                    inner = ex.InnerException?.InnerException?.Message
                });
            }
        }
        [HttpGet]
        [Route("GetSubmitted/{evaluatorId}/{evaluationType}/{sessionId}")]
        public IHttpActionResult GetSubmittedEvaluations(string evaluatorId, string evaluationType, int sessionId)
        {

            var submitted = db.SocietyEvaluation
                .Where(x =>
                    x.EvaluatorId.Trim().ToLower() == evaluatorId.Trim().ToLower() &&
                    x.SessionId == sessionId &&
                    x.EvaluationType == evaluationType
                )
                .Select(x => x.EvaluateeId)
                .Distinct()
                .ToList();

            return Ok(submitted);
        }

        [HttpGet]
        [Route("GetChairpersonSocietyWithMentors/{teacherId}/{sessionId}")]
        public IHttpActionResult GetChairpersonSocietyWithMentors(string teacherId, int sessionId)
        {
            // 🔹 Find society where this teacher is chairperson
            var society = db.SocietyAssignments
                .Where(x => x.TeacherId == teacherId &&
                            x.SessionId == sessionId &&
                            x.IsChairperson == true)
                .Select(x => new
                {
                    x.SocietyId,
                    SocietyName = x.Societies.SocietyName
                })
                .FirstOrDefault();

            // ❌ Not a chairperson
            if (society == null)
            {
                return Ok(new
                {
                    IsChairperson = false
                });
            }

            // 🔹 Get mentors of that society
            var mentors = db.SocietyAssignments
                .Where(x => x.SocietyId == society.SocietyId &&
                            x.SessionId == sessionId &&
                            x.IsMentor == true)
                .Join(db.Teacher,
                      a => a.TeacherId,
                      t => t.userID,
                      (a, t) => new
                      {
                          TeacherId = t.userID,
                          TeacherName = t.name,
                          SocietyId = a.SocietyId,
                          SocietyName = society.SocietyName
                      })
                .ToList();

            return Ok(new
            {
                IsChairperson = true,
                SocietyId = society.SocietyId,
                SocietyName = society.SocietyName,
                Mentors = mentors
            });
        }


        [HttpGet]
        [Route("GetChairpersons/{sessionId}")]
        public IHttpActionResult GetChairpersons(int sessionId)
        {
            var data = db.SocietyAssignments
                .Where(a => a.SessionId == sessionId
                         && a.IsChairperson == true)
                .Join(db.Teacher,
                      a => a.TeacherId,
                      t => t.userID,
                      (a, t) => new
                      {
                          SocietyId = a.SocietyId,
                          SocietyName = a.Societies.SocietyName,
                          SessionId = a.SessionId,
                          TeacherId = a.TeacherId,
                          TeacherName = t.name
                      })
                .ToList(); // 👈 ONLY ONE CHAIRPERSON

            return Ok(data);
        }
    }
}