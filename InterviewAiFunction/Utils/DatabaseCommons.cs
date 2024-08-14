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

        public bool IsResponseInvitation(InterviewInvitation invitation, int responseId)
        {
            return _context.InterviewResponse.Any(x => x.Id == responseId && x.InterviewInvitationId == invitation.Id);
        }
        public bool IsResultInvitation(InterviewInvitation invitation, int resultId)
        {
            return _context.InterviewResult.Any(x => x.Id == resultId && x.InterviewInvitationId == invitation.Id);
        }
        public bool IsInterviewQuestionforInvitation(int interviewQuestionId, InterviewInvitation invitation)
        {
            try
            {
                InterviewQuestion iq = _context.InterviewQuestion.Find(interviewQuestionId);
                if (iq != null)
                {
                    return _context.InterviewInvitation.Any(x => x.InterviewId == iq.InterviewId && x.Id == invitation.Id);
                }
                else
                {
                    return false;
                }
            }catch(Exception ex)
            {
                return false;
            }
        }
    }
}
