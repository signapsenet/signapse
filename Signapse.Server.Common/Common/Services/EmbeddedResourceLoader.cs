﻿using Microsoft.Extensions.Options;
using Mustache;
using System.IO;
using System;
using System.Reflection;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using System.Collections.Generic;

namespace Signapse.Server.Common.Services
{
    /// <summary>
    /// Stream content from assemblies in release mode, and the wwwroot in debug mode
    /// </summary>
    public class EmbeddedResourceLoader
    {
        readonly Assembly asm;
        readonly string webRootPath;
        readonly string prefix;
        readonly FileExtensionContentTypeProvider extProvider = new FileExtensionContentTypeProvider();

        // Makes debugging easier if we just load from disk, when possible
        readonly static Dictionary<string, string[]> ContentFiles = new Dictionary<string, string[]>();

        public EmbeddedResourceLoader(Assembly asm, IWebHostEnvironment env)
            : this(asm, string.Empty, env)
        {
        }

        public EmbeddedResourceLoader(Assembly asm, string prefix, IWebHostEnvironment env)
        {
            this.webRootPath = env.WebRootPath ?? env.ContentRootPath;
            this.prefix = prefix;
            this.asm = asm;

            // Cache all the files from the path for this web folder
            if (ContentFiles.ContainsKey(this.webRootPath) == false)
            {
                ContentFiles[this.webRootPath] = Directory
                    .EnumerateFiles(this.webRootPath, "*.*", SearchOption.AllDirectories)
                    .OrderBy(s => s.Length)
                    .ToArray();
            }
        }

        /// <summary>
        /// Attempt to load the best matching files from the given path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Stream? LoadStream(string path, out string contentType)
        {
            var truncatedPath = TruncatePath(prefix + path);
            var resName = asm.GetManifestResourceNames() // TODO: Cache the resource names (performance?)
                .Where(r => TruncatePath(r).EndsWith(truncatedPath, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            // Grab the content type
            if (!extProvider.TryGetContentType(Path.GetFileName(path), out contentType))
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
                    return asm.GetManifestResourceStream(resName);
            }

            return null;
        }

        string? FindFile(string truncatedPath)
        {
            if (ContentFiles.TryGetValue(this.webRootPath, out var files))
            {
                return files
                    .FirstOrDefault(f => TruncatePath(f).EndsWith(truncatedPath, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return null;
            }
        }

        string TruncatePath(string str)
            => str.Replace("\\", "").Replace("/", "").Replace(".", "");
    }
}
