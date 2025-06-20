-- migration for [cfg] schema

GO
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;

SET NUMERIC_ROUNDABORT OFF;


GO


PRINT N'Dropping Index [config].[ApiResourceClaim].[IX_ApiResourceClaim_ApiResourceId]...';


GO
DROP INDEX [IX_ApiResourceClaim_ApiResourceId]
    ON [config].[ApiResourceClaim];


GO
PRINT N'Dropping Index [config].[ApiResourceProperty].[IX_ApiResourceProperty_ApiResourceId]...';


GO
DROP INDEX [IX_ApiResourceProperty_ApiResourceId]
    ON [config].[ApiResourceProperty];


GO
PRINT N'Dropping Index [config].[ApiResourceScope].[IX_ApiResourceScope_ApiResourceId]...';


GO
DROP INDEX [IX_ApiResourceScope_ApiResourceId]
    ON [config].[ApiResourceScope];


GO
PRINT N'Dropping Index [config].[ApiScopeClaim].[IX_ApiScopeClaim_ScopeId]...';


GO
DROP INDEX [IX_ApiScopeClaim_ScopeId]
    ON [config].[ApiScopeClaim];


GO
PRINT N'Dropping Index [config].[ApiScopeProperty].[IX_ApiScopeProperty_ScopeId]...';


GO
DROP INDEX [IX_ApiScopeProperty_ScopeId]
    ON [config].[ApiScopeProperty];


GO
PRINT N'Dropping Index [config].[ClientClaim].[IX_ClientClaim_ClientId]...';


GO
DROP INDEX [IX_ClientClaim_ClientId]
    ON [config].[ClientClaim];


GO
PRINT N'Dropping Index [config].[ClientCorsOrigin].[IX_ClientCorsOrigin_ClientId]...';


GO
DROP INDEX [IX_ClientCorsOrigin_ClientId]
    ON [config].[ClientCorsOrigin];


GO
PRINT N'Dropping Index [config].[ClientGrantType].[IX_ClientGrantType_ClientId]...';


GO
DROP INDEX [IX_ClientGrantType_ClientId]
    ON [config].[ClientGrantType];


GO
PRINT N'Dropping Index [config].[ClientIdPRestriction].[IX_ClientIdPRestriction_ClientId]...';


GO
DROP INDEX [IX_ClientIdPRestriction_ClientId]
    ON [config].[ClientIdPRestriction];


GO
PRINT N'Dropping Index [config].[ClientPostLogoutRedirectUri].[IX_ClientPostLogoutRedirectUri_ClientId]...';


GO
DROP INDEX [IX_ClientPostLogoutRedirectUri_ClientId]
    ON [config].[ClientPostLogoutRedirectUri];


GO
PRINT N'Dropping Index [config].[ClientProperty].[IX_ClientProperty_ClientId]...';


GO
DROP INDEX [IX_ClientProperty_ClientId]
    ON [config].[ClientProperty];


GO
PRINT N'Dropping Index [config].[ClientRedirectUri].[IX_ClientRedirectUri_ClientId]...';


GO
DROP INDEX [IX_ClientRedirectUri_ClientId]
    ON [config].[ClientRedirectUri];


GO
PRINT N'Dropping Index [config].[ClientScope].[IX_ClientScope_ClientId]...';


GO
DROP INDEX [IX_ClientScope_ClientId]
    ON [config].[ClientScope];


GO
PRINT N'Dropping Index [config].[IdentityResourceClaim].[IX_IdentityResourceClaim_IdentityResourceId]...';


GO
DROP INDEX [IX_IdentityResourceClaim_IdentityResourceId]
    ON [config].[IdentityResourceClaim];


GO
PRINT N'Dropping Index [config].[IdentityResourceProperty].[IX_IdentityResourceProperty_IdentityResourceId]...';


GO
DROP INDEX [IX_IdentityResourceProperty_IdentityResourceId]
    ON [config].[IdentityResourceProperty];


GO
PRINT N'Dropping Foreign Key [config].[FK_ApiResourceClaim_ApiResource_ApiResourceId]...';


GO
ALTER TABLE [config].[ApiResourceClaim] DROP CONSTRAINT [FK_ApiResourceClaim_ApiResource_ApiResourceId];


GO
PRINT N'Dropping Foreign Key [config].[FK_ApiResourceProperty_ApiResource_ApiResourceId]...';


GO
ALTER TABLE [config].[ApiResourceProperty] DROP CONSTRAINT [FK_ApiResourceProperty_ApiResource_ApiResourceId];


GO
PRINT N'Dropping Foreign Key [config].[FK_ApiResourceScope_ApiResource_ApiResourceId]...';


GO
ALTER TABLE [config].[ApiResourceScope] DROP CONSTRAINT [FK_ApiResourceScope_ApiResource_ApiResourceId];


GO
PRINT N'Dropping Foreign Key [config].[FK_ApiResourceSecret_ApiResource_ApiResourceId]...';


