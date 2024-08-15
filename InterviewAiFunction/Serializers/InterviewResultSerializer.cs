using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterviewAiFunction.Serializers
{
    internal class InterviewResultSerializer
    {
        public int? Id { get; set; }
        public int SessionId { get; set; }
        public string? ResultUser { get; set; }
        public string? ResultAi { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? InvitationCode { get; set; }
    }
}
