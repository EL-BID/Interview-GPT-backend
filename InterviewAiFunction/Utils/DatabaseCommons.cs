using Azure;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

        public bool IsValidAdminUserForInterviewUuid(string uuuid, string email)
        {
            return _context.Interview.Any(x=>x.CreatedBy==email && x.Uuid==uuuid);
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
            InterviewResponse response = _context.InterviewResponse.FirstOrDefault(x => x.Id == responseId);
            if (response == null)
            {
                return false;
            }
            else
            {
                return IsValidInvitationForSession(invitation, response.SessionId);
            }
            
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

        public bool IsValidUserForSession(int sessionId, string email)
        {
            return _context.InterviewSession.Any(x => x.Id == sessionId && x.SessionUser == email);
        }

        public bool IsValidUserForResult(InterviewResult result, string email)
        {
            return _context.InterviewSession.Any(x=>x.Id==result.SessionId && x.SessionUser == email);
        }

        public bool IsValidUserForInterview(int interviewId, string email)
        {
            return _context.InterviewSession.Any(x=>x.InterviewId==interviewId && x.SessionUser==email);
        }

        public bool IsValidUserForResponse(int responseId, string email)
        {
            InterviewResponse response = _context.InterviewResponse.FirstOrDefault(x => x.Id == responseId);
            if (response != null)
            {
                return _context.InterviewSession.Any(x => x.SessionUser == email && x.Id == response.SessionId);
            }
            else
            {
                return false;
            }
        }

        public bool IsValidUserForInterviewUuid(string uuid, string email)
        {
            Interview interview = _context.Interview.FirstOrDefault(x => x.Uuid == uuid);
            if(interview == null)
            {
                return false;
            }
            else
            {
                return _context.InterviewSession.Any(x=>x.InterviewId==interview.Id && x.SessionUser== email);
            }
        }


        public HttpResponseData ProcessDbException(HttpRequestData req, Exception ex)
        {
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            if (ex is DbUpdateException)
            {
                var sqlException = ex.GetBaseException() as SqlException;
                if (sqlException != null)
                {
                    var number = sqlException.Number;
                    if (number == 547)
                    {
                        response.WriteStringAsync("You must delete all related data before deleting this record.");
                    }
                    else
                    {
                        response.WriteStringAsync(sqlException.Message);
                    }
                }                
            }
            return response;
        }
    }
}
