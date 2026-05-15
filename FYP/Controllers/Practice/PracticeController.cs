using FYP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.UI;

namespace FYP.Controllers.Practice
{
    public class PracticeController : ApiController
    {
        FYPEntities db = new FYPEntities();





                                    //This is the Basic ENdpoint That Uses join to get data and then return To the frontend


        //[HttpGet]
        //[Route("GetEmployeePerformance")]
        //public IHttpActionResult GetEmployeePerformance()
        //{
        //    try
        //    {
        //        var data = (
        //            from eval in db.PeerEvaluation

        //            join emp in db.Teacher
        //            on eval.Teacher equals emp.userID

        //            join dept in db.Department
        //            on eval.DepartmentId equals dept.Id

        //            select new
        //            {
        //                EmployeeId = emp.Id,

        //                EmployeeName = emp.Name,

        //                DepartmentName = dept.DepartmentName,

        //                Score = eval.Score
        //            }

        //        ).ToList();

        //        return Ok(data);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}
    }
}
