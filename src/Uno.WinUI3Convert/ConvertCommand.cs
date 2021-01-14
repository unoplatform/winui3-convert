using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Uno.WinUI3Convert
{
    public class ConvertCommand
    {
        public static int Execute(DirectoryInfo source, DirectoryInfo destination, bool overwrite, IHost host)
        {
            var logger =
                host.Services
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger<ConvertCommand>();

            try
            {
                if (destination.Exists)
                {
                    if (overwrite)
                    {
                        destination.Delete(true);

                        logger.LogInformation($"Directory \"{destination}\" deleted.");
                    }
                    else
                    {
                        logger.LogError("Destination exists and overwrite flag is not set.");

                        return -1;
                    }
                }

                destination.Create();

                logger.LogInformation($"Copying files to \"{destination}\"...");
                
                CopyFiles(source, destination);

                logger.LogInformation($"Rewriting files...");

                RewriteFiles(destination, logger);

                logger.LogInformation($"Rewriting projects...");

                RewriteProjects(destination, logger);

                logger.LogInformation($"Done.");

                return 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred.");

                return -1;
            }
        }

        private static void CopyFiles(DirectoryInfo source, DirectoryInfo destination)
        {
            var subdirectories = source.EnumerateDirectories("*", SearchOption.AllDirectories);

            foreach (var directory in subdirectories)
            {
                var pathFromBase = Path.GetRelativePath(source.FullName, directory.FullName);

                Directory.CreateDirectory(Path.Combine(destination.FullName, pathFromBase));
            }

            var files = source.EnumerateFiles("*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var pathFromBase = Path.GetRelativePath(source.FullName, file.FullName);

                File.Copy(file.FullName, Path.Combine(destination.FullName, pathFromBase));
            }
        }

        private static void RewriteFiles(DirectoryInfo source, ILogger<ConvertCommand> logger)
        {
            var files = source.EnumerateFiles("*.cs", SearchOption.AllDirectories);

            var mappings = new Dictionary<string, string>()
            {
                // Discard
                { "using Windows.UI.Input;",    string.Empty },

                // Usings
                { "using Windows.UI.Text;",                     "using Microsoft.UI.Text;\r\nusing Windows.UI.Text;" },
                { "using Windows.UI.Xaml.Automation.Peers;",    "using Microsoft.UI.Xaml.Automation.Peers;" },
                { "using Windows.UI.Xaml.Automation.Provider;", "using Microsoft.UI.Xaml.Automation.Provider;" },
                { "using Windows.UI.Xaml.Automation;",          "using Microsoft.UI.Xaml.Automation;" },
                { "using Windows.UI.Xaml.Controls.Primitives;", "using Microsoft.UI.Xaml.Controls.Primitives;" },
                { "using Windows.UI.Xaml.Controls;",            "using Microsoft.UI.Xaml.Controls;" },
                { "using Windows.UI.Xaml.Data;",                "using Microsoft.UI.Xaml.Data;" },
                { "using Windows.UI.Xaml.Input;",               "using Microsoft.UI.Xaml.Input;" },
                { "using Windows.UI.Xaml.Media.Animation;",     "using Microsoft.UI.Xaml.Media.Animation;" },
                { "using Windows.UI.Xaml.Media;",               "using Microsoft.UI.Xaml.Media;" },
                { "using Windows.UI.Xaml.Shapes;",              "using Microsoft.UI.Xaml.Shapes;" },
                { "using Windows.UI.Xaml;",                     "using Microsoft.UI.Xaml;" },
                { "using Windows.UI;",                          "using Microsoft.UI;" },

                // Namespaces
                { "Windows.UI.Xaml.Automation.Peers",   "Microsoft.UI.Xaml.Automation.Peers" },
                { "Windows.UI.Xaml.Automation",         "Microsoft.UI.Xaml.Automation" },
                { "Windows.UI.Xaml.Controls",           "Microsoft.UI.Xaml.Controls" },
            };

            var regexes = new Dictionary<string, string>()
            {
                // Microsoft.System conflict with System
                { "(global::)?System\\.Collections",    "global::System.Collections" },
                { "(global::)?System\\.ComponentModel", "global::System.ComponentModel" },
                { "(global::)?System\\.Globalization",  "global::System.Globalization" },
                { "(global::)?System\\.Reflection",     "global::System.Reflection" },
            };

            foreach (var file in files)
            {
                logger.LogInformation($"Rewriting {file}");

                var content = File.ReadAllText(file.FullName);

                foreach (var mapping in mappings)
                {
                    content = content.Replace(mapping.Key, mapping.Value);
                }

                foreach (var regex in regexes)
                {
                    content = Regex.Replace(content, regex.Key, regex.Value);
                }

                File.WriteAllText(file.FullName, content);
            }
        }

        private static void RewriteProjects(DirectoryInfo source, ILogger<ConvertCommand> logger)
        {
            var projects = source.EnumerateFiles("*.csproj", SearchOption.AllDirectories);

            foreach (var project in projects)
            {
                logger.LogInformation($"Rewriting {project}");

                var document = XDocument.Load(project.FullName);

                document.Root.Attribute("Sdk").Value = "MSBuild.Sdk.Extras/3.0.22";

                document.Root.Descendants("TargetFramework").Single().Value = "net5.0-windows10.0.18362.0";

                var winUIpackageReference = document.Root.Descendants("PackageReference").SingleOrDefault(e => e.Attribute("Include").Value == "Microsoft.WinUI");

                if (winUIpackageReference != null)
                {
                    winUIpackageReference.Attribute("Version").Value = "3.0.0-preview3.201113.0";
                }
                else
                {
                    document.Root.Add(new XElement("ItemGroup", new XElement("PackageReference", new XAttribute("Include", "Microsoft.WinUI"), new XAttribute("Version", "3.0.0-preview3.201113.0"), null)));
                }

                using (var xw = XmlWriter.Create(project.FullName, new XmlWriterSettings() { Encoding = Encoding.UTF8, OmitXmlDeclaration = true, Indent = true, NamespaceHandling = NamespaceHandling.OmitDuplicates }))
                {
                    document.Save(xw);
                }
            }
        }
    }
}
