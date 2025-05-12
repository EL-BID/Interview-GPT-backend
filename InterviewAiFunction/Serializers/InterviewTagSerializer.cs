using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterviewAiFunction.Serializers
{
    internal class InterviewTagSerializer
    {
        public int? Id {  get; set; }
        public int? InterviewId { get; set; }
        public string? InterviewUuid { get; set; }
        public string? Label { get; set; }
        public List<string>? Labels { get; set; }

    }
}