GO
ALTER TABLE [config].[ApiResourceSecret] DROP CONSTRAINT [FK_ApiResourceSecret_ApiResource_ApiResourceId];


GO
PRINT N'Dropping Foreign Key [config].[FK_ClientClaim_Client_ClientId]...';


GO
ALTER TABLE [config].[ClientClaim] DROP CONSTRAINT [FK_ClientClaim_Client_ClientId];


GO
PRINT N'Dropping Foreign Key [config].[FK_ClientCorsOrigin_Client_ClientId]...';


GO
ALTER TABLE [config].[ClientCorsOrigin] DROP CONSTRAINT [FK_ClientCorsOrigin_Client_ClientId];


GO
PRINT N'Dropping Foreign Key [config].[FK_ClientGrantType_Client_ClientId]...';


GO
ALTER TABLE [config].[ClientGrantType] DROP CONSTRAINT [FK_ClientGrantType_Client_ClientId];


GO
PRINT N'Dropping Foreign Key [config].[FK_ClientIdPRestriction_Client_ClientId]...';


GO
ALTER TABLE [config].[ClientIdPRestriction] DROP CONSTRAINT [FK_ClientIdPRestriction_Client_ClientId];


GO
PRINT N'Dropping Foreign Key [config].[FK_ClientPostLogoutRedirectUri_Client_ClientId]...';


GO
ALTER TABLE [config].[ClientPostLogoutRedirectUri] DROP CONSTRAINT [FK_ClientPostLogoutRedirectUri_Client_ClientId];


GO
PRINT N'Dropping Foreign Key [config].[FK_ClientProperty_Client_ClientId]...';


GO
ALTER TABLE [config].[ClientProperty] DROP CONSTRAINT [FK_ClientProperty_Client_ClientId];


GO
PRINT N'Dropping Foreign Key [config].[FK_ClientRedirectUri_Client_ClientId]...';


GO
ALTER TABLE [config].[ClientRedirectUri] DROP CONSTRAINT [FK_ClientRedirectUri_Client_ClientId];


GO
PRINT N'Dropping Foreign Key [config].[FK_ClientScope_Client_ClientId]...';


GO
ALTER TABLE [config].[ClientScope] DROP CONSTRAINT [FK_ClientScope_Client_ClientId];


GO
PRINT N'Dropping Foreign Key [config].[FK_ClientSecret_Client_ClientId]...';


GO
ALTER TABLE [config].[ClientSecret] DROP CONSTRAINT [FK_ClientSecret_Client_ClientId];


GO
PRINT N'Dropping Foreign Key [config].[FK_ClientUser_Client_ClientId]...';


GO
ALTER TABLE [config].[ClientUser] DROP CONSTRAINT [FK_ClientUser_Client_ClientId];


GO
PRINT N'Starting rebuilding table [config].[ApiResource]...';


GO
BEGIN TRANSACTION;

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

SET XACT_ABORT ON;

CREATE TABLE [config].[tmp_ms_xx_ApiResource] (
    [Id]                                  INT             IDENTITY (1, 1) NOT NULL,
    [Enabled]                             BIT             NOT NULL,
    [Name]                                NVARCHAR (200)  NOT NULL,
    [DisplayName]                         NVARCHAR (200)  NULL,
    [Description]                         NVARCHAR (1000) NULL,
    [AllowedAccessTokenSigningAlgorithms] NVARCHAR (100)  NULL,
    [ShowInDiscoveryDocument]             BIT             NOT NULL,
    [RequireResourceIndicator]            BIT             NOT NULL,
    [Created]                             DATETIME2 (7)   NOT NULL,
    [Updated]                             DATETIME2 (7)   NULL,
    [LastAccessed]                        DATETIME2 (7)   NULL,
    [NonEditable]                         BIT             NOT NULL,
    CONSTRAINT [tmp_ms_xx_constraint_PK_ApiResource1] PRIMARY KEY CLUSTERED ([Id] ASC)
);

IF EXISTS (SELECT TOP 1 1 
           FROM   [config].[ApiResource])
    BEGIN
        SET IDENTITY_INSERT [config].[tmp_ms_xx_ApiResource] ON;
        INSERT INTO [config].[tmp_ms_xx_ApiResource] ([Id], [Enabled], [Name], [DisplayName], [Description], [AllowedAccessTokenSigningAlgorithms], [ShowInDiscoveryDocument], [RequireResourceIndicator], [Created], [Updated], [LastAccessed], [NonEditable])
        SELECT   [Id],
                 [Enabled],
                 [Name],
                 [DisplayName],
                 [Description],
                 [AllowedAccessTokenSigningAlgorithms],
                 [ShowInDiscoveryDocument],
                 0 AS [RequireResourceIndicator],
                 [Created],
                 [Updated],
                 [LastAccessed],
                 [NonEditable]
        FROM     [config].[ApiResource]
        ORDER BY [Id] ASC;
        SET IDENTITY_INSERT [config].[tmp_ms_xx_ApiResource] OFF;
    END

