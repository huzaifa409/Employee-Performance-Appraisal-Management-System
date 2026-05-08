using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FYP.Models.DTO
{
    public class QuestionCreateDto
    {
        public string EvaluationType { get; set; }
        public List<QuestionItemDto> Questions { get; set; }
    }

    public class QuestionItemDto
    {
        public int Id { get; set; }
        public string QuestionText { get; set; }

        public bool isCritical { get; set; }
    }
}