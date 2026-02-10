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
                type = model.EvaluationType ,
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



        [HttpGet]
        [Route("GetAll")]
        public IHttpActionResult GetAll()
        {
            var data = db.Questionare
                .Select(q => new QuestionnaireListDto
                {
                    Id = q.id,
                    Type = q.type,
                    Flag = q.flag,
                    QuestionCount = q.Questions.Count()
                })
                .ToList();

            return Ok(data);
        }



        [HttpPost]
        [Route("Toggle")]
        public IHttpActionResult ToggleQuestionnaire(ToggleQuestionnaireDto model)
        {
            var questionnaire = db.Questionare.Find(model.QuestionnaireId);

            if (questionnaire == null)
                return NotFound();

            if (model.TurnOn)
            {
                // ❌ Check if same type is already ON
                bool alreadyActive = db.Questionare.Any(q =>
                    q.type == questionnaire.type &&
                    q.flag == "1" &&
                    q.id != questionnaire.id
                );

                if (alreadyActive)
                {
                    return BadRequest("Another evaluation of this type is already active.");
                }

                questionnaire.flag = "1";
            }
            else
            {
                questionnaire.flag = "0";
            }

            db.SaveChanges();

            return Ok(new { message = "Status updated successfully" });
        }

    }

}