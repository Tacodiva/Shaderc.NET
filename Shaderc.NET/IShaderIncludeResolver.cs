
using System.Collections.Generic;
using System.IO;

namespace Shaderc;

public interface IShaderIncludeResolver {
    public static readonly IShaderIncludeResolver Default = new SimpleShaderIncludeResolver();
    /// <summary>
    /// Override it to provide a custom include resolution. Note that
    /// this Options instance must have been created with enableIncludes set to 'true'.
    /// </summary>
    /// <returns><c>true</c>, if the include was found, <c>false</c> otherwise.</returns>
    /// <param name="sourcePath">requesting source name</param>
    /// <param name="includePath">include name to search for.</param>
    /// <param name="incType">As in c, relative include or global</param>
    /// <param name="incFile">the resolved name of the include, empty if resolution failed</param>
    /// <param name="incContent">if resolution succeeded, contain the source code in plain text of the include</param>
    public bool TryFindInclude(string sourcePath, string includePath, ShaderIncludeType incType, out string incFile, out string incContent);
}

public class SimpleShaderIncludeResolver : IShaderIncludeResolver {

    /// <summary>
    /// List of the pathes to search when trying to resolve a 'Standard' include (enclosed in $lt;>)
    /// May be absolute pathes or relative to the executable directory.
    /// </summary>
    public readonly List<string> IncludeDirectories = new List<string>();

    public SimpleShaderIncludeResolver(params string[] includeDirs) {
        IncludeDirectories = new List<string>();
        IncludeDirectories.AddRange(includeDirs);
    }

    /// <summary>
    /// Adds one or more directories to the list of paths to search.
    /// </summary>
    /// <param name="dirs">The directories to add.</param>
    public void AddIncludeDirectory(params string[] dirs) {
        IncludeDirectories.AddRange(dirs);
    }

    public bool TryFindInclude(string sourcePath, string includePath, ShaderIncludeType incType, out string incFile, out string incContent) {
        if (incType == ShaderIncludeType.Relative) {
            incFile = Path.Combine(Path.GetDirectoryName(sourcePath), includePath);
            if (File.Exists(incFile)) {
                incContent = File.ReadAllText(incFile);
                return true;
            }

        } else {
            foreach (string incDir in IncludeDirectories) {
                incFile = Path.Combine(incDir, includePath);
                if (File.Exists(incFile)) {
                    incContent = File.ReadAllText(incFile);
                    return true;
                }
            }
        }

        incFile = "";
        incContent = "";
        return false;
    }
}