using ExperimentalSQLite;
using Portfolio.Projects.Controllers;
using Portfolio.Projects.Data;
using Portfolio.Technologies.Data;
using WebServer.Http;
using WebServer.Models;

namespace Portfolio.Portfolio.Projects.Models {
    public class ProjectBadgeModel : IPageComponentModel {
        Dictionary<string, object> IDataModel.Values => new() {
            { nameof(ImageSource), ImageSource },
            { nameof(ImageAltText), ImageAltText },
            { nameof(TooltipText), TooltipText }
        };
        readonly PortfolioEndpoint ResourcesProvider;
        public string ImageSource = string.Empty;
        public string ImageAltText = string.Empty;
        public string TooltipText = "Unknown Badge";
        public ProjectBadgeModel(PortfolioEndpoint resourcesProvider) => ResourcesProvider = resourcesProvider;
        protected ProjectBadgeModel(PortfolioEndpoint resourcesProvider, string imageSource, string altText, string tooltipText) : this(resourcesProvider) {
            ImageSource = imageSource;
            ImageAltText = altText;
            TooltipText = tooltipText;
        }

        public string Render() => ResourcesProvider.GetTemplate("/Common/ProjectBadge.html", this);
    }
    public class TechBadgeModel : ProjectBadgeModel {
        public readonly TechnologyUsedInfo TechnologyUsedRefrence;
        public readonly TechnologyInfo? LinkedTechnologyInfo;


        /// <summary></summary>
        /// <param name="resourcesProvider">Used by the base class to get the View for the badge</param>
        /// <param name="techUsedInfo">The TechUsedInfo refrence used to make the badge</param>
        /// <param name="linkedTechInfo">The tech info that corresponds with techUsedInfo</param>
        public TechBadgeModel(PortfolioEndpoint resourcesProvider, TechnologyUsedInfo techUsedInfo, TechnologyInfo? linkedTechInfo)
            : base(resourcesProvider) {
            TechnologyUsedRefrence = techUsedInfo;
            LinkedTechnologyInfo = linkedTechInfo;

            ImageSource = $"/src/sprites/langs/{linkedTechInfo?.EnumName}.png";
            ImageAltText = LinkedTechnologyInfo?.EnumName ?? string.Empty;
            TooltipText = !string.IsNullOrEmpty(TechnologyUsedRefrence.BadgeTextOverride.Value)
                ? TechnologyUsedRefrence.BadgeTextOverride.Value
                : LinkedTechnologyInfo?.DefaultBadgeText.Value ?? "Unknown Badge";
        }

        /// <summary></summary>
        /// <param name="controller">Contains refrences to database, which is used to resolve LinkedTechnologyInfo if not provided</param>
        /// <param name="techUsedInfo">The TechUsedInfo refrence used to make the badge</param>
        public TechBadgeModel(ProjectController controller, TechnologyUsedInfo techUsedInfo)
            : this(controller.Endpoint, techUsedInfo, controller.TechnologiesTable.GetTechInfo(techUsedInfo)) {}
    }
    public class LiveDemoBadgeModel : ProjectBadgeModel {
        public LiveDemoBadgeModel(PortfolioEndpoint endpoint)
            : base(endpoint, "/src/sprites/LiveIcon.png", "Demo", "Live Demo Available") { }
    }
}
