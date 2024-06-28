using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterviewAiFunction.Serializers
{
    internal class InterviewQuestionSerializer
    {
        public int? Id { get; set; }
        public int? InterviewId { get; set; }
        public string? QuestionText { get; set; }
        public int? QuestionOrder { get; set; }
        public bool? IsRequired { get; set; } 
    }
}
