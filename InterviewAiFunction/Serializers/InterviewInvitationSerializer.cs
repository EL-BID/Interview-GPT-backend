using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterviewAiFunction.Serializers
{
    internal class InterviewInvitationSerializer
    {
        public int? Id { get; set; }
        public int? InterviewId { get; set; }
        public string? Email { get; set; }
        public string? InvitationCode { get; set; }
        public string? InvitationStatus { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