DROP TABLE [config].[ApiResource];

EXECUTE sp_rename N'[config].[tmp_ms_xx_ApiResource]', N'ApiResource';

EXECUTE sp_rename N'[config].[tmp_ms_xx_constraint_PK_ApiResource1]', N'PK_ApiResource', N'OBJECT';

COMMIT TRANSACTION;

SET TRANSACTION ISOLATION LEVEL READ COMMITTED;


GO
PRINT N'Creating Index [config].[ApiResource].[IX_ApiResource_Name]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ApiResource_Name]
    ON [config].[ApiResource]([Name] ASC);


GO
PRINT N'Altering Table [config].[ApiScope]...';


GO
ALTER TABLE [config].[ApiScope]
    ADD [Created]      DATETIME2 (7) NOT NULL CONSTRAINT DF_ApiScope_Created DEFAULT (SYSDATETIME()),
        [Updated]      DATETIME2 (7) NULL,
        [LastAccessed] DATETIME2 (7) NULL,
        [NonEditable]  BIT NOT NULL CONSTRAINT DF_ApiScope_NonEditable DEFAULT (0);


GO
PRINT N'Starting rebuilding table [config].[Client]...';


GO
BEGIN TRANSACTION;

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

SET XACT_ABORT ON;

CREATE TABLE [config].[tmp_ms_xx_Client] (
    [Id]                                    INT             IDENTITY (1, 1) NOT NULL,
    [Enabled]                               BIT             NOT NULL,
    [ClientId]                              NVARCHAR (200)  NOT NULL,
    [ProtocolType]                          NVARCHAR (200)  NOT NULL,
    [RequireClientSecret]                   BIT             NOT NULL,
    [ClientName]                            NVARCHAR (200)  NULL,
    [Description]                           NVARCHAR (1000) NULL,
    [ClientUri]                             NVARCHAR (2000) NULL,
    [LogoUri]                               NVARCHAR (2000) NULL,
    [RequireConsent]                        BIT             NOT NULL,
    [AllowRememberConsent]                  BIT             NOT NULL,
    [AlwaysIncludeUserClaimsInIdToken]      BIT             NOT NULL,
    [RequirePkce]                           BIT             NOT NULL,
    [AllowPlainTextPkce]                    BIT             NOT NULL,
    [RequireRequestObject]                  BIT             NOT NULL,
    [AllowAccessTokensViaBrowser]           BIT             NOT NULL,
    [RequireDPoP]                           BIT             NOT NULL,
    [DPoPValidationMode]                    INT             NOT NULL,
    [DPoPClockSkew]                         TIME (7)        NOT NULL,
    [FrontChannelLogoutUri]                 NVARCHAR (2000) NULL,
    [FrontChannelLogoutSessionRequired]     BIT             NOT NULL,
    [BackChannelLogoutUri]                  NVARCHAR (2000) NULL,
    [BackChannelLogoutSessionRequired]      BIT             NOT NULL,
    [AllowOfflineAccess]                    BIT             NOT NULL,
    [IdentityTokenLifetime]                 INT             NOT NULL,
    [AllowedIdentityTokenSigningAlgorithms] NVARCHAR (100)  NULL,
    [AccessTokenLifetime]                   INT             NOT NULL,
    [AuthorizationCodeLifetime]             INT             NOT NULL,
    [ConsentLifetime]                       INT             NULL,
    [AbsoluteRefreshTokenLifetime]          INT             NOT NULL,
    [SlidingRefreshTokenLifetime]           INT             NOT NULL,
    [RefreshTokenUsage]                     INT             NOT NULL,
    [UpdateAccessTokenClaimsOnRefresh]      BIT             NOT NULL,
    [RefreshTokenExpiration]                INT             NOT NULL,
    [AccessTokenType]                       INT             NOT NULL,
    [EnableLocalLogin]                      BIT             NOT NULL,
    [IncludeJwtId]                          BIT             NOT NULL,
    [AlwaysSendClientClaims]                BIT             NOT NULL,
    [ClientClaimsPrefix]                    NVARCHAR (200)  NULL,
    [PairWiseSubjectSalt]                   NVARCHAR (200)  NULL,
    [InitiateLoginUri]                      NVARCHAR (2000) NULL,
    [UserSsoLifetime]                       INT             NULL,
    [UserCodeType]                          NVARCHAR (100)  NULL,
    [DeviceCodeLifetime]                    INT             NOT NULL,
    [CibaLifetime]                          INT             NULL,
    [PollingInterval]                       INT             NULL,
    [CoordinateLifetimeWithUserSession]     BIT             NULL,
    [Created]                               DATETIME2 (7)   NOT NULL,
    [Updated]                               DATETIME2 (7)   NULL,
    [LastAccessed]                          DATETIME2 (7)   NULL,
    [NonEditable]                           BIT             NOT NULL,
    [PushedAuthorizationLifetime]           INT             NULL,
    [RequirePushedAuthorization]            BIT             NOT NULL,
    CONSTRAINT [tmp_ms_xx_constraint_PK_Client1] PRIMARY KEY CLUSTERED ([Id] ASC)
);

