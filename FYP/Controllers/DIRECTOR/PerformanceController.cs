using FYP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using FYP.Models.DTO;

namespace FYP.Controllers.DIRECTOR
{
    [RoutePrefix("api/Performance")]
    public class PerformanceController : ApiController
    {
        FYPEntities db = new FYPEntities();

        // ✅ 1. Get All Sessions (Dropdown)
        [HttpGet]
        [Route("GetSessions")]
        public IHttpActionResult GetSessions()
        {
            var sessions = db.Session
                .Select(s => new
                {
                    id = s.id,
                    name = s.name
                }).ToList();

            return Ok(sessions);
        }

        // ✅ 2. Get Employee Types (Tabs: Teacher, Admin, etc.)
        [HttpGet]
        [Route("GetEmployeeTypes")]
        public IHttpActionResult GetEmployeeTypes()
        {
            var types = db.EmployeeType
                .Select(e => new
                {
                    id = e.id,
                    type = e.type
                }).ToList();

            return Ok(types);
        }

        // ✅ 3. Get Courses based on Session
        [HttpGet]
        [Route("GetCoursesBySession")]
        public IHttpActionResult GetCoursesBySession(int sessionId)
        {
            var courses = db.Enrollment
                .Where(e => e.sessionID == sessionId)
                .Select(e => e.courseCode)
                .Distinct()
                .ToList();

            return Ok(courses);
        }

        [HttpGet]
        [Route("GetTeacherPerformance")]
        public IHttpActionResult GetTeacherPerformance(int sessionId, string department = null, string courseCode = null)
        {
            var query = db.Enrollment.Where(e => e.sessionID == sessionId);

            if (!string.IsNullOrEmpty(courseCode) && courseCode != "All")
            {
                query = query.Where(e => e.courseCode == courseCode);
            }

            var data = query
                .GroupBy(e => new { e.teacherID, e.courseCode })
                .Select(g => new
                {
                    TeacherID = g.Key.teacherID,
                    CourseCode = g.Key.courseCode,

                    TeacherName = db.Teacher
                        .Where(t => t.userID == g.Key.teacherID)
                        .Select(t => t.name)
                        .FirstOrDefault(),

                    Department = db.Teacher
                        .Where(t => t.userID == g.Key.teacherID)
                        .Select(t => t.department)
                        .FirstOrDefault(),

                    // ✅ Peer Evaluation Avg
                    PeerAvg = db.PeerEvaluation
                        .Where(p =>
                            p.evaluateeID == g.Key.teacherID &&
                            p.courseCode == g.Key.courseCode &&
                            p.SessionID == sessionId
                        )
                        .Average(p => (int?)p.score),

                    // ✅ Student Evaluation Avg (FIXED JOIN)
                    StudentAvg = db.StudentEvaluation
                        .Where(s =>
                            s.SessionID == sessionId &&
                            s.Enrollment.teacherID == g.Key.teacherID &&
                            s.Enrollment.courseCode == g.Key.courseCode
                        )
                        .Average(s => (int?)s.score)
                })
                .ToList();

            // ✅ APPLY DEPARTMENT FILTER
            if (!string.IsNullOrEmpty(department))
            {
                data = data.Where(d => d.Department == department).ToList();
            }

            // ✅ FINAL RESULT WITH COMBINED AVERAGE
            var result = data.Select(x =>
            {
                var peer = x.PeerAvg ?? 0;
                var student = x.StudentAvg ?? 0;

                int count = 0;
                if (x.PeerAvg != null) count++;
                if (x.StudentAvg != null) count++;

                var finalAvg = count > 0 ? (peer + student) / count : 0;

                return new
                {
                    x.TeacherID,
                    x.TeacherName,
                    x.CourseCode,
                    x.Department,
                    Percentage = (finalAvg / 4.0) * 100
                };
            });

            return Ok(result);
        }

