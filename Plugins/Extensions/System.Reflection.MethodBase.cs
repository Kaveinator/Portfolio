using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Reflection {
    public static class MethodBaseExt {
        public static string PrettyPrint(this MethodBase method)
            => $"{method.ReflectedType?.FullName ?? "<unknown>"}.{method.Name}";
    }
}
