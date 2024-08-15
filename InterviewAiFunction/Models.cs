using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.SqlServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace InterviewAiFunction
{
    internal class Models
    {
    }

    public class InterviewContext: DbContext
    {
        public InterviewContext(DbContextOptions<InterviewContext> options): base(options) 
        {
          
        }

        public DbSet<Interview> Interview { get; set; }
        public DbSet<InterviewSession> InterviewSession { get; set; }
        public DbSet<InterviewQuestion> InterviewQuestion { get; set; }
        public DbSet<InterviewInvitation> InterviewInvitation { get; set; }
        public DbSet<InterviewResult> InterviewResult { get; set; }
        public DbSet<InterviewResponse> InterviewResponse { get; set; }

    }

    public class Interview
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public required string CreatedBy { get; set; }
        public string? Prompt { get; set; } 
        public string? Model { get; set; }
        public required string Uuid {  get; set; }
        public required string Status { get; set; }
        public bool AuthOnly { get; set; }
        public virtual List<InterviewQuestion>? Questions { get; set; }
        public virtual List<InterviewInvitation>? Invitations { get; set; }

    }

    public class InterviewQuestion
    {
        public int Id { get; set; }
        public int InterviewId { get; set; }
        public string? QuestionText { get; set; }
        public int? QuestionOrder { get; set; }
        public bool? IsRequired { get; set; }
    }

    public class InterviewInvitation
    {
        public int Id { get; set; }
        public int InterviewId { get; set; }
        public string Email { get; set; }
        public string InvitationCode { get; set; }
        public string? InvitationStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public virtual List<InterviewResult>? Results { get; set; }
        public virtual List<InterviewResponse>? Responses { get; set; }
    }

    public class InterviewSession
    {
        public int Id { get; set; }
        public int InterviewId { get; set; }
        public string? Title { get; set; }
        public string SessionUser { get; set; }
        public string? Result { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }


    }
    public class InterviewResult
    {
        public int Id { get; set; }
        public int SessionId { get; set;}
        public string? ResultAi { get; set; }
        public DateTime? CreatedAt { get; set;}
        public DateTime? UpdatedAt { get; set; }

    }

    public class InterviewResponse
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public int InterviewQuestionId { get; set; }
        public string ResponseText { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
