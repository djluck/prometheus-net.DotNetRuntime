using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace DocsGenerator
{
    public static class Extensions
    {
        private static string GetDirectoryPath(this Assembly assembly)
        {
            string codeBase = assembly.CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

        public static AssemblyXmlDocs LoadXmlDocumentation(this Assembly assembly)
        {
            string directoryPath = assembly.GetDirectoryPath();
            string xmlFilePath = Path.Combine(directoryPath, assembly.GetName().Name + ".xml");
            if (File.Exists(xmlFilePath)) {
                return new AssemblyXmlDocs(assembly, (File.ReadAllText(xmlFilePath)));
            }

            throw new FileNotFoundException("Unable to locate xmldoc", xmlFilePath);
        } 
        
    }

    /// <summary>
    /// Super hacky!
    /// </summary>
    public class AssemblyXmlDocs
    {
        private Dictionary<DocKey, MemberDocs> _loadedXmlDocumentation = new();

        public AssemblyXmlDocs(Assembly assembly, string xml)
        {
           var doc = XDocument.Parse(xml);

           _loadedXmlDocumentation = doc.Descendants("member")
               .Select(m =>
               {
                   var nameAttr = m.Attribute("name");
                   // e.g. M:Prometheus.DotNetRuntime.DotNetRuntimeStatsBuilder.Customize
                   var match = Regex.Match(nameAttr.Value, @"(?<member_type>[\w]):(?<parent_type>[^\(]+)\.(?<member_name>[^\.\(]+)");

                   var memberType = match.Groups["member_type"].Value switch
                   {
                       "T" => MemberTypes.TypeInfo,
                       "M" => MemberTypes.Method,
                       "P" => MemberTypes.Property,
                       "F" => MemberTypes.Field
                   };
                   
                   var parentTypeName = match.Groups["parent_type"].Value;
                   var parentType = assembly.GetType(parentTypeName);
                   
                   if (parentType == null && memberType != MemberTypes.TypeInfo) // TypeInfo doesn't currently work
                   {
                       // SO. HACKY.
                       parentType = assembly.GetType("Prometheus.DotNetRuntime.DotNetRuntimeStatsBuilder+Builder");
                   }
                   if (parentType == null && memberType != MemberTypes.TypeInfo) // TypeInfo doesn't currently work
                   {
                       throw new InvalidOperationException($"Could not locate type '{parentTypeName}'");
                   }
                   var key = new DocKey(memberType, parentType, match.Groups["member_name"].Value);
                   
                   return (key, memberDocs: new MemberDocs()
                   {
                        Summary = m.Descendants("summary").SingleOrDefault()?.Value?.Trim()
                   });
               })
               .GroupBy(x => x.key)
               // TODO if we ever need rely on override docs working correctly, fix this
               .ToDictionary(k => k.Key, v => v.First().memberDocs);

        }

        public MemberDocs GetDocumentation(MethodInfo methodInfo)
        {
            if (_loadedXmlDocumentation.TryGetValue((new DocKey(MemberTypes.Method, methodInfo.DeclaringType, methodInfo.Name)), out var documentation))
            {
                return documentation;    
            }

            throw new KeyNotFoundException($"Could not locate docs for {methodInfo}");
        }

        public class MemberDocs
        {
            public string Summary { get; set; }
        }
    }
    
    internal record DocKey(MemberTypes MemberType, Type ParentType, string Name)
    {
        public MemberTypes MemberType { get; } = MemberType;
        public Type ParentType { get; } = ParentType;
        public string Name { get; } = Name;
    }
}