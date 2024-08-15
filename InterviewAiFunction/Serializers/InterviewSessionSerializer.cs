using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterviewAiFunction.Serializers
{
    internal class InterviewSessionSerializer
    {
        public int? Id { get; set; }
        public int? InterviewId { get; set; }
        public string? Title { get; set; }
        public string? Status { get; set; }
        public string? SessionUser { get; set; }
        public string? Result { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? InvitationCode { get; set; }
    }
}
