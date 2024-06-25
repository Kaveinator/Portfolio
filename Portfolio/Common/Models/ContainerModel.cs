using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebServer.Http;
using WebServer.Models;

namespace Portfolio.Common.Models {
    public class ContainerModel : IPageComponentModel {
        Dictionary<string, object> IDataModel.Values => new() {
            { nameof(Header), Header },
            { nameof(SubHeader), SubHeader },
            { nameof(ClassList), string.Join(' ', ClassList) },
            { nameof(Content), Content }
        };

        public readonly string Header;
        public readonly string SubHeader;
        public List<string> ClassList = new List<string>() { "container", "centerize" };
        public object Content;

        public ContainerModel(string title, string subheader) {
            Header = title;
            SubHeader = subheader;
            Content = null;
        }

        const string ViewPath = "kavemans.dev/Common/Container.html";
        public string Render() => HttpTemplates.TryGet(ViewPath, out string renderedContent, this)
            ? renderedContent
            : $"[ ContainerModel View not found! Path: '{ViewPath}' ]";
    }
}
