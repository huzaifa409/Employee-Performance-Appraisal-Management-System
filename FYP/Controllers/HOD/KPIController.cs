using FYP.Models;
using FYP.Models.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Transactions;

namespace FYP.Controllers.HOD
{
    [RoutePrefix("api/Kpi")]

    public class KPIController : ApiController
    {
        FYPEntities db = new FYPEntities();


        [HttpGet]
        [Route("getemployeetype")]
        public HttpResponseMessage GetEmployeeType()
        {
            var res=db.EmployeeType.ToList();

            if(res.Count == 0 )
            {
                return Request.CreateResponse(HttpStatusCode.NoContent,"No Employee Type Found");
            }
            return Request.CreateResponse(HttpStatusCode.OK,res);

        }



        [HttpPost]
        [Route("create-with-weight")]
        public IHttpActionResult CreateWithWeight(AddKpiDto dto)
        {
            if (dto == null || dto.SubKPIs == null || dto.SubKPIs.Count == 0)
                return BadRequest("Data incomplete.");

            // 1. User ne jo Main KPI ka weight allot kiya (e.g., 30)
            // Front-end se 'RequestedKPIWeight' aa raha hai
            decimal mainKpiTargetWeight = (decimal)dto.RequestedKPIWeight;

            // 2. Sub-KPIs ka total input jo user ne list mein dala (e.g., 40)
            decimal subKpiTotalInput = dto.SubKPIs.Sum(s => (decimal)s.Weight);

            if (mainKpiTargetWeight >= 100)
                return BadRequest("Main KPI weight 100 se kam hona chahiye.");

            try
            {
                using (var scope = new TransactionScope())
                {
                    // 3. Create Main KPI
                    KPI kpi = new KPI { name = dto.KPIName, KPI_Employeetype = dto.EmployeeTypeId };
                    db.KPI.Add(kpi);
                    db.SaveChanges();

                    // 4. Sub-KPI Adjustment Factor Calculate karein
                    // Agar Target 30 hai aur Input 40, toh factor = 0.75
                    decimal subFactor = subKpiTotalInput > 0 ? mainKpiTargetWeight / subKpiTotalInput : 0;

                    // 5. Sub-KPIs ko "Scaled" weight ke sath save karein
                    foreach (var subDto in dto.SubKPIs)
                    {
                        var subObj = new SubKPI { KPIID = kpi.id, name = subDto.Name };
                        db.SubKPI.Add(subObj);
                        db.SaveChanges();

                        // Har Sub-KPI ka weight ab 30% ke andar fit ho jayega
                        decimal adjustedSubWeight = (decimal)subDto.Weight * subFactor;

                        db.SessionKPIWeight.Add(new SessionKPIWeight
                        {
                            SessionID = dto.SessionId,
                            KPIID = kpi.id,
                            SubKPIID = subObj.id,
                            Weight = (int)Math.Round(adjustedSubWeight, MidpointRounding.AwayFromZero)
                        });
                    }
                    db.SaveChanges();

                    // 6. Rounding Correction for Sub-KPIs (Check if sum is exactly 30)
                    var currentKpiWeights = db.SessionKPIWeight
                        .Where(w => w.SessionID == dto.SessionId && w.KPIID == kpi.id).ToList();
                    int currentKpiSum = currentKpiWeights.Sum(w => w.Weight ?? 0);
                    if (currentKpiSum != (int)mainKpiTargetWeight && currentKpiWeights.Any())
                    {
                        int diff = (int)mainKpiTargetWeight - currentKpiSum;
                        currentKpiWeights.First().Weight += diff;
                        db.SaveChanges();
                    }

                    // 7. GLOBAL ADJUSTMENT (Purani KPIs ko 100% rule ke liye adjust karein)
                    var existingWeights = db.SessionKPIWeight
                        .Where(w => w.SessionID == dto.SessionId &&
                                    w.KPIID != kpi.id &&
                                    db.KPI.Any(k => k.id == w.KPIID && k.KPI_Employeetype == dto.EmployeeTypeId))
                        .ToList();

                    if (existingWeights.Any())
                    {
                        decimal currentOldTotal = existingWeights.Sum(w => (decimal)(w.Weight ?? 0));
                        decimal targetForOld = 100m - mainKpiTargetWeight;

                        if (currentOldTotal > 0)
                        {
                            decimal globalFactor = targetForOld / currentOldTotal;
                            foreach (var w in existingWeights)
                            {
                                decimal adjusted = (decimal)(w.Weight ?? 0) * globalFactor;
                                w.Weight = (int)Math.Round(adjusted, MidpointRounding.AwayFromZero);
                            }
                            db.SaveChanges();
                        }
                    }

                    // 8. FINAL GLOBAL PRECISION CHECK (Total must be exactly 100)
                    var allWeights = db.SessionKPIWeight
                        .Where(w => w.SessionID == dto.SessionId &&
                                    db.KPI.Any(k => k.id == w.KPIID && k.KPI_Employeetype == dto.EmployeeTypeId))
                        .ToList();

                    int finalSum = allWeights.Sum(w => w.Weight ?? 0);
                    if (finalSum != 100 && existingWeights.Any())
                    {
                        int diff = 100 - finalSum;
                        existingWeights.First().Weight += diff;
                        db.SaveChanges();
                    }

                    scope.Complete();
                    return Ok(new { Message = "KPI Saved. Sub-KPIs scaled to fit KPI weight.", Status = "Success" });
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error: " + ex.Message));
            }
        }




        [HttpGet]
        [Route("view-weights/{sid}/{eid}")]
        public IHttpActionResult GetWeights(int sid, int eid)
        {
            try
            {
                var res = db.KPI
                    .Where(k => k.KPI_Employeetype == eid)
                    .ToList() // Memory mein laa kar mapping karein
                    .Select(k => new {
                        kpiId = k.id,
                        kpiName = k.name,
                        // Is KPI ke andar jitne sub-kpis hain unka total weight calculate karein
                        totalKpiWeight = db.SessionKPIWeight
                                         .Where(w => w.SessionID == sid && w.KPIID == k.id)
                                         .Sum(w => (int?)w.Weight) ?? 0,

                        subKpis = (from w in db.SessionKPIWeight
                                   join s in db.SubKPI on w.SubKPIID equals s.id
                                   where w.SessionID == sid && w.KPIID == k.id
                                   select new
                                   {
                                       subKpiId = s.id,
                                       subKpiName = s.name,
                                       weight = w.Weight
                                   }).ToList()
                    })
                    .Where(x => x.totalKpiWeight > 0) // Sirf wo dikhayein jinka weight set hai
                    .ToList();

                return Ok(res);
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }
        [HttpGet]
        [Route("sessions")]
        public IHttpActionResult GetSessions() => Ok(db.Session.Select(s => new { s.id, s.name }).ToList());



        [HttpGet]
        [Route("emptypes")]
        public IHttpActionResult GetEmpTypes() => Ok(db.EmployeeType.Select(e => new { e.id, e.type }).ToList());







    }
}
