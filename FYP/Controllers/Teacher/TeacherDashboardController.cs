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
                    .Where(q =>q.flag == "1" &&q.type.Trim().ToLower() == type.Trim().ToLower())
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







        private int GetDesignationRank(string designation)
        {
            if (string.IsNullOrWhiteSpace(designation))
                return 0;

            switch (designation.Trim().ToLower())
            {
                case "hod": return 5;                  // 🔥 highest
                case "professor": return 4;
                case "assistant professor": return 3;
                case "teacher": return 2;
                case "junior teacher": return 1;
                default: return 0;
            }
        }



        [HttpGet]
        [Route("GetTeachersWithCourses")]
        public IHttpActionResult GetTeachersWithCourses(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return BadRequest("UserId is required");

                string normalizedUserId = userId.Trim().ToLower();

                // 🔹 Current Teacher
                var currentTeacher = db.Teacher
                    .FirstOrDefault(t => t.userID.Trim().ToLower() == normalizedUserId);

                if (currentTeacher == null)
                    return Ok(new List<object>());

                int currentRank = GetDesignationRank(currentTeacher.designation);

                var data = db.Enrollment
                    .GroupBy(e => e.teacherID)
                    .Select(g => new
                    {
                        TeacherID = g.Key,
                        TeacherInfo = db.Teacher
                            .Where(t => t.userID == g.Key)
                            .Select(t => new
                            {
                                t.name,
                                t.designation
                            })
                            .FirstOrDefault(),

                        Courses = g
                            .Select(x => x.courseCode)
                            .Distinct()
                            .ToList()
                    })
                    .ToList()

                    // 🔥 FILTER LOGIC
                    .Where(t =>
                    {
                        if (t.TeacherInfo == null)
                            return false;

                        int targetRank = GetDesignationRank(t.TeacherInfo.designation);

                        // ❌ no self evaluation
                        if (t.TeacherID.Trim().ToLower() == normalizedUserId)
                            return false;

                        // 🔥 RULE: same level + lower
                        return targetRank <= currentRank;
                    })

                    .Select(t => new
                    {
                        TeacherID = t.TeacherID,
                        TeacherName = t.TeacherInfo.name,
                        Courses = t.Courses
                    })
                    .ToList();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }










        [HttpGet]
        [Route("IsEvaluator")]
        public IHttpActionResult IsEvaluator(string userId)
        {
            
            var exists = db.PeerEvaluator.Any(e => e.teacherID.Trim().ToLower() == userId.Trim().ToLower());


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

            // Get latest session
            var latestSession = db.Session
                                  .OrderByDescending(s => s.id) // or CreatedDate
                                  .FirstOrDefault();

            if (latestSession == null)
                return BadRequest("No active session found");

            foreach (var eval in evaluations)
            {
                var record = new PeerEvaluation
                {
                    evaluatorID = eval.evaluatorID,
                    evaluateeID = eval.evaluateeID,
                    questionID = eval.questionID,
                    courseCode = eval.courseCode,
                    score = eval.score,
                    SessionID = latestSession.id // <-- store latest session
                };

                db.PeerEvaluation.Add(record);
            }

            db.SaveChanges();

            return Ok(new { success = true, sessionID = latestSession.id });
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


        [HttpGet]
        [Route("GetTeacherName/{userId}")]
        public IHttpActionResult GetTeacherName(string userId)
        {
            System.Diagnostics.Debug.WriteLine("GetTeacherName called with userId: " + userId);

            var teacher = db.Teacher.FirstOrDefault(s => s.userID.Trim().ToLower() == userId.Trim().ToLower());
            if (teacher == null)
            {
                System.Diagnostics.Debug.WriteLine("Teacher not found!");
                return NotFound();
            }

            return Ok(teacher.name);
        }




        //[HttpGet]
        //[Route("GetPeerEvaluatorID")]
        //public IHttpActionResult GetPeerEvaluatorID(string userId)
        //{
        //    try
        //    {
        //        // Get the PeerEvaluator entry for this teacher (you may also filter by current session)
        //        var peerEvaluator = db.PeerEvaluator
        //            .FirstOrDefault(pe => pe.teacherID.Trim().ToLower() == userId.Trim().ToLower());

        //        if (peerEvaluator == null)
        //            return Ok(new { peerEvaluatorID = (int?)null });

        //        return Ok(new { peerEvaluatorID = peerEvaluator.id });
        //    }
        //    catch (Exception ex)
        //    {
        //        return InternalServerError(ex);
        //    }
        //}


        [HttpGet]
        [Route("GetPeerEvaluatorID")]
        public IHttpActionResult GetPeerEvaluatorID(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return BadRequest("UserId is required");

                string normalizedUserId = userId.Trim().ToLower();

                var teacher = db.Teacher
                    .FirstOrDefault(t => t.userID.Trim().ToLower() == normalizedUserId);

                if (teacher == null)
                    return Ok(new { peerEvaluatorID = (int?)null, isAllowed = false });

                var latestSession = db.Session
                    .OrderByDescending(s => s.id)
                    .FirstOrDefault();

                if (latestSession == null)
                    return Ok(new { peerEvaluatorID = (int?)null, isAllowed = false });

                bool isPermanent = teacher.isPermanentEvaluator == 1;

                // STEP 1: check existing evaluator in latest session
                var peerEvaluator = db.PeerEvaluator
                    .FirstOrDefault(pe =>
                        pe.teacherID.Trim().ToLower() == normalizedUserId &&
                        pe.sessionID == latestSession.id
                    );

                // STEP 2: AUTO INSERT ONLY ONCE (FIXED)
                if (isPermanent && peerEvaluator == null)
                {
                    peerEvaluator = new PeerEvaluator
                    {
                        teacherID = normalizedUserId,
                        sessionID = latestSession.id
                    };

                    db.PeerEvaluator.Add(peerEvaluator);
                    db.SaveChanges(); // save immediately so ID is generated
                }

                // STEP 3: response
                if (peerEvaluator != null)
                {
                    return Ok(new
                    {
                        peerEvaluatorID = peerEvaluator.id,
                        isAllowed = true,
                        source = isPermanent ? "PermanentTeacherAutoAdded" : "SessionEvaluator"
                    });
                }

                return Ok(new
                {
                    peerEvaluatorID = (int?)null,
                    isAllowed = false,
                    source = "NotEvaluator"
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }




        int employeeTypeId;






        [Route("SeeOwnPerformance")]
        public IHttpActionResult GetTeacherPerformance(string userId, int sessionId)
        {
            FYPEntities db = new FYPEntities();

            var response = new PerformanceDto();
            var kpiList = new List<KpiDto>();

            // 🔹 STEP 1: Get Employee Type ID from userId
            var role = db.Teacher
                .Where(u => u.userID == userId)
                .Select(u => u.department)
                .FirstOrDefault();

            if (role == "CS")
            {
                employeeTypeId = 1;
            }
            else if (role == "Non CS")
            {
                employeeTypeId = 2;
            }

            var kpiIds = db.EmployeSessionKPI
            .Where(e => e.SessionID == sessionId && e.EmployeeTypeID == employeeTypeId)
            .Select(e => e.KPIID)
            .Distinct()
            .ToList();


            // 🔹 STEP 2: Get KPIs for this employee type
            var kpis = db.KPI
                    .Where(k => kpiIds.Contains(k.id))
                    .ToList();

            int overallScore = 0;
            int overallWeight = 0;

            foreach (var kpi in kpis)
            {
                var subKpiIds = db.EmployeSessionKPI
                .Where(e => e.KPIID == kpi.id &&
                e.SessionID == sessionId &&
                e.EmployeeTypeID == employeeTypeId)
                .Select(e => e.SubKPIID)
                .ToList();

                var subKpis = db.SubKPI
                 .Where(s => subKpiIds.Contains(s.id))
                 .ToList();

                int kpiScore = 0;
                int kpiTotal = 0;

                var subKpiDtos = new List<SubKpiDto>();

                foreach (var sub in subKpis)
                {
                    double avg = 0;

                    // 🔹 STUDENT
                    if (sub.name == "Student Evaluation ")
                    {
                        var scores = db.StudentEvaluation
                            .Where(se => se.SessionID == sessionId &&
                                db.Enrollment.Any(e =>
                                    e.id == se.enrollmentID &&
                                    e.teacherID == userId))
                            .Select(se => (int?)se.score)
                            .ToList();

                        avg = scores.Count > 0 ? scores.Average() ?? 0 : 0;
                    }

                    // 🔹 PEER
                    else if (sub.name == "Peer Evaluation ")
                    {
                        var scores = db.PeerEvaluation
                            .Where(pe => pe.evaluateeID == userId &&pe.SessionID== sessionId)
                            .Select(pe => (int?)pe.score)
                            .ToList();

                        avg = scores.Count > 0 ? scores.Average() ?? 0 : 0;
                    }

                    // 🔹 OTHER
                    else
                    {
                        var scores = db.KPIScore
                            .Where(s => s.empID == userId && s.empKPIID == sub.id)
                            .Select(s => (int?)s.score)
                            .ToList();

                        avg = scores.Count > 0 ? scores.Average() ?? 0 : 0;
                    }

                    // 🔹 Sub KPI weight
                    int weight = db.SessionKPIWeight
                         .Where(w => w.SubKPIID == sub.id && w.SessionID == sessionId)
                         .Select(w => w.Weight)
                         .FirstOrDefault() ?? 0;

                    // 🔹 Convert to marks (max = 4)
                    int finalScore = (int)Math.Round((avg / 4.0) * weight);

                    subKpiDtos.Add(new SubKpiDto
                    {
                        Name = sub.name,
                        Score = finalScore,
                        Total = weight
                    });

                    kpiScore += finalScore;
                    kpiTotal += weight;
                }

                // 🔹 Main KPI weight (80%, 20%)
                int kpiWeight = db.SessionKPIWeight
                    .Where(w => w.KPIID == kpi.id && w.SessionID == sessionId && w.SubKPIID == null)
                    .Select(w => w.Weight)
                    .FirstOrDefault() ?? 0;

                double kpiPercentage = kpiTotal > 0 ? (double)kpiScore / kpiTotal : 0;
                int weightedKpiScore = (int)Math.Round(kpiPercentage * kpiWeight);

                overallScore += kpiScore;
                overallWeight += kpiTotal;

                kpiList.Add(new KpiDto
                {
                    Name = kpi.name,
                    Score = kpiScore,
                    Total = kpiTotal,
                    SubKpis = subKpiDtos
                });
            }

            response.Kpis = kpiList;
            response.OverallPercentage = overallWeight > 0
                ? (int)Math.Round((double)overallScore * 100 / overallWeight)
                : 0;

            response.ObtainedPoints = overallScore;
            response.TotalPoints = overallWeight;

            return Ok(response);
        }

    }


}









