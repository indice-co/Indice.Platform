using Indice.Features.Agents.Core.Data;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Agents.Core.Tests;

/// <summary>
/// An internal knowledge base category (the antifraud KB) must never reach an ordinary chat user. These tests pin the rules:
/// restricted chunks are invisible to an ordinary search whatever the category filter says, only code that opts in with
/// <see cref="RetrievalFilters.IncludeRestricted"/> reads them, and the category is not advertised to users.
/// </summary>
public class RestrictedCategoriesTests : IDisposable
{
    private const string Restricted = "antifraud-kb";

    private readonly AgentsDbContext _db = new(new DbContextOptionsBuilder<AgentsDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private readonly Dictionary<string, Guid> _ids = new();

    public RestrictedCategoriesTests() {
        // name -> (category, language)
        var chunks = new (string Name, string? Category, string? Language)[] {
            ("general-faq", "faq", "en"),
            ("general-policy-el", "policy", "el"),
            ("uncategorized", null, null),
            ("antifraud-en", Restricted, "en"),
            ("antifraud-el", Restricted, "el"),
            ("antifraud-mixed-case", "Antifraud-KB", "en"),
        };
        foreach (var (name, category, language) in chunks) {
            var id = Guid.NewGuid();
            _ids[name] = id;
            _db.Set<DbChunk>().Add(new DbChunk {
                Id = id,
                DocumentId = Guid.NewGuid(),
                Content = name,
                Category = category,
                Language = language,
                ContentHash = name,
                Embedding = new SqlVector<float>(new float[] { 0.1f, 0.2f, 0.3f })
            });
        }
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    private static AgentsOptions.TaxonomyOptions Taxonomy(params string[] restricted) => new() {
        Categories = ["faq", "policy", Restricted],
        RestrictedCategories = restricted
    };

    private HashSet<string> Search(RetrievalFilters filters, AgentsOptions.TaxonomyOptions taxonomy) {
        var ids = DocumentsService.ApplyScope(_db.Set<DbChunk>().AsNoTracking(), filters, taxonomy).Select(c => c.Id).ToList();
        return _ids.Where(pair => ids.Contains(pair.Value)).Select(pair => pair.Key).ToHashSet();
    }

    [Fact]
    public void Without_Restricted_Categories_Nothing_Changes() {
        var result = Search(new RetrievalFilters(), Taxonomy());

        Assert.Equal(_ids.Keys.ToHashSet(), result);
    }

    [Fact]
    public void An_Ordinary_Search_Never_Returns_Restricted_Chunks() {
        var result = Search(new RetrievalFilters(), Taxonomy(Restricted));

        Assert.Equal(new[] { "general-faq", "general-policy-el", "uncategorized" }.ToHashSet(), result);
    }

    [Fact]
    public void The_Rule_Does_Not_Depend_On_The_Casing_Stored_In_The_Database() {
        var result = Search(new RetrievalFilters(), Taxonomy(Restricted));

        Assert.DoesNotContain("antifraud-mixed-case", result);
    }

    [Fact]
    public void Naming_A_Restricted_Category_In_The_Filter_Does_Not_Unlock_It() {
        // The category filter can come from a classified intent, which is model output. It must not be a way in.
        var result = Search(new RetrievalFilters { Category = Restricted }, Taxonomy(Restricted));

        Assert.DoesNotContain("antifraud-en", result);
        Assert.DoesNotContain("antifraud-el", result);
        Assert.DoesNotContain("antifraud-mixed-case", result);
    }

    [Fact]
    public void An_Ordinary_Category_Filter_Still_Returns_That_Category_And_Uncategorized_Chunks() {
        var result = Search(new RetrievalFilters { Category = "faq" }, Taxonomy(Restricted));

        Assert.Equal(new[] { "general-faq", "uncategorized" }.ToHashSet(), result);
    }

    [Fact]
    public void Opting_In_With_The_Restricted_Category_Reads_Exactly_That_Category() {
        var result = Search(new RetrievalFilters { Category = Restricted, IncludeRestricted = true }, Taxonomy(Restricted));

        // Case-insensitive match, no category-less fallback, nothing from other categories.
        Assert.Equal(new[] { "antifraud-en", "antifraud-el", "antifraud-mixed-case" }.ToHashSet(), result);
    }

    [Fact]
    public void Opting_In_Without_A_Category_Returns_Everything() {
        // This is the router's knowledge-match search: it groups hits by category afterwards.
        var result = Search(new RetrievalFilters { IncludeRestricted = true }, Taxonomy(Restricted));

        Assert.Equal(_ids.Keys.ToHashSet(), result);
    }

    [Fact]
    public void The_Language_Filter_Applies_To_Restricted_Reads_Too() {
        var result = Search(new RetrievalFilters { Category = Restricted, Language = "el", IncludeRestricted = true }, Taxonomy(Restricted));

        Assert.Equal(new[] { "antifraud-el" }.ToHashSet(), result);
    }

    [Fact]
    public void Opting_In_For_An_Ordinary_Category_Keeps_The_Ordinary_Rules() {
        var result = Search(new RetrievalFilters { Category = "policy", IncludeRestricted = true }, Taxonomy(Restricted));

        Assert.Equal(new[] { "general-policy-el", "uncategorized" }.ToHashSet(), result);
    }
}

public class TaxonomyOptionsTests
{
    [Fact]
    public void PublicCategories_Leaves_Out_Restricted_Ones_Case_Insensitively() {
        var taxonomy = new AgentsOptions.TaxonomyOptions {
            Categories = ["faq", "Antifraud-KB", "policy"],
            RestrictedCategories = ["antifraud-kb"]
        };

        Assert.Equal(new[] { "faq", "policy" }, taxonomy.PublicCategories);
    }

    [Theory]
    [InlineData("antifraud-kb", true)]
    [InlineData("ANTIFRAUD-KB", true)]
    [InlineData("  antifraud-kb ", true)]
    [InlineData("faq", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsRestricted_Matches_Case_And_Whitespace_Insensitively(string? category, bool expected) {
        var taxonomy = new AgentsOptions.TaxonomyOptions { RestrictedCategories = ["antifraud-kb"] };

        Assert.Equal(expected, taxonomy.IsRestricted(category));
    }

    [Fact]
    public void By_Default_Nothing_Is_Restricted() {
        var taxonomy = new AgentsOptions.TaxonomyOptions();

        Assert.Empty(taxonomy.RestrictedCategories);
        Assert.Equal(taxonomy.Categories, taxonomy.PublicCategories);
    }

    private static List<string> Failures(Action<AgentsOptions> configure) {
        var options = new AgentsOptions();
        options.AzureOpenAI.Endpoint = "https://example.invalid";
        options.AzureOpenAI.ApiKey = "not-a-real-key";
        options.AzureOpenAI.Deployments.Reasoning = "r";
        options.AzureOpenAI.Deployments.Fast = "f";
        options.AzureOpenAI.Deployments.Embedding = "e";
        configure(options);
        var result = new AgentsOptionsValidator().Validate(null, options);
        return result.Failed ? [.. result.Failures] : [];
    }

    [Fact]
    public void A_Restricted_Category_Must_Also_Be_Ingestable() {
        var failures = Failures(o => {
            o.Taxonomy.Categories = ["faq"];
            o.Taxonomy.RestrictedCategories = ["antifraud-kb"];
        });

        Assert.Contains(failures, f => f.Contains("'antifraud-kb'") && f.Contains("Dex:Taxonomy:Categories"));
    }

    [Fact]
    public void A_Consistent_Restriction_Passes_Validation() {
        var failures = Failures(o => {
            o.Taxonomy.Categories = ["faq", "antifraud-kb"];
            o.Taxonomy.RestrictedCategories = ["ANTIFRAUD-KB"];
        });

        Assert.Empty(failures);
    }

    [Fact]
    public void Empty_Restricted_Entries_Are_Rejected() {
        var failures = Failures(o => o.Taxonomy.RestrictedCategories = [" "]);

        Assert.Contains(failures, f => f.Contains("RestrictedCategories") && f.Contains("empty"));
    }
}
