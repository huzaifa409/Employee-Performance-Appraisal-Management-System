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
    public class QuestionaireController : ApiController
    {
        FYPEntities db = new FYPEntities();


        // POST: api/Questions
        [HttpPost]
        [Route("api/Questions")]
        public IHttpActionResult CreateQuestion(QuestionCreateDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.QuestionText) || string.IsNullOrEmpty(dto.QuestionareType))
                return BadRequest("Invalid input");

            // Check if Questionare type exists
            var questionare = db.Questionare.FirstOrDefault(q => q.type == dto.QuestionareType);
            if (questionare == null)
            {
                questionare = new Questionare { type = dto.QuestionareType };
                db.Questionare.Add(questionare);
                db.SaveChanges(); // save to get Id
            }

            var question = new Questions
            {
                QuestionText = dto.QuestionText,
                score = dto.Score,
                QuestionareID = questionare.id
            };

            db.Questions.Add(question);
            db.SaveChanges();

            return Ok(new { Message = "Question added successfully", QuestionID = question.QuestionID });
        }

        // GET: api/Questions
        [HttpGet]
        [Route("api/Questions")]
        public IHttpActionResult GetAllQuestions()
        {
            var result = db.Questions
                .Select(q => new
                {
                    q.QuestionID,
                    q.QuestionText,
                    q.score,
                    QuestionareType = q.Questionare.type
                })
                .ToList();

            return Ok(result);
        }

    }
    }