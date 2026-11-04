using System.Globalization;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Models.Requests;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Core.Services;

internal class CategoryService : ICategoryService
{
    private readonly CasesDbContext _dbContext;

    public CategoryService(CasesDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<ResultSet<Category>> GetList(ListOptions options) {
        var query = _dbContext.Categories.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(options.Search)) {
            var search = options.Search.Trim();
            query = query.Where(c => (c.Name != null && c.Name.Contains(search)) || (c.Description != null && c.Description.Contains(search)));
        }
        if (string.IsNullOrWhiteSpace(options.Sort)) {
            options.Sort = $"{nameof(Category.Order)},{nameof(Category.Name)}";
        }
        var result = await query.Select(ToModel).ToResultSetAsync(options);
        for (var i = 0; i < result.Items.Length; i++) {
            result.Items[i] = result.Items[i].Translate(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, true);
        }
        return result;
    }

    public async Task<Category?> GetById(Guid id) =>
        await _dbContext.Categories.AsNoTracking().Where(x => x.Id == id).Select(ToModel).FirstOrDefaultAsync();

    public async Task<Category> Create(CategoryRequest request) {
        if (await _dbContext.Categories.AnyAsync(x => x.Name == request.Name)) {
            throw new BusinessException("A category with the same name already exists.", nameof(request.Name));
        }
        var entity = new DbCategory { Id = Guid.NewGuid() };
        Map(request, entity);
        _dbContext.Categories.Add(entity);
        await _dbContext.SaveChangesAsync();
        return (await GetById(entity.Id))!;
    }

    public async Task<Category?> Update(Guid id, CategoryRequest request) {
        var entity = await _dbContext.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) {
            return null;
        }
        if (await _dbContext.Categories.AnyAsync(x => x.Id != id && x.Name == request.Name)) {
            throw new BusinessException("A category with the same name already exists.", nameof(request.Name));
        }
        Map(request, entity);
        await _dbContext.SaveChangesAsync();
        return await GetById(id);
    }

    public async Task<bool> Delete(Guid id) {
        var entity = await _dbContext.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) {
            return false;
        }
        if (await _dbContext.CaseTypes.AnyAsync(x => x.CategoryId == id)) {
            throw new BusinessException("Category cannot be deleted because it is used by existing case types.", nameof(id));
        }
        _dbContext.Categories.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    private static void Map(CategoryRequest request, DbCategory entity) {
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.Order = request.Order;
        entity.Translations = request.Translations;
    }

    private static readonly System.Linq.Expressions.Expression<Func<DbCategory, Category>> ToModel = c => new Category {
        Id = c.Id,
        Name = c.Name!,
        Description = c.Description,
        Order = c.Order,
        Translations = c.Translations
    };
}
