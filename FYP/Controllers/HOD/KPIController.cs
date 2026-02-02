using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using FYP.Models;

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



    }
}
