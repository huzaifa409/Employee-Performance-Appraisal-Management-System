using FYP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Transactions;
using System.Web.Http;
using static FYP.Models.DTO.CourseManagementDto;

namespace FYP.Controllers.HOD
{
    [RoutePrefix("api/CourseManagement")]
    public class CourseManagementController : ApiController
    {

        FYPEntities db = new FYPEntities();

        //[HttpGet]
        //[Route("EnrollmentCourses/{sessionId}")]
        //public IHttpActionResult GetEnrollmentCourses(int sessionId)
        //{
        //    var data = (from e in db.Enrollment
        //                join t in db.Teacher
        //                    on e.teacherID equals t.userID
        //                join c in db.Course
        //                    on e.courseCode equals c.code
        //                where e.sessionID == sessionId
        //                select new
        //                {
        //                    id = e.id,
        //                    teacher = t.name,
        //                    teacherID = t.userID,
        //                    course = c.title,
        //                    code = e.courseCode
        //                }).ToList();

        //    return Ok(data);
        //}


        [HttpGet]
        [Route("EnrollmentCourses/{sessionId}")]
        public IHttpActionResult GetEnrollmentCourses(int sessionId)
        {
            var data = (from e in db.Enrollment
                        join t in db.Teacher
                            on e.teacherID equals t.userID
                        join c in db.Course
                            on e.courseCode equals c.code
                        where e.sessionID == sessionId
                        group new { e, t, c } by new
                        {
                            t.userID,
                            t.name,
                            c.code,
                            c.title
                        } into g
                        select new
                        {
                            id = g.FirstOrDefault().e.id,
                            teacher = g.Key.name,
                            teacherID = g.Key.userID,
                            course = g.Key.title,
                            code = g.Key.code
                        }).ToList();

            return Ok(data);
        }







        /// //////// ///// ////////  ////              COURSE MANAGEMENT 
        /// 


