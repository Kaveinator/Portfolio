using Markdig;
using Portfolio.Common.Models;
using Portfolio.DevLog.Models;
using Portfolio.Orgs.Data;
using Portfolio.Projects.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebServer.Http;
using WebServer.Models;

namespace Portfolio.Projects.Models {
    public class ProjectPageModel : IPageModel {
        Dictionary<string, object> Values;
        Dictionary<string, object> IDataModel.Values => Values;
        readonly PortfolioEndpoint Endpoint;
        public readonly ProjectInfo Project;
        public readonly OrganizationInfo? Organization;

        // TODO: NavLinksThing
        public string SubHeader;
        public string LogoUrl;
        public string BackgroundUrl;
        public string OrganizationLinkElement;
        public readonly IEnumerable<ProjectLinkInfo> ProjectLinks;
        public readonly IEnumerable<TechUsedModel> TechnologiesUsed;
        public readonly ContainerModel? RelatedDevLogs;
        public readonly ProjectMediaModel MediaContainer;

        public ProjectPageModel(PortfolioEndpoint endpoint, ProjectInfo project, OrganizationInfo? org) {
            Endpoint = endpoint;
            Project = project;
            Organization = org;
            Values = new Dictionary<string, object>();

            bool isInOrg = org != null;
            SubHeader = isInOrg ? org!.Name : project.Role;
            LogoUrl = "/src" + (isInOrg
                ? $"/orgs/{org!.UrlName}.webp"
                : $"/projects/{project.UrlName}.webp");
            BackgroundUrl = "/src" + (isInOrg
                ? $"/orgs/{org!.UrlName}"
                : $"/projects"
            ) + $"/{project.UrlName}.Preview.webp";
            OrganizationLinkElement = !isInOrg ? string.Empty
                : $"/ <a href=\"/orgs/{org!.UrlName}\">{org.Name}</a>";

            ProjectLinks = endpoint.Database.ProjectLinksTable.GetLinksForProject(project);
            // Technologies Used
            var techUsed = endpoint.Database.TechnologiesUsedTable.GetTechUsedFromProject(project);
            var techInfo = endpoint.Database.TechnologiesTable.GetTechInfoSetFromTechUsedSet(techUsed);

            TechnologiesUsed = techUsed.Join(techInfo,
                techUsed => techUsed.TechId.Value,
                techInfo => techInfo.TechId.Value,
                (techUsed, techInfo) => new TechUsedModel(endpoint, techUsed, techInfo)
            );
            MediaContainer = new ProjectMediaModel(endpoint, Project);

            var posts = endpoint.Database.DevLogProjectBindingsTable.GetPosts(project)
                .OrderByDescending(post => post.CreatedTimestamp.Value)
                .Select(post => new DevLogEntryModel(endpoint, post));
            if (posts.Any()) {
                RelatedDevLogs = new ContainerModel("Related DevLogs", "DevLogs that mentioned this project") {
                    Content = posts
                };
                RelatedDevLogs.ClassList.Add("hr");
            }
        }

        public ProjectPageModel(PortfolioEndpoint endpoint, ProjectInfo project)
            : this(endpoint, project, endpoint.Database.OrganizationTable.GetOrgFromProject(project)) { }

        public HttpResponse Render() {
            Values = new Dictionary<string, object>() {
                { nameof(SubHeader), SubHeader },
                { nameof(LogoUrl), LogoUrl },
                { nameof(BackgroundUrl), BackgroundUrl },
                { nameof(TechnologiesUsed), TechnologiesUsed },
                { nameof(ProjectLinks), ProjectLinks },
                { nameof(MediaContainer), MediaContainer },
                { nameof(OrganizationLinkElement), OrganizationLinkElement },
                { nameof(RelatedDevLogs), RelatedDevLogs }
            };

            Project.Values.Update(Project.OverviewMarkdown.ColumnName, _ => Markdown.ToHtml(Project.OverviewMarkdown, PortfolioEndpoint.MarkdownPipeline))
                .Update(nameof(ProjectInfo.HeaderVerticalAnchorOverride), currentValue => currentValue ?? 40)
                .UpdateKeys(key => $"{nameof(Project)}.{key}").Do(Values.Add);
            if (Organization != null)
                Organization.Values.UpdateKeys(key => $"{nameof(Organization)}.{key}").Do(Values.Add);

            return Endpoint.TryGetTemplate("/ProjectPage/_Layout.html", out string content, out var errorStatus, this)
            ? new HttpResponse(
                HttpStatusCode.OK,
                content,
                MimeTypeMap.HtmlDocument
            ) : Endpoint.GetGenericStatusPage(errorStatus);
        }
    }
}
