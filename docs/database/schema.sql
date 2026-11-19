IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [AuditId] bigint NOT NULL IDENTITY,
        [EntityType] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(100) NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [ActorId] nvarchar(100) NOT NULL,
        [TimestampUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([AuditId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE TABLE [CredentialTypes] (
        [CredentialTypeId] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_CredentialTypes] PRIMARY KEY ([CredentialTypeId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE TABLE [Departments] (
        [DepartmentId] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(10) NOT NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY ([DepartmentId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE TABLE [OutboxEvents] (
        [EventId] uniqueidentifier NOT NULL,
        [EventType] nvarchar(100) NOT NULL,
        [SchemaVersion] int NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        [OccurredAtUtc] datetime2 NOT NULL,
        [PublishedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_OutboxEvents] PRIMARY KEY ([EventId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE TABLE [StaffMembers] (
        [StaffId] int NOT NULL IDENTITY,
        [StaffNumber] nvarchar(20) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Email] nvarchar(254) NOT NULL,
        [HireDate] date NOT NULL,
        [EmploymentStatus] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_StaffMembers] PRIMARY KEY ([StaffId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE TABLE [Services] (
        [ServiceId] int NOT NULL IDENTITY,
        [DepartmentId] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Services] PRIMARY KEY ([ServiceId]),
        CONSTRAINT [FK_Services_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([DepartmentId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE TABLE [Compensations] (
        [CompensationId] int NOT NULL IDENTITY,
        [StaffId] int NOT NULL,
        [AnnualSalary] decimal(12,2) NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        CONSTRAINT [PK_Compensations] PRIMARY KEY ([CompensationId]),
        CONSTRAINT [FK_Compensations_StaffMembers_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [StaffMembers] ([StaffId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE TABLE [Credentials] (
        [CredentialId] int NOT NULL IDENTITY,
        [StaffId] int NOT NULL,
        [CredentialTypeId] int NOT NULL,
        [IssueDate] date NOT NULL,
        [ExpiryDate] date NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_Credentials] PRIMARY KEY ([CredentialId]),
        CONSTRAINT [FK_Credentials_CredentialTypes_CredentialTypeId] FOREIGN KEY ([CredentialTypeId]) REFERENCES [CredentialTypes] ([CredentialTypeId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Credentials_StaffMembers_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [StaffMembers] ([StaffId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE TABLE [Requisitions] (
        [RequisitionId] int NOT NULL IDENTITY,
        [RequestingDeptId] int NOT NULL,
        [StaffId] int NULL,
        [Type] nvarchar(20) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [ReasonCode] nvarchar(50) NULL,
        [RoleTitle] nvarchar(100) NULL,
        [RequestedHeadcount] int NULL,
        [ProposedSalary] decimal(12,2) NULL,
        [BudgetDecision] nvarchar(20) NULL,
        [IdempotencyKey] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Requisitions] PRIMARY KEY ([RequisitionId]),
        CONSTRAINT [FK_Requisitions_Departments_RequestingDeptId] FOREIGN KEY ([RequestingDeptId]) REFERENCES [Departments] ([DepartmentId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Requisitions_StaffMembers_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [StaffMembers] ([StaffId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE TABLE [SecurityFlags] (
        [FlagId] int NOT NULL IDENTITY,
        [StaffId] int NOT NULL,
        [FlagType] nvarchar(50) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_SecurityFlags] PRIMARY KEY ([FlagId]),
        CONSTRAINT [FK_SecurityFlags_StaffMembers_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [StaffMembers] ([StaffId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE TABLE [RosterEntries] (
        [RosterEntryId] int NOT NULL IDENTITY,
        [StaffId] int NOT NULL,
        [ServiceId] int NOT NULL,
        [ShiftStart] datetime2 NOT NULL,
        [ShiftEnd] datetime2 NOT NULL,
        CONSTRAINT [PK_RosterEntries] PRIMARY KEY ([RosterEntryId]),
        CONSTRAINT [CK_RosterEntry_ShiftTimes] CHECK ([ShiftEnd] > [ShiftStart]),
        CONSTRAINT [FK_RosterEntries_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([ServiceId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RosterEntries_StaffMembers_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [StaffMembers] ([StaffId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE TABLE [StaffAssignments] (
        [AssignmentId] int NOT NULL IDENTITY,
        [StaffId] int NOT NULL,
        [ServiceId] int NOT NULL,
        [RoleTitle] nvarchar(100) NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NULL,
        CONSTRAINT [PK_StaffAssignments] PRIMARY KEY ([AssignmentId]),
        CONSTRAINT [FK_StaffAssignments_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([ServiceId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StaffAssignments_StaffMembers_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [StaffMembers] ([StaffId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'CredentialTypeId', N'Name') AND [object_id] = OBJECT_ID(N'[CredentialTypes]'))
        SET IDENTITY_INSERT [CredentialTypes] ON;
    EXEC(N'INSERT INTO [CredentialTypes] ([CredentialTypeId], [Name])
    VALUES (1, N''Paramedic Certification''),
    (2, N''Heavy Machinery Operator Licence''),
    (3, N''Bus Driver Licence'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'CredentialTypeId', N'Name') AND [object_id] = OBJECT_ID(N'[CredentialTypes]'))
        SET IDENTITY_INSERT [CredentialTypes] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'DepartmentId', N'Code', N'Name') AND [object_id] = OBJECT_ID(N'[Departments]'))
        SET IDENTITY_INSERT [Departments] ON;
    EXEC(N'INSERT INTO [Departments] ([DepartmentId], [Code], [Name])
    VALUES (1, N''HR'', N''Staff/HR''),
    (2, N''RES'', N''Residence''),
    (3, N''SEC'', N''Security''),
    (4, N''TRN'', N''Public Transport''),
    (5, N''MNT'', N''Maintenance''),
    (6, N''BUD'', N''Budget''),
    (7, N''EMG'', N''Emergency Services'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'DepartmentId', N'Code', N'Name') AND [object_id] = OBJECT_ID(N'[Departments]'))
        SET IDENTITY_INSERT [Departments] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'StaffId', N'Email', N'EmploymentStatus', N'FirstName', N'HireDate', N'LastName', N'StaffNumber') AND [object_id] = OBJECT_ID(N'[StaffMembers]'))
        SET IDENTITY_INSERT [StaffMembers] ON;
    EXEC(N'INSERT INTO [StaffMembers] ([StaffId], [Email], [EmploymentStatus], [FirstName], [HireDate], [LastName], [StaffNumber])
    VALUES (1, N''alex.example@example.com'', N''Active'', N''Alex'', ''2024-03-04'', N''Example'', N''EMP1001''),
    (2, N''sam.sample@example.com'', N''Active'', N''Sam'', ''2023-09-18'', N''Sample'', N''EMP1002''),
    (3, N''jordan.placeholder@example.com'', N''Terminated'', N''Jordan'', ''2022-01-10'', N''Placeholder'', N''EMP1003'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'StaffId', N'Email', N'EmploymentStatus', N'FirstName', N'HireDate', N'LastName', N'StaffNumber') AND [object_id] = OBJECT_ID(N'[StaffMembers]'))
        SET IDENTITY_INSERT [StaffMembers] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'CredentialId', N'CredentialTypeId', N'ExpiryDate', N'IssueDate', N'StaffId', N'Status') AND [object_id] = OBJECT_ID(N'[Credentials]'))
        SET IDENTITY_INSERT [Credentials] ON;
    EXEC(N'INSERT INTO [Credentials] ([CredentialId], [CredentialTypeId], [ExpiryDate], [IssueDate], [StaffId], [Status])
    VALUES (1, 1, ''2027-03-03'', ''2024-03-04'', 1, N''Valid'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'CredentialId', N'CredentialTypeId', N'ExpiryDate', N'IssueDate', N'StaffId', N'Status') AND [object_id] = OBJECT_ID(N'[Credentials]'))
        SET IDENTITY_INSERT [Credentials] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Compensations_StaffId] ON [Compensations] ([StaffId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Credentials_CredentialTypeId] ON [Credentials] ([CredentialTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Credentials_StaffId] ON [Credentials] ([StaffId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CredentialTypes_Name] ON [CredentialTypes] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Departments_Code] ON [Departments] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Departments_Name] ON [Departments] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Requisitions_IdempotencyKey] ON [Requisitions] ([IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Requisitions_RequestingDeptId] ON [Requisitions] ([RequestingDeptId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Requisitions_StaffId] ON [Requisitions] ([StaffId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RosterEntries_ServiceId] ON [RosterEntries] ([ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RosterEntries_StaffId_ShiftStart] ON [RosterEntries] ([StaffId], [ShiftStart]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SecurityFlags_StaffId] ON [SecurityFlags] ([StaffId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Services_DepartmentId_Name] ON [Services] ([DepartmentId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StaffAssignments_ServiceId] ON [StaffAssignments] ([ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_StaffAssignments_StaffId] ON [StaffAssignments] ([StaffId]) WHERE [EndDate] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StaffMembers_StaffNumber] ON [StaffMembers] ([StaffNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111051922_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261111051922_InitialCreate', N'10.0.14');
END;

COMMIT;
GO

