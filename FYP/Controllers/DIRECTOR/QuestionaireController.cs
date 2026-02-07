using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using FYP.Models;
using FYP.Models.DTO;

namespace FYP.Controllers.DIRECTOR
{
    [RoutePrefix("api/Questionnaire")]
    public class QuestionnaireController : ApiController
    {
        FYPEntities db = new FYPEntities();

        [HttpPost]
        [Route("Create")]
        public IHttpActionResult CreateQuestionnaire(QuestionCreateDto model)
        {
            if (model == null || model.Questions == null || model.Questions.Count == 0)
            {
                return BadRequest("Invalid data");
            }

            // 1️⃣ Create Questionnaire
            var questionnaire = new Questionare
            {
                type = model.EvaluationType ,// OR map to text if needed
                flag = "0" // DEFAULT — DO NOT CHANGE
            };

            db.Questionare.Add(questionnaire);
            db.SaveChanges(); // 🔥 ID generated here

            // 2️⃣ Insert Questions
            foreach (var q in model.Questions)
            {
                var question = new Questions
                {
                    QuestionareID = questionnaire.id,
                    QuestionText = q
                };

                db.Questions.Add(question);
            }

            db.SaveChanges();

            return Ok(new
            {
                message = "Questionnaire saved successfully",
                QuestionnaireId = questionnaire.id
            });
        }
    }

}