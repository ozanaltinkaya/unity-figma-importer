using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cdm.Figma.UIToolkit
{
    [CreateAssetMenu(fileName = nameof(UIToolkit) + "-" + nameof(FigmaImporter), 
        menuName = AssetMenuRoot + "Figma Importer", order = 0)]
    public class FigmaImporter : Figma.FigmaImporter
    {
        public new const string AssetMenuRoot = Figma.FigmaImporter.AssetMenuRoot + "UI Toolkit/";

        [SerializeField]
        private string _typePrefix = "@type:";

        public string typePrefix
        {
            get => _typePrefix;
            set => _typePrefix = value;
        }
        
        [SerializeField]
        private string _bindingPrefix = "@id:";

        public string bindingPrefix
        {
            get => _bindingPrefix;
            set => _bindingPrefix = value;
        }
        
        [SerializeField]
        private string _localizationPrefix = "@lang:";

        public string localizationPrefix
        {
            get => _localizationPrefix;
            set => _localizationPrefix = value;
        }
        
        [SerializeField]
        private string _assetsPath = "Resources/Figma/Documents";

        public string assetsPath => _assetsPath;

        [SerializeField]
        private List<NodeConverter> _nodeConverters = new List<NodeConverter>();

        public List<NodeConverter> nodeConverters => _nodeConverters;
        
        [SerializeField]
        private List<ComponentConverter> _componentConverters = new List<ComponentConverter>();

        public List<ComponentConverter> componentConverters => _componentConverters;

        public override async Task ImportFileAsync(FigmaFile file, FigmaImportOptions options = null)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            options ??= new FigmaImportOptions();
            
            var assetsDirectory = Path.Combine("Assets", assetsPath);
            Directory.CreateDirectory(assetsDirectory);

            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
            XNamespace ui = "UnityEngine.UIElements";
            XNamespace uie = "UnityEditor.UIElements";

            var conversionArgs = new NodeConvertArgs(this, file);
            conversionArgs.namespaces = new XNamespaces(ui, uie);

            // Collect all component sets from all pages.
            var pages = file.document.children;
            foreach (var page in pages)
            {
                CollectComponentSets(page, conversionArgs.componentSets);
            }
            
            // Generate all pages.
            foreach (var page in pages)
            {
                if (options.pages != null && options.pages.All(p => p != page.id))
                    continue;
                
                var xml = new XDocument();
                var root = new XElement(ui + "UXML");
                root.Add(new XAttribute(XNamespace.Xmlns + nameof(xsi), xsi.NamespaceName));
                root.Add(new XAttribute(XNamespace.Xmlns + nameof(ui), ui.NamespaceName));
                root.Add(new XAttribute(xsi + "noNamespaceSchemaLocation", "../../../../../UIElementsSchema/UIElements.xsd"));
                xml.Add(root);
                
                // Add root visual element.
                var visualElement = new XElement(ui + nameof(VisualElement));
                visualElement.Add(new XAttribute("style", $"background-color: {page.backgroundColor}; flex-grow: 1;"));
                root.Add(visualElement);
                
                var nodes = page.children;
                foreach (var node in nodes)
                {
                    if (TryConvertNode(node, conversionArgs, out var element))
                    {
                        root.Add(element);
                    }
                }

                await Task.Run(() =>
                {
                    var documentPath = Path.Combine(assetsDirectory, $"{page.name}.uxml");
                    using var fileStream = File.Create(documentPath);
                    using var xmlWriter = new XmlTextWriter(fileStream, Encoding.UTF8);
                    xmlWriter.Formatting = Formatting.Indented;
                    xml.Save(xmlWriter);
                    
                    Debug.Log($"UI document has been saved to: {documentPath}");
                });
            }
            
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        internal bool TryConvertNode(Node node, NodeConvertArgs args, out XElement element)
        {
            // Try with component converters first.
            var componentConverter = componentConverters.FirstOrDefault(c => c.CanConvert(node, args));
            if (componentConverter != null)
            {
                element = componentConverter.Convert(node, args);
                return true;
            }
            
            // Try with node converters.
            var nodeConverter = nodeConverters.FirstOrDefault(c => c.CanConvert(node, args));
            if (nodeConverter != null)
            {
                element = nodeConverter.Convert(node, args);
                return true;
            }
            
            element = null;
            return false;
        }

        private static void CollectComponentSets(Node node, List<ComponentSetNode> ctx)
        {
            if (node is ComponentSetNode componentSet)
            {
                ctx.Add(componentSet);
            }
            else
            {
                var children = node.GetChildren();
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        CollectComponentSets(child, ctx);
                    }
                }
            }
        }
    }
}