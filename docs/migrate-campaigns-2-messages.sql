begin tran
delete from [msg].[Hit]
delete from [msg].[Message]
delete from [msg].[Contact]
delete from [msg].[Campaign]
delete from [msg].[MessageType]


insert into [msg].[MessageType] (Id, [Name]) 
select Id, [Name] from [cmp].[CampaignType] 

GO

INSERT INTO [msg].[Campaign]
           ([Id]
           ,[Title]
           ,[Content]
           ,[ActionText]
           ,[ActionHref]
           ,[Published]
           ,[From]
           ,[To]
           ,[IsGlobal]
           ,[Data]
           ,[MessageChannelKind]
           ,[TypeId]
           ,[AttachmentId]
           ,[DistributionListId]
           ,[CreatedBy]
           ,[CreatedAt]
           ,[UpdatedBy]
           ,[UpdatedAt])
select 
	        Id
           ,Title
           ,(SELECT [Title] 'Inbox.title', 
				    [Content] 'Inbox.body' FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) as [Content]
           ,ActionText
           ,ActionUrl as ActionHref
           ,Published
           ,[From]
           ,[To]
           ,IsGlobal
           ,[Data]
           ,DeliveryChannel as MessageChannelKind
           ,TypeId
           ,AttachmentId
           ,null as DistributionListId
           ,'migration' as CreatedBy 
           ,CreatedAt
           ,null as UpdatedBy
           ,null as UpdatedAt
from [cmp].[Campaign] c

GO

insert into [msg].[Contact] (Id, RecipientId, UpdatedAt)
select NewId() as Id, UserCode as RecipientId, GetDate() as UpdatedAt
from (select distinct UserCode from [cmp].[CampaignUser] where UserCode <> '') a

GO

INSERT INTO [msg].[Message]
           ([Id]
           ,[RecipientId]
           ,[ContactId]
           ,[IsDeleted]
           ,[IsRead]
           ,[Content]
           ,[ReadDate]
           ,[DeleteDate]
           ,[CampaignId])
select
            cu.Id
           ,cu.UserCode as RecipientId
           ,(select top 1 Id from [msg].[Contact] where RecipientId = cu.UserCode) as ContactId
           ,cu.IsDeleted
           ,cu.IsRead
           ,(SELECT c.[Title] 'Inbox.title', 
				    c.[Content] 'Inbox.body' FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) as [Content]
           ,cu.ReadDate
           ,cu.DeleteDate
           ,cu.CampaignId
from [cmp].[CampaignUser] cu
left join [cmp].[Campaign] c on c.id = cu.CampaignId
GO

insert into [msg].[Hit] ([TimeStamp], CampaignId)
select [TimeStamp], CampaignId
from [cmp].[CampaignVisit]

--rollback