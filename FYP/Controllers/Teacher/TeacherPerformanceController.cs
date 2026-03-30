using FYP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace FYP.Controllers.Teacher
{
    [RoutePrefix("api/Performance")]
    public class PerformanceController : ApiController
    {
        FYPEntities db = new FYPEntities();

        //[HttpGet]
        //[Route("GetMyPerformance/{userId}")]
        //public IHttpActionResult GetMyPerformance(string userId)
        //{
        //    try
        //    {
        //        // 1. Get Teacher Details
        //        var teacher = db.Teacher.FirstOrDefault(t => t.userID == userId);
        //        if (teacher == null) return Content(HttpStatusCode.NotFound, "Teacher not found");

        //        // 2. Get the latest session the teacher is enrolled in
        //        var latestEnrollment = db.Enrollment
        //            .Where(e => e.teacherID == userId)
        //            .OrderByDescending(e => e.sessionID)
        //            .FirstOrDefault();

        //        if (latestEnrollment == null)
        //            return Ok(new { Message = "No active sessions found for this teacher." });

        //        int sessionId = latestEnrollment.sessionID ?? 0;
        //        var currentSession = db.Session.FirstOrDefault(s => s.id == sessionId);

        //        // 3. Fetch Active KPIs for this Session
        //        var activeKPIs = db.EmployeSessionKPI
        //            .Where(esk => esk.SessionID == sessionId)
        //            .Select(esk => new
        //            {
        //                esk.id,
        //                esk.KPIID,
        //                esk.SubKPIID,
        //                KPIName = db.KPI.Where(k => k.id == esk.KPIID).Select(k => k.name).FirstOrDefault(),
        //                SubKPIName = db.SubKPI.Where(sk => sk.id == esk.SubKPIID).Select(sk => sk.name).FirstOrDefault()
        //            }).ToList();

        //        if (!activeKPIs.Any())
        //            return Ok(new { Message = "No KPIs configured for this session." });

        //        // 4. Fetch Base Data (Student & Peer) with Null-Safe Averages
        //        var studentData = db.StudentEvaluation.Where(se => se.Enrollment.teacherID == userId && se.Enrollment.sessionID == sessionId);
        //        double studentAvg = studentData.Any() ? studentData.Average(se => (double?)se.score) ?? 0 : 0;

        //        var peerData = db.PeerEvaluation.Where(pe => pe.evaluateeID == userId && pe.PeerEvaluator.sessionID == sessionId);
        //        double peerAvg = peerData.Any() ? peerData.Average(pe => (double?)pe.score) ?? 0 : 0;

        //        var confScores = db.KPIScore.Where(ks => ks.empID == userId && ks.EmployeSessionKPI.SessionID == sessionId).ToList();

        //        // 5. Grouping & Calculations
        //        var groupedKPIs = activeKPIs.GroupBy(k => new { k.KPIID, k.KPIName });
        //        var blockList = new List<object>();
        //        var chartList = new List<object>();
        //        double totalAchieved = 0;
        //        double totalWeight = 0;

        //        foreach (var kpiGroup in groupedKPIs)
        //        {
        //            double kpiGroupAchieved = 0;
        //            double kpiGroupWeight = 0;

        //            foreach (var item in kpiGroup)
        //            {
        //                var weightEntry = db.SessionKPIWeight.FirstOrDefault(w =>
        //                    w.SessionID == sessionId && w.KPIID == item.KPIID && w.SubKPIID == item.SubKPIID);

        //                double weight = weightEntry?.Weight ?? 0;
        //                string subNameLower = (item.SubKPIName ?? "").ToLower();
        //                double multiplier = 0;

        //                if (subNameLower.Contains("student"))
        //                    multiplier = studentAvg;
        //                else if (subNameLower.Contains("peer"))
        //                    multiplier = peerAvg;
        //                else
        //                {
        //                    var specificList = confScores.Where(cs => cs.empKPIID == item.id).ToList();
        //                    multiplier = specificList.Any() ? specificList.Average(cs => (double?)cs.score) ?? 0 : 0;
        //                }

        //                // Formula: (Obtained / MaxScale 4.0) * Weight
        //                double achieved = (multiplier / 4.0) * weight;

        //                kpiGroupAchieved += achieved;
        //                kpiGroupWeight += weight;
        //            }

        //            double kpiPercentage = kpiGroupWeight > 0 ? Math.Round((kpiGroupAchieved / kpiGroupWeight) * 100, 0) : 0;

        //            // Data for the summary blocks/cards
        //            blockList.Add(new
        //            {
        //                Title = kpiGroup.Key.KPIName,
        //                Value = kpiPercentage,
        //                MaxWeight = kpiGroupWeight
        //            });

        //            // Data for the Graph labels
        //            chartList.Add(new
        //            {
        //                value = kpiPercentage,
        //                label = kpiGroup.Key.KPIName,
        //                frontColor = "#1E7F4D"
        //            });

        //            totalAchieved += kpiGroupAchieved;
        //            totalWeight += kpiGroupWeight;
        //        }


        //        if (latestEnrollment == null)
        //            return Ok(new
        //            {
        //                Status = "Empty",
        //                Message = "No active sessions found.",
        //                Blocks = new List<object>(),
        //                ChartData = new List<object>(),
        //                OverallPercentage = 0
        //            });
        //        // Final clean object for React Native
        //        return Ok(new
        //        {
        //            Status = "Success",
        //            TeacherName = teacher.name,
        //            TeacherID = teacher.userID,
        //            SessionName = currentSession?.name ?? "N/A",
        //            OverallPercentage = totalWeight > 0 ? Math.Round((totalAchieved / totalWeight) * 100, 1) : 0,
        //            Blocks = blockList,
        //            ChartData = chartList
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return InternalServerError(ex);
        //    }
        //}






        [HttpGet]
        [Route("GetMyPerformance/{userId}")]
        public IHttpActionResult GetMyPerformance(string userId)
        {
            try
            {
                // 1. Verify Teacher exists
                var teacher = db.Teacher.FirstOrDefault(t => t.userID == userId);
                if (teacher == null)
                {
                    return Ok(new { Status = "Error", Message = "Teacher ID " + userId + " not found." });
                }

                // 2. Get Enrollment (Check for T001 specifically here)
                var latestEnrollment = db.Enrollment
                    .Where(e => e.teacherID == userId)
                    .OrderByDescending(e => e.sessionID)
                    .FirstOrDefault();

                if (latestEnrollment == null)
                {
                    return Ok(new
                    {
                        Status = "NotEnrolled",
                        TeacherName = teacher.name,
                        Message = "This teacher is not enrolled in any session."
                    });
                }

                int sessionId = latestEnrollment.sessionID ?? 0;
                var currentSession = db.Session.FirstOrDefault(s => s.id == sessionId);

                // 3. KPI Lookup - Ensure we don't crash if session has no KPIs
                var activeKPIs = db.EmployeSessionKPI
                    .Where(esk => esk.SessionID == sessionId)
                    .ToList();

                if (!activeKPIs.Any())
                {
                    return Ok(new { Status = "NoKPI", TeacherName = teacher.name, Message = "No KPIs configured." });
                }

                // 4. Safe Evaluation Averages
                var studentData = db.StudentEvaluation.Where(se => se.Enrollment.teacherID == userId && se.Enrollment.sessionID == sessionId);
                double studentAvg = studentData.Any() ? studentData.Average(se => (double?)se.score) ?? 0 : 0;

                var peerData = db.PeerEvaluation.Where(pe => pe.evaluateeID == userId && pe.PeerEvaluator.sessionID == sessionId);
                double peerAvg = peerData.Any() ? peerData.Average(pe => (double?)pe.score) ?? 0 : 0;

                // 5. Build Result Lists Safely
                var blockList = new List<object>();
                var chartList = new List<object>();
                double totalAchieved = 0;
                double totalWeight = 0;

                foreach (var esk in activeKPIs)
                {
                    var kpi = db.KPI.FirstOrDefault(k => k.id == esk.KPIID);
                    var subKpi = db.SubKPI.FirstOrDefault(sk => sk.id == esk.SubKPIID);
                    var weightEntry = db.SessionKPIWeight.FirstOrDefault(w => w.SessionID == sessionId && w.KPIID == esk.KPIID && w.SubKPIID == esk.SubKPIID);

                    double weight = weightEntry?.Weight ?? 0;
                    double score = 0;

                    // Logic to determine score source
                    string subName = subKpi?.name?.ToLower() ?? "";
                    if (subName.Contains("student")) score = studentAvg;
                    else if (subName.Contains("peer")) score = peerAvg;
                    else
                    {
                        var manualScore = db.KPIScore.FirstOrDefault(ks => ks.empID == userId && ks.empKPIID == esk.id);
                        score = (double?)(manualScore?.score) ?? 0;
                    }

                    double achieved = (score / 4.0) * weight;
                    totalAchieved += achieved;
                    totalWeight += weight;

                    blockList.Add(new { Title = kpi?.name ?? "Unknown", Value = weight > 0 ? Math.Round((achieved / weight) * 100, 0) : 0, MaxWeight = weight });
                    chartList.Add(new { value = weight > 0 ? (achieved / weight) * 100 : 0, label = kpi?.name ?? "KPI", frontColor = "#1E7F4D" });
                }

                return Ok(new
                {
                    Status = "Success",
                    TeacherName = teacher.name,
                    SessionName = currentSession?.name ?? "N/A",
                    OverallPercentage = totalWeight > 0 ? Math.Round((totalAchieved / totalWeight) * 100, 1) : 0,
                    Blocks = blockList,
                    ChartData = chartList
                });
            }
            catch (Exception ex)
            {
                // This ensures your app gets JSON even if the code fails
                return Ok(new { Status = "Exception", Message = ex.Message, Inner = ex.InnerException?.Message });
            }
        }
    }
}
