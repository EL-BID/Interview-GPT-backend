using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterviewAiFunction.Utils
{
    public class DatabaseCommons
    {
        private readonly InterviewContext _context;

        public DatabaseCommons(InterviewContext context)
        {
            _context = context;
        }

        public bool IsUserQuestion(InterviewQuestion question, string userEmail)
        {
            int interviewId = question.InterviewId;
            return _context.Interview.Any(x => x.Id == interviewId && x.CreatedBy == userEmail.ToLower());
        }

        public bool IsUserInterviewId(int interviewId, string userEmail)
        {
            return _context.Interview.Any(x=>x.Id==interviewId && x.CreatedBy==userEmail.ToLower());
        }

        public bool IsUserInvitation(InterviewInvitation invitation, string userEmail)
        {
            return _context.Interview.Any(x=>x.Id == invitation.InterviewId && x.CreatedBy == userEmail.ToLower());
        }
    }
}
