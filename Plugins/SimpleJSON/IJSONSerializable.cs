using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleJSON {
    public interface IJSONSerializable {
        void Parse(JSONNode node);
        JSONNode ToJSON();
    }
}
