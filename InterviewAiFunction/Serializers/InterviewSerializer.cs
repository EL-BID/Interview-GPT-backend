using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterviewAiFunction.Serializers
{
    internal class InterviewSerializer
    {
        public string? Title {  get; set; }
        public string? Description { get; set; }
        public string? Prompt { get; set; }
        public string? Model { get; set; }
        public string? Uuid { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? CreatedBy { get; set; }
        public List<InterviewQuestionSerializer>? Questions { get; set; }
    }
}
