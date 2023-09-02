namespace Indice.Features.Identity.UI.Pages;
/// <summary>Profile tab item view model</summary>
public class ProfileTabViewModel
{
    /// <summary>Constructs the tab model</summary>
    /// <param name="id">The id of the tab</param>
    /// <param name="tabName">The tab display name</param>
    /// <param name="pagePath">the Page path</param>
    public ProfileTabViewModel(string id, string tabName, string pagePath) {
        Id = id;
        TabName = tabName;
        PagePath = pagePath;
    }
    /// <summary>
    /// The id of the tab
    /// </summary>
    public string Id { get; }
    /// <summary>
    /// The tab display name
    /// </summary>
    public string TabName { get; }
    /// <summary>
    /// The page path
    /// </summary>
    public string PagePath { get; }

}
