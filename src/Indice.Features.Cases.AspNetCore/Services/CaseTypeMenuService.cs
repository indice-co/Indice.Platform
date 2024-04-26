using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services;

/// <inheritdoc/>
public class CaseTypeMenuService(CasesDbContext dbContext) : ICaseTypeMenuService
{
    private readonly CasesDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    //private readonly ICaseAuthorizationProvider _memberAuthorizationProvider;

    //public CaseTypeMenuService(ICaseAuthorizationProvider memberAuthorizationProvider) {
    //    _memberAuthorizationProvider = memberAuthorizationProvider ?? throw new ArgumentNullException(nameof(memberAuthorizationProvider));
    //}

    /// <inheritdoc/>
    public async Task<ResultSet<CaseTypeMenu>> GetMenuItems(ListOptions options) {
        var result = await _dbContext.CaseTypes.AsNoTracking()
                               .Where(x => x.IsMenuItem)
                               .Select(x => new CaseTypeMenu {
                                   Id = x.Id,
                                   Title = x.Title
                               })
                               .ToResultSetAsync(options);

        return result;
    }

    /// <inheritdoc/>
    public async Task<ResultSet<CasePartial>> GetCasesByCaseTypeId(ClaimsPrincipal user, ListOptions<GetCasesListFilter> options, Guid caseTypeId) {
        var query = _dbContext.Cases
            .AsNoTracking()
            .Where(x=> x.CaseTypeId == caseTypeId) //TODO: Delete this and just use case type code
            .Where(c => !c.Draft) // filter out draft cases
            .Where(options.Filter.Metadata) // filter Metadata
            .Select(@case => new CasePartial {
                Id = @case.Id,
                ReferenceNumber = @case.ReferenceNumber,
                CustomerId = @case.Customer.CustomerId,
                CustomerName = @case.Customer.FirstName + " " + @case.Customer.LastName, // concat like this to enable searching with "contains"
                CreatedByWhen = @case.CreatedBy.When,
                CaseType = new CaseTypePartial {
                    Id = @case.CaseType.Id,
                    Code = @case.CaseType.Code,
                    Title = @case.CaseType.Title,
                    Translations = TranslationDictionary<CaseTypeTranslation>.FromJson(@case.CaseType.Translations)
                },
                Metadata = @case.Metadata,
                GroupId = @case.GroupId,
                CheckpointType = new CheckpointType {
                    Id = @case.Checkpoint.CheckpointType.Id,
                    Status = @case.Checkpoint.CheckpointType.Status,
                    Code = @case.Checkpoint.CheckpointType.Code,
                    Title = @case.Checkpoint.CheckpointType.Title,
                    Description = @case.Checkpoint.CheckpointType.Description,
                    Translations = TranslationDictionary<CheckpointTypeTranslation>.FromJson(@case.Checkpoint.CheckpointType.Translations)
                },
                AssignedToName = @case.AssignedTo.Name
            });

        // TODO: not crazy about this one
        // if a CaseAuthorizationService down the line wants to 
        // not allow a user to see the list of case, it throws a ResourceUnauthorizedException
        // which we catch and return an empty resultset. 
        //try {
        //    query = await _memberAuthorizationProvider.GetCaseMembership(query, user);
        //} catch (ResourceUnauthorizedException) {
        //    return new List<CasePartial>().ToResultSet();
        //}

        // filter ReferenceNumbers
        if (options.Filter.ReferenceNumbers.Any()) {
            foreach (var refNumber in options.Filter.ReferenceNumbers) {
                if (!int.TryParse(refNumber.Value, out var value)) {
                    continue;
                }
                switch (refNumber.Operator) {
                    case (FilterOperator.Eq):
                        query = query.Where(c => (c.ReferenceNumber ?? 0) == value);
                        break;
                    case (FilterOperator.Neq):
                        query = query.Where(c => (c.ReferenceNumber ?? 0) != value);
                        break;
                    case (FilterOperator.Contains):
                        query = query.Where(c => c.ReferenceNumber.HasValue && c.ReferenceNumber.ToString().Contains(value.ToString()));
                        break;
                }
            }
        }

        // filter CustomerId
        if (options.Filter.CustomerIds.Any()) {
            foreach (var customerId in options.Filter.CustomerIds) {
                switch (customerId.Operator) {
                    case (FilterOperator.Eq):
                        query = query.Where(c => c.CustomerId.Equals(customerId.Value));
                        break;
                    case (FilterOperator.Neq):
                        query = query.Where(c => !c.CustomerId.Equals(customerId.Value));
                        break;
                    case (FilterOperator.Contains):
                        query = query.Where(c => c.CustomerId.Contains(customerId.Value));
                        break;
                }
            }

        }
        // filter CustomerName
        if (options.Filter.CustomerNames.Any()) {
            foreach (var customerName in options.Filter.CustomerNames) {
                switch (customerName.Operator) {
                    case (FilterOperator.Eq):
                        query = query.Where(c => c.CustomerName.ToLower().Equals(customerName.Value.ToLower()));
                        break;
                    case (FilterOperator.Neq):
                        query = query.Where(c => !c.CustomerName.ToLower().Equals(customerName.Value.ToLower()));
                        break;
                    case (FilterOperator.Contains):
                        query = query.Where(c => c.CustomerName.ToLower().Contains(customerName.Value.ToLower()));
                        break;
                }
            }
        }
        if (options.Filter.From != null) {
            query = query.Where(c => c.CreatedByWhen >= options.Filter.From.Value.Date);
        }
        if (options.Filter.To != null) {
            query = query.Where(c => c.CreatedByWhen <= options.Filter.To.Value.Date.AddDays(1));
        }
        // filter CaseTypeCodes. You can reach this with an empty array only if you are admin/systemic user
        if (options.Filter.CaseTypeCodes.Any()) {
            // Create a different expression based on the filter operator
            var expressionsEq = options.Filter.CaseTypeCodes
                .Where(x => x.Operator == FilterOperator.Eq)
                .Select(f => (Expression<Func<CasePartial, bool>>)(c => c.CaseType.Code == f.Value));
            var expressionsNeq = options.Filter.CaseTypeCodes
                .Where(x => x.Operator == FilterOperator.Neq)
                .Select(f => (Expression<Func<CasePartial, bool>>)(c => c.CaseType.Code != f.Value));
            if (expressionsEq.Any()) {
                // Aggregate the expressions with OR in SQL
                var aggregatedExpressionEq = expressionsEq.Aggregate((expression, next) => {
                    var orExp = Expression.OrElse(expression.Body, Expression.Invoke(next, expression.Parameters));
                    return Expression.Lambda<Func<CasePartial, bool>>(orExp, expression.Parameters);
                });
                query = query.Where(aggregatedExpressionEq);
            }
            if (expressionsNeq.Any()) {
                // Aggregate the expression with AND in SQL
                var aggregatedExpressionNeq = expressionsNeq.Aggregate((expression, next) => {
                    var andExp = Expression.AndAlso(expression.Body, Expression.Invoke(next, expression.Parameters));
                    return Expression.Lambda<Func<CasePartial, bool>>(andExp, expression.Parameters);
                });
                query = query.Where(aggregatedExpressionNeq);
            }
        }

        // if we have Filter.CheckpointTypeCodes from the client, we have to map them to the correct checkpoint types for the filter to work
        if (options.Filter.CheckpointTypeCodes.Any()) {
            options.Filter.CheckpointTypeIds = await MapCheckpointTypeCodeToId(options.Filter.CheckpointTypeCodes);
        }

        // also: filter CheckpointTypeIds
        if (options.Filter.CheckpointTypeIds.Any()) {
            // Create a different expression based on the filter operator
            var expressionsEq = options.Filter.CheckpointTypeIds
                .Where(x => x.Operator == FilterOperator.Eq)
                .Select(f => (Expression<Func<CasePartial, bool>>)(c => c.CheckpointType.Id.ToString() == f.Value));
            var expressionsNeq = options.Filter.CheckpointTypeIds
                .Where(x => x.Operator == FilterOperator.Neq)
                .Select(f => (Expression<Func<CasePartial, bool>>)(c => c.CheckpointType.Id.ToString() != f.Value));
            if (expressionsEq.Any()) {
                // Aggregate the expressions with OR in SQL
                var aggregatedExpressionEq = expressionsEq.Aggregate((expression, next) => {
                    var orExp = Expression.OrElse(expression.Body, Expression.Invoke(next, expression.Parameters));
                    return Expression.Lambda<Func<CasePartial, bool>>(orExp, expression.Parameters);
                });
                query = query.Where(aggregatedExpressionEq);
            }
            if (expressionsNeq.Any()) {
                // Aggregate the expression with AND in SQL
                var aggregatedExpressionNeq = expressionsNeq.Aggregate((expression, next) => {
                    var andExp = Expression.AndAlso(expression.Body, Expression.Invoke(next, expression.Parameters));
                    return Expression.Lambda<Func<CasePartial, bool>>(andExp, expression.Parameters);
                });
                query = query.Where(aggregatedExpressionNeq);
            }
        }
        // filter by group ID, if it is present
        if (options.Filter.GroupIds.Any()) {
            foreach (var groupId in options.Filter.GroupIds) {
                switch (groupId.Operator) {
                    case (FilterOperator.Eq):
                        query = query.Where(c => c.GroupId.Equals(groupId.Value));
                        break;
                    case (FilterOperator.Neq):
                        query = query.Where(c => !c.GroupId.Equals(groupId.Value));
                        break;
                    case (FilterOperator.Contains):
                        query = query.Where(c => c.GroupId.Contains(groupId.Value));
                        break;
                }
            }
        }
        // sorting option
        if (string.IsNullOrEmpty(options?.Sort)) {
            options!.Sort = $"{nameof(CasePartial.CreatedByWhen)}";
        }
        var result = await query.ToResultSetAsync(options);
        // translate case types
        foreach (var item in result.Items) {
            item.CaseType = item.CaseType?.Translate(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, true);
            item.CheckpointType = item.CheckpointType?.Translate(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, true);
        }
        return result;
    }

