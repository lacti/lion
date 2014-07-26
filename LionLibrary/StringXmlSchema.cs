using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LionLibrary
{
    public class StringXmlSchema
    {
        private readonly Node _root = new Node();

        public string Name { get; set; }
        public Node Root { get { return _root; } }

        #region load & save

        public void Load(string schemaPath)
        {
            var schemaXml = XElement.Load(schemaPath);
            Name = schemaXml.Attribute("name").Value;
            Load(_root, schemaXml);
        }

        private static void Load(Node schemaNode, XElement schemaElement)
        {
            foreach (var childSchemaElement in schemaElement.Elements("node"))
                Load(schemaNode.AddOrGetChild(childSchemaElement.Attribute("name").Value), childSchemaElement);
            foreach (var attributeElement in schemaElement.Elements("attr"))
                schemaNode.Attributes[attributeElement.Attribute("name").Value] = (AttributeType) Enum.Parse(typeof (AttributeType), attributeElement.Attribute("type").Value, true);
        }

        public void Save(string schemaPath)
        {
            var schemaXml = new XElement("schema",
                new XAttribute("name", Name));
            Save(_root, schemaXml);
            schemaXml.Save(schemaPath);
        }

        private static void Save(Node schemaNode, XElement schemaElement)
        {
            foreach (var childSchemaNode in schemaNode.Children.Values)
            {
                var childSchemaElement = new XElement("node", 
                    new XAttribute("name", childSchemaNode.Name));
                schemaElement.Add(childSchemaElement);
                Save(childSchemaNode, childSchemaElement);
            }
            foreach (var attribute in schemaNode.Attributes)
            {
                schemaElement.Add(new XElement("attr", 
                    new XAttribute("name", attribute.Key),
                    new XAttribute("type", attribute.Value.ToString().ToLower())));
            }
        }

        #endregion

        #region build

        public void BuildFromXml(string xmlPath)
        {
            var rootElement = XElement.Load(xmlPath);
            var rootSchema = _root.AddOrGetChild(rootElement.Name.LocalName);
            BuildFromXml(rootSchema, rootElement);
            Name = Path.GetFileNameWithoutExtension(xmlPath);
        }

        private static void BuildFromXml(Node schemaNode, XElement element)
        {
            foreach (var childElement in element.Elements())
                BuildFromXml(schemaNode.AddOrGetChild(childElement.Name.LocalName), childElement);
            foreach (var attribute in element.Attributes())
                schemaNode.Attributes[attribute.Name.LocalName] = AttributeType.None;
        }

        #endregion

        #region enumerate

        public void Enumerate(Action<Node> enumerator)
        {
            foreach (var childNode in Root.Children.Values)
                Enumerate(childNode, enumerator);
        }

        private void Enumerate(Node targetNode, Action<Node> enumerator)
        {
            enumerator(targetNode);

            foreach (var childNode in targetNode.Children.Values)
                Enumerate(childNode, enumerator);
        }

        #endregion

        public class Node
        {
            public string Name { get; set; }
            public Node Parent { get; set; }

            public readonly Dictionary<string /* name */, Node> Children = new Dictionary<string, Node>();
            public readonly Dictionary<string /* attrName */, AttributeType> Attributes = new Dictionary<string, AttributeType>();

            #region edit node

            internal Node AddOrGetChild(string name)
            {
                Node childNode;
                if (Children.TryGetValue(name, out childNode))
                    return childNode;

                childNode = new Node {Name = name, Parent = this};
                Children.Add(name, childNode);
                return childNode;
            }

            #endregion

            public string XPath
            {
                get
                {
                    var stack = new Stack<string>();
                    for (var node = this; node.Parent != null; node = node.Parent)
                        stack.Push(node.Name);
                    return string.Join("/", stack.ToArray());
                }
            }
        }

        public enum AttributeType
        {
            None, String
        }
    }
}
