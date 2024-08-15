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

        public bool IsValidInvitationForInterview(InterviewInvitation invitation, string interviewUuid)
        {
            return _context.Interview.Any(x=>x.Uuid==interviewUuid && x.Id==invitation.InterviewId);
        }

        public bool IsValidAdminUserForInterview(int interviewId, string email)
        {
            return _context.Interview.Any(x => x.CreatedBy == email && x.Id == interviewId);
        }

        public bool IsUserInvitation(InterviewInvitation invitation, string userEmail)
        {
            return _context.Interview.Any(x=>x.Id == invitation.InterviewId && x.CreatedBy == userEmail.ToLower());
        }

        public bool IsValidInvitationForSession(InterviewInvitation invitation, int sessionId)
        {
            return _context.InterviewSession.Any(x=> x.Id==sessionId && x.SessionUser.ToLower()==invitation.Email.ToLower());
        }

        public bool IsValidInvitationForResponse(InterviewInvitation invitation, int responseId)
        {
            return _context.InterviewResponse.Any(x => x.Id == responseId && IsValidInvitationForSession(invitation, x.SessionId));
        }

        public bool IsValidInvitationForResult(InterviewInvitation invitation, int resultId)
        {
            return _context.InterviewResult.Any(x => x.Id == resultId && IsValidInvitationForSession(invitation, x.SessionId));
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
