CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "CreditPackages" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Credits" integer NOT NULL,
    "Price" numeric(10,2) NOT NULL,
    "BonusPercentage" integer,
    "Badge" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CreditPackages" PRIMARY KEY ("Id")
);

CREATE TABLE "MentorRoles" (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "DisplayName" character varying(100) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_MentorRoles" PRIMARY KEY ("Id")
);

CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "Email" character varying(255) NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Avatar" text,
    "FocusAreas" text NOT NULL,
    "Credits" integer NOT NULL DEFAULT 10,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

CREATE TABLE "CreditTransactions" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Type" integer NOT NULL,
    "Amount" integer NOT NULL,
    "BalanceAfter" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CreditTransactions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CreditTransactions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Mentors" (
    "Id" uuid NOT NULL,
    "Name" character varying(255) NOT NULL,
    "PublicBio" text NOT NULL,
    "ExpertisePrompt" text NOT NULL,
    "ExpertiseTags" text NOT NULL,
    "Level" integer NOT NULL DEFAULT 1,
    "RoleId" uuid NOT NULL,
    "FollowerCount" integer NOT NULL DEFAULT 0,
    "InsightCount" integer NOT NULL DEFAULT 0,
    "CreatedBy" uuid NOT NULL,
    "Avatar" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Mentors" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Mentors_MentorRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "MentorRoles" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Mentors_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Actors" (
    "Id" uuid NOT NULL,
    "Type" integer NOT NULL,
    "UserId" uuid,
    "MentorId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Actors" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Actors_Mentors_MentorId" FOREIGN KEY ("MentorId") REFERENCES "Mentors" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Actors_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Conversations" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "MentorId" uuid NOT NULL,
    "LastMessage" text NOT NULL,
    "LastMessageAt" timestamp with time zone NOT NULL,
    "UserUnreadCount" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Conversations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Conversations_Mentors_MentorId" FOREIGN KEY ("MentorId") REFERENCES "Mentors" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Conversations_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Insights" (
    "Id" uuid NOT NULL,
    "MentorId" uuid NOT NULL,
    "Content" text NOT NULL,
    "Quote" character varying(280),
    "Tags" text NOT NULL,
    "LikeCount" integer NOT NULL DEFAULT 0,
    "CommentCount" integer NOT NULL DEFAULT 0,
    "HasMedia" boolean NOT NULL,
    "MediaUrl" text,
    "EditedAt" timestamp with time zone,
    "IsEdited" boolean NOT NULL,
    "Type" integer NOT NULL,
    "MasterclassPostId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Insights" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Insights_Mentors_MentorId" FOREIGN KEY ("MentorId") REFERENCES "Mentors" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "MentorAutomations" (
    "MentorId" uuid NOT NULL,
    "Enabled" boolean NOT NULL,
    "Cadence" character varying(50) NOT NULL,
    "Timezone" character varying(50) NOT NULL,
    "NextPostAt" timestamp with time zone,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_MentorAutomations" PRIMARY KEY ("MentorId"),
    CONSTRAINT "FK_MentorAutomations_Mentors_MentorId" FOREIGN KEY ("MentorId") REFERENCES "Mentors" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "UserFollowsMentor" (
    "UserId" uuid NOT NULL,
    "MentorId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_UserFollowsMentor" PRIMARY KEY ("UserId", "MentorId"),
    CONSTRAINT "FK_UserFollowsMentor_Mentors_MentorId" FOREIGN KEY ("MentorId") REFERENCES "Mentors" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_UserFollowsMentor_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Messages" (
    "Id" uuid NOT NULL,
    "ConversationId" uuid NOT NULL,
    "SenderActorId" uuid NOT NULL,
    "Content" text NOT NULL,
    "EditedAt" timestamp with time zone,
    "IsEdited" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Messages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Messages_Actors_SenderActorId" FOREIGN KEY ("SenderActorId") REFERENCES "Actors" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Messages_Conversations_ConversationId" FOREIGN KEY ("ConversationId") REFERENCES "Conversations" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Comments" (
    "Id" uuid NOT NULL,
    "InsightId" uuid NOT NULL,
    "AuthorActorId" uuid NOT NULL,
    "Content" text NOT NULL,
    "LikeCount" integer NOT NULL DEFAULT 0,
    "EditedAt" timestamp with time zone,
    "IsEdited" boolean NOT NULL,
    "ParentId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Comments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Comments_Actors_AuthorActorId" FOREIGN KEY ("AuthorActorId") REFERENCES "Actors" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Comments_Comments_ParentId" FOREIGN KEY ("ParentId") REFERENCES "Comments" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Comments_Insights_InsightId" FOREIGN KEY ("InsightId") REFERENCES "Insights" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "UserLikes" (
    "UserId" uuid NOT NULL,
    "InsightId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_UserLikes" PRIMARY KEY ("UserId", "InsightId"),
    CONSTRAINT "FK_UserLikes_Insights_InsightId" FOREIGN KEY ("InsightId") REFERENCES "Insights" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_UserLikes_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_Actors_MentorId" ON "Actors" ("MentorId");

CREATE INDEX "IX_Actors_UserId" ON "Actors" ("UserId");

CREATE INDEX "IX_Comments_AuthorActorId" ON "Comments" ("AuthorActorId");

CREATE INDEX "IX_Comments_InsightId" ON "Comments" ("InsightId");

CREATE INDEX "IX_Comments_ParentId" ON "Comments" ("ParentId");

CREATE INDEX "IX_Conversations_MentorId" ON "Conversations" ("MentorId");

CREATE UNIQUE INDEX "IX_Conversations_UserId_MentorId" ON "Conversations" ("UserId", "MentorId");

CREATE INDEX "IX_CreditTransactions_UserId" ON "CreditTransactions" ("UserId");

CREATE INDEX "IX_Insights_MentorId" ON "Insights" ("MentorId");

CREATE UNIQUE INDEX "IX_MentorRoles_Code" ON "MentorRoles" ("Code");

CREATE INDEX "IX_Mentors_CreatedBy" ON "Mentors" ("CreatedBy");

CREATE INDEX "IX_Mentors_RoleId" ON "Mentors" ("RoleId");

CREATE INDEX "IX_Messages_ConversationId" ON "Messages" ("ConversationId");

CREATE INDEX "IX_Messages_SenderActorId" ON "Messages" ("SenderActorId");

CREATE INDEX "IX_UserFollowsMentor_MentorId" ON "UserFollowsMentor" ("MentorId");

CREATE INDEX "IX_UserLikes_InsightId" ON "UserLikes" ("InsightId");

CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260209110148_InitialCreate', '8.0.0');

COMMIT;

