using System;
using System.Linq;
using System.Web.Http;
using FYP.Models;

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

                    AvgScore = db.PeerEvaluation
                        .Where(p =>
                            p.evaluateeID == g.Key.teacherID &&
                            p.courseCode == g.Key.courseCode &&
                            p.SessionID == sessionId
                        )
                        .Average(p => (int?)p.score) ?? 0
                })
                .ToList();

            // ✅ APPLY DEPARTMENT FILTER
            if (!string.IsNullOrEmpty(department))
            {
                data = data.Where(d => d.Department == department).ToList();
            }

            var result = data.Select(x => new
            {
                x.TeacherID,
                x.TeacherName,
                x.CourseCode,
                x.Department,
                Percentage = (x.AvgScore / 4.0) * 100
            });

            return Ok(result);
        }
    }
    }