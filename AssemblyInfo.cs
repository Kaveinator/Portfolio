using System;
using System.Reflection;
using System.Reflection.Metadata;

// This class... needs some work
// Concept Source: https://www.meziantou.net/getting-the-date-of-build-of-a-dotnet-assembly-at-runtime.htm
[AttributeUsage(AttributeTargets.Assembly)]
public class AssemblyInfo : Attribute {
    public static readonly AssemblyInfo Current;

    //public Version Version { get; private set; }
    public Version Branch { get; private set; }

    static AssemblyInfo() {
        Assembly assembly = Assembly.GetExecutingAssembly();
        //Console.WriteLine(assembly.FullName);
        AssemblyInfo info = assembly.GetCustomAttribute<AssemblyInfo>();
        AssemblyName name = assembly.GetName();
        //info.Version = name.Version;
        Current = info;
    }

    public AssemblyInfo() {
        //Version = new Version(0, 1);
        Branch = new Version(0, 1);
    }

    public AssemblyInfo(string branch) {
        if (!Version.TryParse(branch, out Version _branch))
            _branch = new Version();
        Branch = _branch;
    }

    public string PrettyPrint()
        => $"{Branch.ToString(3)}r{Branch.Revision}";
}
