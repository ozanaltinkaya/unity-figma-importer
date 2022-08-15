using System.Collections.Generic;
using UnityEngine;

namespace Cdm.Figma.UI
{
    [DisallowMultipleComponent]
    public class NodeObject : MonoBehaviour
    {
        [SerializeField]
        private string _nodeID;

        public string nodeID
        {
            get => _nodeID;
            internal set => _nodeID = value;
        }
        
        [SerializeField]
        private string _nodeName;

        public string nodeName
        {
            get => _nodeName;
            internal set => _nodeName = value;
        }
        
        [SerializeField]
        private string _nodeType;

        public string nodeType
        {
            get => _nodeType;
            internal set => _nodeType = value;
        }
        
        [SerializeField]
        private string _bindingKey;

        public string bindingKey
        {
            get => _bindingKey;
            internal set => _bindingKey = value;
        }

        public RectTransform rectTransform { get; private set; }
        
        [SerializeField]
        private List<Styles.Style> _styles = new List<Styles.Style>();

        public List<Styles.Style> styles => _styles;

        // TODO: Should be private or at least internal. Because it is only available while importing figma file.
        public Node node { get; private set; }
        
        /// <summary>
        /// Initializes a new instance of the XElement class with the specified <paramref name="node"/>.
        /// </summary>
        public static T Create<T>(Node node, NodeConvertArgs args) where T : NodeObject
        {
            var go = new GameObject(node.name);
            var nodeObject = go.AddComponent<T>();

            nodeObject.node = node;
            nodeObject.nodeID = node.id;
            nodeObject.nodeName = node.name;
            nodeObject.nodeType = node.type;
            nodeObject.rectTransform = nodeObject.gameObject.AddComponent<RectTransform>();
            nodeObject.bindingKey = node.GetBindingName();
            //nodeObject.localizationKey = node.GetLocalizationKey();
            
            if (node is SceneNode sceneNode)
            {
                nodeObject.gameObject.SetActive(sceneNode.visible);    
            }
            
            return nodeObject;
        }
    }
}