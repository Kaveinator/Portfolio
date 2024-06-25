using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebServer.Http;
using WebServer.Models;
using Portfolio.Projects.Data;
using Portfolio.Portfolio.Projects.Models;
using Portfolio.Orgs.Data;
using ExperimentalSQLite;

namespace Portfolio.Projects.Models {
    public class ProjectEntryModel : IPageComponentModel {
        // View: kavemans.dev/OrgPage/ProjectEntry.html
        public Dictionary<string, object> Values { get; }
        readonly HttpEndpointHandler EndpointProvider;

        public readonly ProjectInfo Project;
        public readonly OrganizationInfo? OrganizationInfo;

        public string ThumbnailUrl;
        public string ProjectUrl;
        public List<ProjectBadgeModel> Badges;

        public ProjectEntryModel(PortfolioEndpoint endpoint, ProjectInfo project, OrganizationInfo? organization = null) {
            EndpointProvider = endpoint;
            Project = project;
            if (organization != null)
                OrganizationInfo = organization;
            else if (project.OrganizationId.Value.HasValue) {
                var orgsTable = endpoint.Database.OrganizationTable;
                OrganizationInfo = organization = orgsTable.Query(1,
                    new WhereClause<long>(orgsTable.Schema.OrganizationId, "=", project.OrganizationId.Value.Value)
                ).FirstOrDefault(defaultValue: null);
            }
            // Am considering to remove IPageModel for the SQLiteRows....
            // but not sure, because it does allow me to use it in any model
            // with more ease, and it already removes private-only fields
            Values = new Dictionary<string, object>();
            project.Values.UpdateKeys(key => $"{nameof(project)}.{key}")
                .Do(kvp => Values.Add(kvp.Key, kvp.Value));

            var techUsed = endpoint.Database.TechnologiesUsedTable.GetTechUsedFromProject(project);
            var techInfos = endpoint.Database.TechnologiesTable.GetTechInfoSetFromTechUsedSet(techUsed);
            Badges = new(techUsed.Join(techInfos,
                techUsed => techUsed.TechId.Value,
                techInfo => techInfo.TechId.Value,
                (techUsed, techInfo) => new TechBadgeModel(endpoint, techUsed, techInfo)
            ));

            if (endpoint.Database.ProjectLinksTable.HasLiveProjectLink(project))
                Badges.Add(new LiveDemoBadgeModel(endpoint));


            bool isPartOfOrg = organization != null;
            ThumbnailUrl = "/src/"
                + (isPartOfOrg 
                  ? $"orgs/{organization!.UrlName}"
                  : $"projects"
                ) + $"/{project.UrlName}.Thumbnail.webp";
            ProjectUrl = (isPartOfOrg
                ? $"/orgs/{organization!.UrlName}"
                : $"/projects"
                ) + $"/{project.UrlName}";
            if (isPartOfOrg) {
                // Add org values
                organization!.Values.UpdateKeys(key => $"{nameof(organization)}.{key}")
                    .Do(kvp => Values.Add(kvp.Key, kvp.Value));
            }
        }

        public string Render() {
            // Add values since they could have been modified
            Values.Add(nameof(ThumbnailUrl), ThumbnailUrl);
            Values.Add(nameof(ProjectUrl), ProjectUrl);
            Values.Add(nameof(Badges), Badges);
            return EndpointProvider.GetTemplate("/Common/ProjectEntry.html", this);
        }
    }
}
