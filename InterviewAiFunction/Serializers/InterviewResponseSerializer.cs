using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterviewAiFunction.Serializers
{
    internal class InterviewResponseSerializer
    {
        public int? Id { get; set; }
        public int? InterviewQuestionId { get; set; }
        public int? InterviewInviteId { get; set; }
        public string? ResponseText { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
