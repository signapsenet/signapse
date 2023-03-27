using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Signapse.Server.Middleware;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Signapse.Server.Common.Services
{
    /// <summary>
    /// Stream content from assemblies in release mode, and the wwwroot in debug mode
    /// </summary>
    public class EmbeddedResourceLoader
    {
        private readonly EmbeddedResourceOptions options;
        private readonly FileExtensionContentTypeProvider extProvider = new FileExtensionContentTypeProvider();

        static string[] AllSourceFiles = { };
        static string GetFilePath([CallerFilePath] string filePath = "") { return filePath; }
        static EmbeddedResourceLoader()
        {
#if DEBUG
            // Store the list of source files for debug, so we don't have to recompile
            if (GetFilePath() is string filePath)
            {
                int idx = filePath.IndexOf("Signapse\\", StringComparison.OrdinalIgnoreCase);
                if (idx != -1)
                {
                    string[] validExtensions = { ".js", ".css", ".html" };
                    AllSourceFiles = Directory.EnumerateFiles(filePath.Substring(0, idx + 8), "*", SearchOption.AllDirectories)
                        .Where(f => validExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                        .ToArray();
                }
            }
#endif
        }

        public EmbeddedResourceLoader(EmbeddedResourceOptions options)
        {
            this.options = options;
        }

        static readonly Dictionary<Assembly, string[]> AssemblyResourceNames = new Dictionary<Assembly, string[]>();
        private string[] ResourceNames()
        {
            if (AssemblyResourceNames.TryGetValue(options.Assembly, out var res) == false)
            {
                res = options.Assembly.GetManifestResourceNames();
                AssemblyResourceNames[options.Assembly] = res;
            }

            return res;
        }

        /// <summary>
        /// Attempt to load the best matching files from the given path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Stream? LoadStream(string path, out string contentType)
        {
            // Find the closest matching resource name
            var truncatedPath = TruncatePath(options.ResourcePath + path);
            var resName = ResourceNames()
                .Where(r => TruncatePath(r).EndsWith(truncatedPath, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            // Grab the content type
            if (!extProvider.TryGetContentType(Path.GetFileName(path), out contentType!)
                || contentType == null)
            {
                contentType = "application/octet-stream";
            }

            if (resName != null)
            {
#if DEBUG
                if (FindFile(truncatedPath) is string filePath)
                {
                    return new FileStream(filePath, FileMode.Open, FileAccess.Read);
                }
                else
#endif
                    return options.Assembly.GetManifestResourceStream(resName);
            }

            return null;
        }

        private string? FindFile(string truncatedPath)
        {
            if (options.Assembly.GetName().Name is string asmName)
            {
                return AllSourceFiles
                    .Where(f => f.Contains(asmName, StringComparison.OrdinalIgnoreCase))
                    .Where(f => TruncatePath(f).EndsWith(truncatedPath, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();
            }

            return null;
        }

        private string TruncatePath(string str)
            => str.Replace("\\", "").Replace("/", "").Replace(".", "");
    }
}