IF EXISTS (SELECT TOP 1 1 
           FROM   [config].[Client])
    BEGIN
        SET IDENTITY_INSERT [config].[tmp_ms_xx_Client] ON;
        INSERT INTO [config].[tmp_ms_xx_Client] ([Id], [Enabled], [ClientId], [ProtocolType], [RequireClientSecret], [ClientName], [Description], [ClientUri], [LogoUri], [RequireConsent], [AllowRememberConsent], [AlwaysIncludeUserClaimsInIdToken], [RequirePkce], [AllowPlainTextPkce], [RequireRequestObject], [AllowAccessTokensViaBrowser], [FrontChannelLogoutUri], [FrontChannelLogoutSessionRequired], [BackChannelLogoutUri], [BackChannelLogoutSessionRequired], [AllowOfflineAccess], [IdentityTokenLifetime], [AllowedIdentityTokenSigningAlgorithms], [AccessTokenLifetime], [AuthorizationCodeLifetime], [ConsentLifetime], [AbsoluteRefreshTokenLifetime], [SlidingRefreshTokenLifetime], [RefreshTokenUsage], [UpdateAccessTokenClaimsOnRefresh], [RefreshTokenExpiration], [AccessTokenType], [EnableLocalLogin], [IncludeJwtId], [AlwaysSendClientClaims], [ClientClaimsPrefix], [PairWiseSubjectSalt], [Created], [Updated], [LastAccessed], [UserSsoLifetime], [UserCodeType], [DeviceCodeLifetime], [NonEditable], [RequirePushedAuthorization], [RequireDPoP], [DPoPValidationMode], [DPoPClockSkew])
        SELECT   [Id],
                 [Enabled],
                 [ClientId],
                 [ProtocolType],
                 [RequireClientSecret],
                 [ClientName],
                 [Description],
                 [ClientUri],
                 [LogoUri],
                 [RequireConsent],
                 [AllowRememberConsent],
                 [AlwaysIncludeUserClaimsInIdToken],
                 [RequirePkce],
                 [AllowPlainTextPkce],
                 [RequireRequestObject],
                 [AllowAccessTokensViaBrowser],
                 [FrontChannelLogoutUri],
                 [FrontChannelLogoutSessionRequired],
                 [BackChannelLogoutUri],
                 [BackChannelLogoutSessionRequired],
                 [AllowOfflineAccess],
                 [IdentityTokenLifetime],
                 [AllowedIdentityTokenSigningAlgorithms],
                 [AccessTokenLifetime],
                 [AuthorizationCodeLifetime],
                 [ConsentLifetime],
                 [AbsoluteRefreshTokenLifetime],
                 [SlidingRefreshTokenLifetime],
                 [RefreshTokenUsage],
                 [UpdateAccessTokenClaimsOnRefresh],
                 [RefreshTokenExpiration],
                 [AccessTokenType],
                 [EnableLocalLogin],
                 [IncludeJwtId],
                 [AlwaysSendClientClaims],
                 [ClientClaimsPrefix],
                 [PairWiseSubjectSalt],
                 [Created],
                 [Updated],
                 [LastAccessed],
                 [UserSsoLifetime],
                 [UserCodeType],
                 [DeviceCodeLifetime],
                 [NonEditable],
                 0 AS [RequirePushedAuthorization],
                 0 AS [RequireDPoP],
                 1 AS [DPoPValidationMode], -- 1 is for IAT
                 N'00:05:00' AS [DPoPClockSkew] -- 5 minutes
        FROM     [config].[Client]
        ORDER BY [Id] ASC;
        SET IDENTITY_INSERT [config].[tmp_ms_xx_Client] OFF;
    END

DROP TABLE [config].[Client];

EXECUTE sp_rename N'[config].[tmp_ms_xx_Client]', N'Client';

EXECUTE sp_rename N'[config].[tmp_ms_xx_constraint_PK_Client1]', N'PK_Client', N'OBJECT';

COMMIT TRANSACTION;

SET TRANSACTION ISOLATION LEVEL READ COMMITTED;


GO
PRINT N'Creating Index [config].[Client].[IX_Client_ClientId]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Client_ClientId]
    ON [config].[Client]([ClientId] ASC);


GO
PRINT N'Altering Table [config].[ClientPostLogoutRedirectUri]...';


GO
ALTER TABLE [config].[ClientPostLogoutRedirectUri] ALTER COLUMN [PostLogoutRedirectUri] NVARCHAR (400) NOT NULL;


