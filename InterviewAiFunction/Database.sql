
CREATE TABLE Interview(
  Id INT IDENTITY PRIMARY KEY,
  Title VARCHAR(255) NOT NULL,
  Description VARCHAR(255) NULL,
  CreatedAt DATETIME NOT NULL,
  CreatedBy VARCHAR(255) NOT NULL,
  Prompt VARCHAR(8000) NULL, 
  Model VARCHAR(100) NULL,
  Uuid VARCHAR(36) NOT NULL,
  Status VARCHAR(50) NOT NULL DEFAULT 'inactive',
  /* TODO VALIDATE status: active, inactive*/ 
  AuthOnly BIT NOT NULL DEFAULT 0, -- requires authentication
  InvitationOnly BIT NOT NULL DEFAULT 0,  -- requires an invitation
  WelcomeTitle VARCHAR(255) NULL,
  WelcomeMessage VARCHAR(8000) NULL,
  CompletedTitle VARCHAR(255) NULL,
  CompletedMessage VARCHAR(8000) NULL,
  ChatMode BIT NOT NULL DEFAULT 0,
  IsDeleted BIT NOT NULL DEFAULT 0  
)

CREATE TABLE InterviewQuestion(
  Id INT IDENTITY PRIMARY KEY,
  InterviewId INT NOT NULL FOREIGN KEY REFERENCES Interview(Id),
  QuestionText VARCHAR(255) NOT NULL,
  QuestionOrder INT NOT NULL DEFAULT 0,
  IsRequired BIT NOT NULL DEFAULT 1,
  Context VARCHAR(8000) NULL   
)

CREATE TABLE InterviewInvitation(
  Id INT IDENTITY PRIMARY KEY,
  InterviewId INT NOT NULL FOREIGN KEY REFERENCES Interview(Id),
  Email VARCHAR(255) NOT NULL,
  InvitationCode VARCHAR(255) NOT NULL,
  InvitationStatus VARCHAR(255) NOT NULL,
  CreatedAt DATETIME NOT NULL
  --TODO: validate status:
  /* active, inactive*/
)

CREATE TABLE InterviewSession(
  Id INT IDENTITY PRIMARY KEY,
  InterviewId INT NOT NULL FOREIGN KEY REFERENCES Interview(Id),
  Title VARCHAR(255) NULL,
  SessionUser VARCHAR(255) NULL,
  Result VARCHAR(8000) NULL, -- Final output of the interview session.
  Status VARCHAR(100) NOT NULL DEFAULT '',
  CreatedAt DATETIME NOT NULL,
  UpdatedAt DATETIME NULL,
  UserRating INT NULL, --TODO: validate number between 0-10
  CustomInstructions VARCHAR(8000) NULL -- Tells the model aditional information on how to create results.
  --TODO: validate status:
  /* active, completed*/  
)

CREATE TABLE InterviewResponse(
  Id INT IDENTITY PRIMARY KEY,
  InterviewQuestionId INT NOT NULL FOREIGN KEY REFERENCES InterviewQuestion(Id),
  SessionId INT NOT NULL FOREIGN KEY REFERENCES InterviewSession(Id),
  ResponseText VARCHAR(8000) NOT NULL, -- Stores user response to a interview question.
  CreatedAt DATETIME NOT NULL,
  UpdatedAt DATETIME NOT NULL
)

CREATE TABLE InterviewResult(
  Id INT IDENTITY PRIMARY KEY,
  SessionId INT NOT NULL FOREIGN KEY REFERENCES InterviewSession(Id),
  ResultAi VARCHAR(8000) NULL, -- Stores the AI assisted response
  CreatedAt DATETIME NOT NULL,
  UpdatedAt DATETIME NOT NULL
)

CREATE TABLE InterviewUserInstruction(
  Id INT IDENTITY PRIMARY KEY,
  SessionId INT NOT NULL FOREIGN KEY REFERENCES InterviewSession(Id),
  Instruction VARCHAR(8000), -- Tells the AI model how to modify the initial output result.
  CreatedAt DATETIME NOT NULL
)

CREATE TABLE InterviewTag(
  Id INT IDENTITY PRIMARY KEY,
  InterviewId INT NOT NULL FOREIGN KEY REFERENCES Interview(Id),
  [Label] VARCHAR(255) NOT NULL    
)
