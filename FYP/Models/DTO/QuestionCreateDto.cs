using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FYP.Models.DTO
{
    public class QuestionCreateDto
    {
        public string QuestionText { get; set; }
        public int Score { get; set; }
        public string QuestionareType { get; set; }
    }
}