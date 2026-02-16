using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FYP.Models.DTO
{
    public class StudentEvalatuationDto
    {
      
            public int EnrollmentId { get; set; }
            public List<AnswerDto> Answers { get; set; }
    }
        public class AnswerDto
        {
            public int QuestionId { get; set; }
            public int Score { get; set; }
        }
    
}