    private async Task<List<FilterClause>> MapCheckpointTypeCodeToId(List<FilterClause> checkpointTypeCodeFilterClauses) {
        var checkpointTypeCodes = checkpointTypeCodeFilterClauses.Select(f => f.Value).ToList();
        var checkpointTypeIds = new List<FilterClause>();
        var filteredCheckpointTypesList = await _dbContext.CheckpointTypes
                .AsQueryable()
                .Where(checkpointType => checkpointTypeCodes.Contains(checkpointType.Code))
                .ToListAsync();
        foreach (var checkpointType in filteredCheckpointTypesList) {
            // find the checkpoint type that matches the checkpoint type code given in the parameters
            if (checkpointTypeCodeFilterClauses.Select(f => f.Value).Contains(checkpointType.Code)) {
                var checkpointTypeOperator = checkpointTypeCodeFilterClauses.FirstOrDefault(f => f.Value == checkpointType.Code).Operator;
                // create a new FilterClause object that holds the Id but also the operator
                var newCheckpointTypeIdFilterClause = new FilterClause("checkpointTypeId", checkpointType.Id.ToString(), checkpointTypeOperator, JsonDataType.String);
                checkpointTypeIds.Add(newCheckpointTypeIdFilterClause);
            }
        }
        return checkpointTypeIds;
    }
}