        [HttpGet]
        [Route("GetAllCourses")]
        public IHttpActionResult GetAllCourses()
        {
            var courses = db.Course
                .Select(c => c.code)
                .Distinct()
                .ToList();

            return Ok(courses);
        }




        [HttpGet]
        [Route("GetTeachersByCourse")]
        public IHttpActionResult GetTeachersByCourse(string courseCode)
        {
            if (string.IsNullOrEmpty(courseCode))
                return Ok(new List<object>());

            courseCode = courseCode.Trim().ToUpper(); // normalize

            // Get latest session ID for this course
            var latestSessionId = db.Enrollment
                .Where(e => e.courseCode.ToUpper().Trim() == courseCode)
                .OrderByDescending(e => e.sessionID)
                .Select(e => e.sessionID)
                .FirstOrDefault();

            var teachers = db.Enrollment
                .Where(e => e.courseCode.ToUpper().Trim() == courseCode && e.sessionID == latestSessionId)
                .Select(e => new
                {
                    id = e.teacherID,
                    name = db.Teacher
                                .Where(t => t.userID == e.teacherID)
                                .Select(t => t.name)
                                .FirstOrDefault()
                })
                .Distinct()
                .ToList();

            return Ok(teachers);
        }



        [HttpGet]
        [Route("GetAllTeachers")]
        public IHttpActionResult GetAllTeachers()
        {
            var teachers = db.Enrollment
                .Select(e => new
                {
                    id = e.teacherID,
                    name = db.Teacher
                                .Where(t => t.userID == e.teacherID)
                                .Select(t => t.name)
                                .FirstOrDefault()
                })
                .Distinct()
                .ToList();

            return Ok(teachers);
        }


        [HttpPost]
        [Route("CompareTeachers")]
        public IHttpActionResult CompareTeachers(CompareDTO dto)
        {
            var result = new List<object>();

            if (dto.mode == "course")
            {
                // ✅ Get latest session for this course
                var latestSessionId = db.Enrollment
                    .Where(e => e.courseCode == dto.courseCode)
                    .OrderByDescending(e => e.sessionID)
                    .Select(e => e.sessionID)
                    .FirstOrDefault();

                result.Add(GetTeacherScore(dto.teacherA, dto.courseCode, latestSessionId));
                result.Add(GetTeacherScore(dto.teacherB, dto.courseCode, latestSessionId));
            }
            else
            {
                result.Add(GetTeacherScore(dto.teacherA, null, dto.session1));
                result.Add(GetTeacherScore(dto.teacherA, null, dto.session2));
            }

            return Ok(result);
        }

        private object GetTeacherScore(string teacherId, string courseCode, int? sessionId)
        {
            var peer = db.PeerEvaluation
                .Where(p => p.evaluateeID == teacherId &&
                       (courseCode == null || p.courseCode.ToUpper().Trim() == courseCode.ToUpper().Trim()) &&
                       (sessionId == null || p.SessionID == sessionId))
                .Average(p => (double?)p.score) ?? 0;

            var student = db.StudentEvaluation
                .Where(s =>
                    (courseCode == null || s.Enrollment.courseCode.ToUpper().Trim() == courseCode.ToUpper().Trim()) &&
                    (sessionId == null || s.SessionID == sessionId) &&
                    s.Enrollment.teacherID == teacherId
                )
                .Average(s => (double?)s.score) ?? 0;

            var finalScore = (peer + student) / 2.0;

            var name = db.Teacher
                .Where(t => t.userID == teacherId)
                .Select(t => t.name)
                .FirstOrDefault();

            return new
            {
                Name = name,
                Peer = Math.Round(peer, 2),        // ✅ REQUIRED
                Student = Math.Round(student, 2),  // ✅ REQUIRED
                Final = Math.Round(finalScore, 2),
                Percentage = Math.Round((finalScore / 4.0) * 100, 2)
            };
        }
    }
    }