        // 2. POST: Evaluate Submission (Handles Paper and Folder)
        [HttpPost]
        [Route("SaveEvaluation")]
        public IHttpActionResult SaveEvaluation(EvaluationRequestDTO dto)
        {
            if (dto == null || dto.Evaluations == null || !dto.Evaluations.Any())
                return BadRequest("Invalid Data");

            try
            {
                using (var scope = new TransactionScope())
                {
                    int total = dto.Evaluations.Count;

                    //  FIX: Har course ke marks jodo
                    int totalPaperEarned = 0;
                    int totalFolderEarned = 0;

                    foreach (var eval in dto.Evaluations)
                    {
                        // On-time = 5, Late = 2
                        totalPaperEarned += eval.PaperStatus.ToLower().Contains("on-time") ? 5 : 2;
                        totalFolderEarned += eval.FolderStatus.ToLower().Contains("on-time") ? 5 : 2;
                    }

                    // FIX: Sum ki bajaye average lo
                    // Example: 3 courses sab on-time  → (5+5+5)/3 = 5 
                    // Example: 3 courses sab late      → (2+2+2)/3 = 2 
                    // Example: 2 courses 1 each        → (5+2)/2   = 4 
                    int paperScore = (int)Math.Round((double)totalPaperEarned / total);
                    int folderScore = (int)Math.Round((double)totalFolderEarned / total);

                    // FIX: Contains ki bajaye direct ID comparison — EF error nahi dega
                    // Paper ke purane scores delete karo
                    var paperMapping = db.EmployeSessionKPI.FirstOrDefault(m =>
                        m.SubKPI.name.Contains("Paper") && m.SessionID == dto.SessionID);

                    if (paperMapping != null)
                    {
                        int paperMappingId = paperMapping.id;
                        var oldPaper = db.KPIScore
                            .Where(s => s.empID == dto.TeacherID && s.empKPIID == paperMappingId)
                            .ToList();
                        db.KPIScore.RemoveRange(oldPaper);
                    }

                    // Folder ke purane scores delete karo
                    var folderMapping = db.EmployeSessionKPI.FirstOrDefault(m =>
                        m.SubKPI.name.Contains("Folder") && m.SessionID == dto.SessionID);

                    if (folderMapping != null)
                    {
                        int folderMappingId = folderMapping.id;
                        var oldFolder = db.KPIScore
                            .Where(s => s.empID == dto.TeacherID && s.empKPIID == folderMappingId)
                            .ToList();
                        db.KPIScore.RemoveRange(oldFolder);
                    }

                    // Pehle delete save karo
                    db.SaveChanges();

                    //  Naye averaged scores save karo (ek record per SubKPI)
                    UpsertScore(dto.TeacherID, dto.SessionID, "Paper Submission", paperScore, dto.HODID);
                    UpsertScore(dto.TeacherID, dto.SessionID, "Folder Submission", folderScore, dto.HODID);

                    db.SaveChanges();
                    scope.Complete();

                    return Ok(new
                    {
                        message = "Evaluation saved successfully!",
                        paperScore,
                        folderScore,
                        totalCourses = total
                    });
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Backend Error: " + ex.Message));
            }
        }

        // FIX: CourseCode parameter hata diya — ek hi record per teacher per SubKPI
        private void UpsertScore(string tid, int sid, string subKpiName, int scoreValue, string hodId)
        {
            var mapping = db.EmployeSessionKPI.FirstOrDefault(m =>
                          m.SubKPI.name.Contains(subKpiName) && m.SessionID == sid);

            if (mapping == null) return;

            var existingScore = db.KPIScore.FirstOrDefault(s =>
                                s.empKPIID == mapping.id && s.empID == tid);

            if (existingScore != null)
            {
                existingScore.score = scoreValue;
                existingScore.evaluatorID = hodId;
            }
            else
            {
                db.KPIScore.Add(new KPIScore
                {
                    empKPIID = mapping.id,
                    empID = tid,
                    score = scoreValue,
                    evaluatorID = hodId
                });
            }
        }

        // 4. GET: Teacher Performance/Remarks for Teacher Login
        [HttpGet]
        [Route("my-Courseperformance/{tid}/{sid}")]
        public IHttpActionResult GetTeacherRemarks(string tid, int sid)
        {
            try
            {
                var performance = (from s in db.KPIScore
                                   join m in db.EmployeSessionKPI on s.empKPIID equals m.id
                                   join sub in db.SubKPI on m.SubKPIID equals sub.id
                                   where s.empID == tid && m.SessionID == sid
                                   select new
                                   {
                                       Activity = sub.name,
                                       ObtainedScore = s.score,
                                       Status = s.score == 5 ? "On Time" : "Late",
                                       Remarks = s.score == 5
                                           ? "Excellent! Submitted on time."
                                           : "Delayed submission recorded."
                                   }).ToList();

                if (!performance.Any())
                    return NotFound();

                return Ok(performance);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Fetch performance error: " + ex.Message));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }










        /////////////////////////////////////SOciety APIS////////////////////////////////




        // =========================
        [HttpPost]
        [Route("AddSociety")]
        public IHttpActionResult AddSociety([FromBody] FYP.Models.DTO.SocietyDTO model)
        {
            if (model == null)
                return BadRequest("Invalid data");

            var society = new FYP.Models.Societies
            {
                SocietyName = model.SocietyName,
                Description = model.Description
            };

            db.Societies.Add(society);
            db.SaveChanges();

            // OPTIONAL: if you want session mapping, add here

            return Ok(new { message = "Society added successfully" });
        }
        // =========================
        // 2. GET ALL SOCIETIES
        // =========================
        [HttpGet]
        [Route("GetAll")]
        public IHttpActionResult GetAll()
        {
            var data = db.Societies
                .Select(s => new
                {
                    s.SocietyId,
                    s.SocietyName,
                    s.Description,

                    // Chair count
                    ChairCount = db.SocietyAssignments
                        .Count(a => a.SocietyId == s.SocietyId && a.IsChairperson == true),

                    // Mentor count
                    MentorCount = db.SocietyAssignments
                        .Count(a => a.SocietyId == s.SocietyId && a.IsMentor == true),

                    // Chairperson names
                    Chairpersons = (from a in db.SocietyAssignments
                                    join t in db.Teacher on a.TeacherId equals t.userID
                                    where a.SocietyId == s.SocietyId
                                       && a.IsChairperson == true
                                    select t.name).ToList(),

                    // Mentor names (optional but useful)
                    Mentors = (from a in db.SocietyAssignments
                               join t in db.Teacher on a.TeacherId equals t.userID
                               where a.SocietyId == s.SocietyId
                                  && a.IsMentor == true
                               select t.name).ToList()
                })
                .ToList();

            return Ok(data);
        }

        // =========================
        // 3. GET SOCIETIES BY SESSION
        // =========================
        [HttpGet]
        [Route("GetBySession/{sessionId}")]
        public IHttpActionResult GetBySession(int sessionId)
        {
            var data = (from s in db.Societies
                        join a in db.SocietyAssignments
                        on s.SocietyId equals a.SocietyId
                        where a.SessionId == sessionId
                        select new
                        {
                            s.SocietyId,
                            s.SocietyName,
                            s.Description
                        }).Distinct().ToList();

            return Ok(data);
        }

        // =========================
        // 4. ASSIGN TEACHER (Chair / Mentor)
        // =========================
        [HttpPost]
        [Route("AssignTeacher")]
        public IHttpActionResult AssignTeacher([FromBody] FYP.Models.DTO.SocietyAssignment model)
        {
            if (model == null)
                return BadRequest("Invalid data");

            // =========================================
            // STEP 1: REMOVE ALL OLD CHAIRPERSONS
            // (IMPORTANT: fixes duplicate problem permanently)
            // =========================================
            var oldChairs = db.SocietyAssignments
                .Where(x =>
                    x.SocietyId == model.SocietyId &&
                    x.SessionId == model.SessionId &&
                    x.IsChairperson == true)
                .ToList();

            if (oldChairs.Any())
            {
                db.SocietyAssignments.RemoveRange(oldChairs);
            }

            // =========================================
            // STEP 2: ADD NEW CHAIRPERSON
            // =========================================
            var newChair = new SocietyAssignments
            {
                TeacherId = model.TeacherId,
                SocietyId = model.SocietyId,
                SessionId = model.SessionId,
                IsChairperson = true,
                IsMentor = false
            };

            db.SocietyAssignments.Add(newChair);

            db.SaveChanges();

            return Ok(new { message = "Chairperson updated successfully" });
        }
        // =========================
        // 5. GET ASSIGNMENTS BY SOCIETY
        // =========================
        [HttpGet]
        [Route("GetAssignments/{societyId}")]
        public IHttpActionResult GetAssignments(int societyId)
        {
            var data = db.SocietyAssignments
                .Where(x => x.SocietyId == societyId)
                .ToList();

            return Ok(data);
        }

        // =========================
        // 6. GET CHAIRPERSONS
        // =========================
        [HttpGet]
        [Route("GetChairpersons/{societyId}/{sessionId}")]
        public IHttpActionResult GetChairpersons(int societyId, int sessionId)
        {
            var data = db.SocietyAssignments
                .Where(a => a.SocietyId == societyId
                         && a.SessionId == sessionId
                         && a.IsChairperson == true)
                .Join(db.Teacher,
                      a => a.TeacherId,
                      t => t.userID,
                      (a, t) => new
                      {
                          SocietyId = a.SocietyId,
                          SessionId = a.SessionId,
                          TeacherId = a.TeacherId,
                          TeacherName = t.name
                      })
                .FirstOrDefault(); // 👈 ONLY ONE CHAIRPERSON

            return Ok(data);
        }

        // =========================
        // 7. GET MENTORS
        // =========================
        [HttpGet]
        [Route("GetMentors/{societyId}")]
        public IHttpActionResult GetMentors(int societyId)
        {
            var data = db.SocietyAssignments
                .Where(x => x.SocietyId == societyId && x.IsMentor == true)
                .ToList();

            return Ok(data);
        }



        // =========================
        // UPDATE SOCIETY
        // =========================
        [HttpPut]
        [Route("UpdateSociety/{id}")]
        public IHttpActionResult UpdateSociety(int id, [FromBody] FYP.Models.DTO.SocietyDTO model)
        {
            if (model == null)
                return BadRequest("Invalid data");

            var society = db.Societies.FirstOrDefault(x => x.SocietyId == id);

            if (society == null)
                return NotFound();

            society.SocietyName = model.SocietyName;
            society.Description = model.Description;

            db.SaveChanges();

            return Ok(new { message = "Society updated successfully" });
        }


        // =========================
        // GET ALL TEACHERS
        // =========================
        [HttpGet]
        [Route("GetTeachers")]
        public IHttpActionResult GetTeachers()
        {
            var data = db.Teacher
                .Select(t => new
                {
                    t.userID,
                    t.name,
                    //t.designation
                }).ToList();

            return Ok(data);
        }


        [HttpPost]
        [Route("AssignMentorsBulk")]
        public IHttpActionResult AssignMentorsBulk([FromBody] List<FYP.Models.DTO.SocietyAssignment> models)
        {
            if (models == null || !models.Any())
                return BadRequest("Invalid data");

            foreach (var model in models)
            {
                var exists = db.SocietyAssignments.FirstOrDefault(x =>
                    x.SocietyId == model.SocietyId &&
                    x.SessionId == model.SessionId &&
                    x.TeacherId == model.TeacherId &&
                    x.IsMentor == true);

                if (exists == null)
                {
                    db.SocietyAssignments.Add(new SocietyAssignments
                    {
                        TeacherId = model.TeacherId,
                        SocietyId = model.SocietyId,
                        SessionId = model.SessionId,
                        IsChairperson = false,
                        IsMentor = true
                    });
                }
            }

            db.SaveChanges();

            return Ok(new { message = "Mentors assigned successfully" });
        }
        


      
    }
}