GO
PRINT N'Creating Index [config].[ClientPostLogoutRedirectUri].[IX_ClientPostLogoutRedirectUri_ClientId_PostLogoutRedirectUri]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ClientPostLogoutRedirectUri_ClientId_PostLogoutRedirectUri]
    ON [config].[ClientPostLogoutRedirectUri]([ClientId] ASC, [PostLogoutRedirectUri] ASC);


GO
PRINT N'Altering Table [config].[ClientRedirectUri]...';


GO
ALTER TABLE [config].[ClientRedirectUri] ALTER COLUMN [RedirectUri] NVARCHAR (400) NOT NULL;


GO
PRINT N'Creating Index [config].[ClientRedirectUri].[IX_ClientRedirectUri_ClientId_RedirectUri]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ClientRedirectUri_ClientId_RedirectUri]
    ON [config].[ClientRedirectUri]([ClientId] ASC, [RedirectUri] ASC);


GO
PRINT N'Creating Table [config].[IdentityProvider]...';


GO
CREATE TABLE [config].[IdentityProvider] (
    [Id]           INT            IDENTITY (1, 1) NOT NULL,
    [Scheme]       NVARCHAR (200) NOT NULL,
    [DisplayName]  NVARCHAR (200) NULL,
    [Enabled]      BIT            NOT NULL,
    [Type]         NVARCHAR (20)  NOT NULL,
    [Properties]   NVARCHAR (MAX) NULL,
    [Created]      DATETIME2 (7)  NOT NULL,
    [Updated]      DATETIME2 (7)  NULL,
    [LastAccessed] DATETIME2 (7)  NULL,
    [NonEditable]  BIT            NOT NULL,
    CONSTRAINT [PK_IdentityProvider] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
PRINT N'Creating Index [config].[IdentityProvider].[IX_IdentityProvider_Scheme]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_IdentityProvider_Scheme]
    ON [config].[IdentityProvider]([Scheme] ASC);


GO
PRINT N'Creating Index [config].[ApiResourceClaim].[IX_ApiResourceClaim_ApiResourceId_Type]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ApiResourceClaim_ApiResourceId_Type]
    ON [config].[ApiResourceClaim]([ApiResourceId] ASC, [Type] ASC);


GO
PRINT N'Creating Index [config].[ApiResourceProperty].[IX_ApiResourceProperty_ApiResourceId_Key]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ApiResourceProperty_ApiResourceId_Key]
    ON [config].[ApiResourceProperty]([ApiResourceId] ASC, [Key] ASC);


GO
PRINT N'Creating Index [config].[ApiResourceScope].[IX_ApiResourceScope_ApiResourceId_Scope]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ApiResourceScope_ApiResourceId_Scope]
    ON [config].[ApiResourceScope]([ApiResourceId] ASC, [Scope] ASC);


GO
PRINT N'Creating Index [config].[ApiScopeClaim].[IX_ApiScopeClaim_ScopeId_Type]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ApiScopeClaim_ScopeId_Type]
    ON [config].[ApiScopeClaim]([ScopeId] ASC, [Type] ASC);


GO
PRINT N'Creating Index [config].[ApiScopeProperty].[IX_ApiScopeProperty_ScopeId_Key]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ApiScopeProperty_ScopeId_Key]
    ON [config].[ApiScopeProperty]([ScopeId] ASC, [Key] ASC);


GO
PRINT N'Creating Index [config].[ClientClaim].[IX_ClientClaim_ClientId_Type_Value]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ClientClaim_ClientId_Type_Value]
    ON [config].[ClientClaim]([ClientId] ASC, [Type] ASC, [Value] ASC);


GO
PRINT N'Creating Index [config].[ClientCorsOrigin].[IX_ClientCorsOrigin_ClientId_Origin]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ClientCorsOrigin_ClientId_Origin]
    ON [config].[ClientCorsOrigin]([ClientId] ASC, [Origin] ASC);


GO
PRINT N'Creating Index [config].[ClientGrantType].[IX_ClientGrantType_ClientId_GrantType]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ClientGrantType_ClientId_GrantType]
    ON [config].[ClientGrantType]([ClientId] ASC, [GrantType] ASC);


GO
PRINT N'Creating Index [config].[ClientIdPRestriction].[IX_ClientIdPRestriction_ClientId_Provider]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ClientIdPRestriction_ClientId_Provider]
    ON [config].[ClientIdPRestriction]([ClientId] ASC, [Provider] ASC);


GO
PRINT N'Creating Index [config].[ClientProperty].[IX_ClientProperty_ClientId_Key]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ClientProperty_ClientId_Key]
    ON [config].[ClientProperty]([ClientId] ASC, [Key] ASC);


GO
PRINT N'Creating Index [config].[ClientScope].[IX_ClientScope_ClientId_Scope]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ClientScope_ClientId_Scope]
    ON [config].[ClientScope]([ClientId] ASC, [Scope] ASC);


GO
PRINT N'Creating Index [config].[IdentityResourceClaim].[IX_IdentityResourceClaim_IdentityResourceId_Type]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_IdentityResourceClaim_IdentityResourceId_Type]
    ON [config].[IdentityResourceClaim]([IdentityResourceId] ASC, [Type] ASC);


GO
PRINT N'Creating Index [config].[IdentityResourceProperty].[IX_IdentityResourceProperty_IdentityResourceId_Key]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_IdentityResourceProperty_IdentityResourceId_Key]
    ON [config].[IdentityResourceProperty]([IdentityResourceId] ASC, [Key] ASC);


GO
PRINT N'Creating Foreign Key [config].[FK_ApiResourceClaim_ApiResource_ApiResourceId]...';


GO
ALTER TABLE [config].[ApiResourceClaim] WITH NOCHECK
    ADD CONSTRAINT [FK_ApiResourceClaim_ApiResource_ApiResourceId] FOREIGN KEY ([ApiResourceId]) REFERENCES [config].[ApiResource] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [config].[FK_ApiResourceProperty_ApiResource_ApiResourceId]...';


GO
ALTER TABLE [config].[ApiResourceProperty] WITH NOCHECK
    ADD CONSTRAINT [FK_ApiResourceProperty_ApiResource_ApiResourceId] FOREIGN KEY ([ApiResourceId]) REFERENCES [config].[ApiResource] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [config].[FK_ApiResourceScope_ApiResource_ApiResourceId]...';


GO
ALTER TABLE [config].[ApiResourceScope] WITH NOCHECK
    ADD CONSTRAINT [FK_ApiResourceScope_ApiResource_ApiResourceId] FOREIGN KEY ([ApiResourceId]) REFERENCES [config].[ApiResource] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [config].[FK_ApiResourceSecret_ApiResource_ApiResourceId]...';


GO
ALTER TABLE [config].[ApiResourceSecret] WITH NOCHECK
    ADD CONSTRAINT [FK_ApiResourceSecret_ApiResource_ApiResourceId] FOREIGN KEY ([ApiResourceId]) REFERENCES [config].[ApiResource] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [config].[FK_ClientClaim_Client_ClientId]...';


GO
ALTER TABLE [config].[ClientClaim] WITH NOCHECK
    ADD CONSTRAINT [FK_ClientClaim_Client_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [config].[Client] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [config].[FK_ClientCorsOrigin_Client_ClientId]...';


GO
ALTER TABLE [config].[ClientCorsOrigin] WITH NOCHECK
    ADD CONSTRAINT [FK_ClientCorsOrigin_Client_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [config].[Client] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [config].[FK_ClientGrantType_Client_ClientId]...';


GO
ALTER TABLE [config].[ClientGrantType] WITH NOCHECK
    ADD CONSTRAINT [FK_ClientGrantType_Client_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [config].[Client] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [config].[FK_ClientIdPRestriction_Client_ClientId]...';


GO
ALTER TABLE [config].[ClientIdPRestriction] WITH NOCHECK
    ADD CONSTRAINT [FK_ClientIdPRestriction_Client_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [config].[Client] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [config].[FK_ClientPostLogoutRedirectUri_Client_ClientId]...';


GO
ALTER TABLE [config].[ClientPostLogoutRedirectUri] WITH NOCHECK
    ADD CONSTRAINT [FK_ClientPostLogoutRedirectUri_Client_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [config].[Client] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [config].[FK_ClientProperty_Client_ClientId]...';


GO
ALTER TABLE [config].[ClientProperty] WITH NOCHECK
    ADD CONSTRAINT [FK_ClientProperty_Client_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [config].[Client] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [config].[FK_ClientRedirectUri_Client_ClientId]...';


GO
ALTER TABLE [config].[ClientRedirectUri] WITH NOCHECK
    ADD CONSTRAINT [FK_ClientRedirectUri_Client_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [config].[Client] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [config].[FK_ClientScope_Client_ClientId]...';


GO
ALTER TABLE [config].[ClientScope] WITH NOCHECK
    ADD CONSTRAINT [FK_ClientScope_Client_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [config].[Client] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [config].[FK_ClientSecret_Client_ClientId]...';


GO
ALTER TABLE [config].[ClientSecret] WITH NOCHECK
    ADD CONSTRAINT [FK_ClientSecret_Client_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [config].[Client] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [config].[FK_ClientUser_Client_ClientId]...';


GO
ALTER TABLE [config].[ClientUser] WITH NOCHECK
    ADD CONSTRAINT [FK_ClientUser_Client_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [config].[Client] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Checking existing data against newly created constraints';

GO
ALTER TABLE [config].[ApiResourceClaim] WITH CHECK CHECK CONSTRAINT [FK_ApiResourceClaim_ApiResource_ApiResourceId];

ALTER TABLE [config].[ApiResourceProperty] WITH CHECK CHECK CONSTRAINT [FK_ApiResourceProperty_ApiResource_ApiResourceId];

ALTER TABLE [config].[ApiResourceScope] WITH CHECK CHECK CONSTRAINT [FK_ApiResourceScope_ApiResource_ApiResourceId];

ALTER TABLE [config].[ApiResourceSecret] WITH CHECK CHECK CONSTRAINT [FK_ApiResourceSecret_ApiResource_ApiResourceId];

ALTER TABLE [config].[ClientClaim] WITH CHECK CHECK CONSTRAINT [FK_ClientClaim_Client_ClientId];

ALTER TABLE [config].[ClientCorsOrigin] WITH CHECK CHECK CONSTRAINT [FK_ClientCorsOrigin_Client_ClientId];

ALTER TABLE [config].[ClientGrantType] WITH CHECK CHECK CONSTRAINT [FK_ClientGrantType_Client_ClientId];

ALTER TABLE [config].[ClientIdPRestriction] WITH CHECK CHECK CONSTRAINT [FK_ClientIdPRestriction_Client_ClientId];

ALTER TABLE [config].[ClientPostLogoutRedirectUri] WITH CHECK CHECK CONSTRAINT [FK_ClientPostLogoutRedirectUri_Client_ClientId];

ALTER TABLE [config].[ClientProperty] WITH CHECK CHECK CONSTRAINT [FK_ClientProperty_Client_ClientId];

ALTER TABLE [config].[ClientRedirectUri] WITH CHECK CHECK CONSTRAINT [FK_ClientRedirectUri_Client_ClientId];

ALTER TABLE [config].[ClientScope] WITH CHECK CHECK CONSTRAINT [FK_ClientScope_Client_ClientId];

ALTER TABLE [config].[ClientSecret] WITH CHECK CHECK CONSTRAINT [FK_ClientSecret_Client_ClientId];

ALTER TABLE [config].[ClientUser] WITH CHECK CHECK CONSTRAINT [FK_ClientUser_Client_ClientId];


GO
PRINT N'[cfg] Update complete.';


GO

-- migration for [op] schema

GO
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;

SET NUMERIC_ROUNDABORT OFF;


GO
PRINT N'Starting rebuilding table [auth].[PersistedGrant]...';


GO
BEGIN TRANSACTION;

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

SET XACT_ABORT ON;

CREATE TABLE [auth].[tmp_ms_xx_PersistedGrant] (
    [Id]           BIGINT         IDENTITY (1, 1) NOT NULL,
    [Key]          NVARCHAR (200) NULL,
    [Type]         NVARCHAR (50)  NOT NULL,
    [SubjectId]    NVARCHAR (200) NULL,
    [SessionId]    NVARCHAR (100) NULL,
    [ClientId]     NVARCHAR (200) NOT NULL,
    [Description]  NVARCHAR (200) NULL,
    [CreationTime] DATETIME2 (7)  NOT NULL,
    [Expiration]   DATETIME2 (7)  NULL,
    [ConsumedTime] DATETIME2 (7)  NULL,
    [Data]         NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [tmp_ms_xx_constraint_PK_PersistedGrant1] PRIMARY KEY CLUSTERED ([Id] ASC)
);

IF EXISTS (SELECT TOP 1 1 
           FROM   [auth].[PersistedGrant])
    BEGIN
        INSERT INTO [auth].[tmp_ms_xx_PersistedGrant] ([Key], [Type], [SubjectId], [SessionId], [ClientId], [Description], [CreationTime], [Expiration], [ConsumedTime], [Data])
        SELECT [Key],
               [Type],
               [SubjectId],
               [SessionId],
               [ClientId],
               [Description],
               [CreationTime],
               [Expiration],
               [ConsumedTime],
               [Data]
        FROM   [auth].[PersistedGrant];
    END

DROP TABLE [auth].[PersistedGrant];

EXECUTE sp_rename N'[auth].[tmp_ms_xx_PersistedGrant]', N'PersistedGrant';

EXECUTE sp_rename N'[auth].[tmp_ms_xx_constraint_PK_PersistedGrant1]', N'PK_PersistedGrant', N'OBJECT';

COMMIT TRANSACTION;

SET TRANSACTION ISOLATION LEVEL READ COMMITTED;


GO
PRINT N'Creating Index [auth].[PersistedGrant].[IX_PersistedGrant_ConsumedTime]...';


GO
CREATE NONCLUSTERED INDEX [IX_PersistedGrant_ConsumedTime]
    ON [auth].[PersistedGrant]([ConsumedTime] ASC);


GO
PRINT N'Creating Index [auth].[PersistedGrant].[IX_PersistedGrant_Expiration]...';


GO
CREATE NONCLUSTERED INDEX [IX_PersistedGrant_Expiration]
    ON [auth].[PersistedGrant]([Expiration] ASC);


GO
PRINT N'Creating Index [auth].[PersistedGrant].[IX_PersistedGrant_Key]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_PersistedGrant_Key]
    ON [auth].[PersistedGrant]([Key] ASC) WHERE ([Key] IS NOT NULL);


GO
PRINT N'Creating Index [auth].[PersistedGrant].[IX_PersistedGrant_SubjectId_ClientId_Type]...';


GO
CREATE NONCLUSTERED INDEX [IX_PersistedGrant_SubjectId_ClientId_Type]
    ON [auth].[PersistedGrant]([SubjectId] ASC, [ClientId] ASC, [Type] ASC);


GO
PRINT N'Creating Index [auth].[PersistedGrant].[IX_PersistedGrant_SubjectId_SessionId_Type]...';


GO
CREATE NONCLUSTERED INDEX [IX_PersistedGrant_SubjectId_SessionId_Type]
    ON [auth].[PersistedGrant]([SubjectId] ASC, [SessionId] ASC, [Type] ASC);


GO
PRINT N'Creating Table [auth].[Key]...';


GO
CREATE TABLE [auth].[Key] (
    [Id]                NVARCHAR (450) NOT NULL,
    [Version]           INT            NOT NULL,
    [Created]           DATETIME2 (7)  NOT NULL,
    [Use]               NVARCHAR (450) NULL,
    [Algorithm]         NVARCHAR (100) NOT NULL,
    [IsX509Certificate] BIT            NOT NULL,
    [DataProtected]     BIT            NOT NULL,
    [Data]              NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_Key] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
PRINT N'Creating Index [auth].[Key].[IX_Key_Use]...';


GO
CREATE NONCLUSTERED INDEX [IX_Key_Use]
    ON [auth].[Key]([Use] ASC);


GO
PRINT N'Creating Table [auth].[PushedAuthorizationRequest]...';


GO
CREATE TABLE [auth].[PushedAuthorizationRequest] (
    [Id]                 BIGINT         IDENTITY (1, 1) NOT NULL,
    [ReferenceValueHash] NVARCHAR (64)  NOT NULL,
    [ExpiresAtUtc]       DATETIME2 (7)  NOT NULL,
    [Parameters]         NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_PushedAuthorizationRequest] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
PRINT N'Creating Index [auth].[PushedAuthorizationRequest].[IX_PushedAuthorizationRequest_ExpiresAtUtc]...';


GO
CREATE NONCLUSTERED INDEX [IX_PushedAuthorizationRequest_ExpiresAtUtc]
    ON [auth].[PushedAuthorizationRequest]([ExpiresAtUtc] ASC);


GO
PRINT N'Creating Index [auth].[PushedAuthorizationRequest].[IX_PushedAuthorizationRequest_ReferenceValueHash]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_PushedAuthorizationRequest_ReferenceValueHash]
    ON [auth].[PushedAuthorizationRequest]([ReferenceValueHash] ASC);


GO
PRINT N'Creating Table [auth].[ServerSideSession]...';


GO
CREATE TABLE [auth].[ServerSideSession] (
    [Id]          BIGINT         IDENTITY (1, 1) NOT NULL,
    [Key]         NVARCHAR (100) NOT NULL,
    [Scheme]      NVARCHAR (100) NOT NULL,
    [SubjectId]   NVARCHAR (100) NOT NULL,
    [SessionId]   NVARCHAR (100) NULL,
    [DisplayName] NVARCHAR (100) NULL,
    [Created]     DATETIME2 (7)  NOT NULL,
    [Renewed]     DATETIME2 (7)  NOT NULL,
    [Expires]     DATETIME2 (7)  NULL,
    [Data]        NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_ServerSideSession] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
PRINT N'Creating Index [auth].[ServerSideSession].[IX_ServerSideSession_DisplayName]...';


GO
CREATE NONCLUSTERED INDEX [IX_ServerSideSession_DisplayName]
    ON [auth].[ServerSideSession]([DisplayName] ASC);


GO
PRINT N'Creating Index [auth].[ServerSideSession].[IX_ServerSideSession_Expires]...';


GO
CREATE NONCLUSTERED INDEX [IX_ServerSideSession_Expires]
    ON [auth].[ServerSideSession]([Expires] ASC);


GO
PRINT N'Creating Index [auth].[ServerSideSession].[IX_ServerSideSession_Key]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ServerSideSession_Key]
    ON [auth].[ServerSideSession]([Key] ASC);


GO
PRINT N'Creating Index [auth].[ServerSideSession].[IX_ServerSideSession_SessionId]...';


GO
CREATE NONCLUSTERED INDEX [IX_ServerSideSession_SessionId]
    ON [auth].[ServerSideSession]([SessionId] ASC);


GO
PRINT N'Creating Index [auth].[ServerSideSession].[IX_ServerSideSession_SubjectId]...';


GO
CREATE NONCLUSTERED INDEX [IX_ServerSideSession_SubjectId]
    ON [auth].[ServerSideSession]([SubjectId] ASC);


GO
PRINT N'[op] Update complete.';


GO
