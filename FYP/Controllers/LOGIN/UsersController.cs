using FYP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Routing;
using System.Web.Services.Description;
namespace FYP.Controllers.LOGIN
{


    [RoutePrefix("api/Users")]
    public class UsersController : ApiController
    {
        
        FYPEntities db = new FYPEntities();
        
        [Route("Login")]
        [HttpPost]
        public HttpResponseMessage Login(string id,string password)
        {
            var res = db.Users.FirstOrDefault(x => x.id == id && x.password == password && x.isActive == 1);
            if (res == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, "No User Found");

            }

            return Request.CreateResponse(HttpStatusCode.OK, new {message = "LoginSuccessful", role =res.role,userId = res.id});

        }
    }
}
