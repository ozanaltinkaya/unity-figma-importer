using System.Linq;
using Cdm.Figma.UI.Styles;
using Cdm.Figma.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Cdm.Figma.UI
{
    public class FrameNodeConverter : NodeConverter<FrameNode>
    {
        protected override NodeObject Convert(NodeObject parentObject, FrameNode frameNode, NodeConvertArgs args)
        {
            var nodeObject = NodeObject.NewNodeObject(frameNode, args);
            nodeObject.SetTransform(frameNode);

            // Frame node's parent may be a page so check if it is INodeTransform.
            if (frameNode.parent is INodeTransform parent)
            {
                nodeObject.SetLayoutConstraints(parent);
            }

            GenerateStyles(nodeObject, frameNode, args);

            nodeObject.ApplyStyles();
            
            AddLayoutComponentIfNeeded(nodeObject, frameNode);
            AddContentSizeFitterIfNeeded(nodeObject, frameNode);
            AddGridIfNeeded(nodeObject, frameNode);
            
            BuildChildren(frameNode, nodeObject, args);
            return nodeObject;
        }

        private static void GenerateStyles(NodeObject nodeObject, FrameNode node, NodeConvertArgs args)
        {
            if (node.fills.Any() || node.strokes.Any())
            {
                var options = new VectorImageUtils.SpriteOptions()
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    sampleCount = 8,
                    textureSize = 1024
                };

                // Multiple fill is not supported, only one image can be attached to the node object.
                var style = new ImageStyle();
                style.enabled = node.fills.Any(f => f.visible) || node.strokes.Any(s => s.visible);
                style.sprite.SetValue(VectorImageUtils.CreateSpriteFromRect(node, options));
                style.imageType.SetValue(Image.Type.Sliced);
                style.color.SetValue(new UnityEngine.Color(1f, 1f, 1f, node.opacity));
                nodeObject.styles.Add(style);
            }

            // Add mask if enabled.
            if (node.clipsContent)
            {
                var style = new MaskStyle();
                style.enabled = true;
                nodeObject.styles.Add(style);
            }
        }

        private static void BuildChildren(FrameNode currentNode, NodeObject nodeObject, NodeConvertArgs args)
        {
            var children = currentNode.children;
            if (children != null)
            {
                for (var child = 0; child < children.Length; child++)
                {
                    if (args.importer.TryConvertNode(nodeObject, children[child], args, out var childObject))
                    {
                        if (currentNode.layoutMode != LayoutMode.None)
                        {
                            childObject.gameObject.AddComponent<LayoutElement>();
                            HandleFillContainer(currentNode.layoutMode, nodeObject, childObject);
                        }

                        childObject.rectTransform.SetParent(nodeObject.rectTransform, false);
                        childObject.AdjustPosition(currentNode.size);
                    }
                }
            }
        }

        private static void HandleFillContainer(LayoutMode layoutMode, NodeObject nodeObject, NodeObject childElement)
        {
            INodeLayout childLayout = (INodeLayout)childElement.node;
            INodeTransform childTransform = (INodeTransform)childElement.node;

            if (childLayout.layoutAlign == LayoutAlign.Stretch)
            {
                if (layoutMode == LayoutMode.Horizontal)
                {
                    nodeObject.GetComponent<HorizontalLayoutGroup>().childControlHeight = true;
                    childElement.gameObject.GetComponent<LayoutElement>().flexibleHeight = 1;
                }
                else if (layoutMode == LayoutMode.Vertical)
                {
                    nodeObject.GetComponent<VerticalLayoutGroup>().childControlWidth = true;
                    childElement.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1;
                }
            }
            else
            {
                if (layoutMode == LayoutMode.Horizontal)
                {
                    nodeObject.GetComponent<HorizontalLayoutGroup>().childControlHeight = true;
                    childElement.gameObject.GetComponent<LayoutElement>().minHeight = childTransform.size.y;
                }
                else
                {
                    nodeObject.GetComponent<VerticalLayoutGroup>().childControlWidth = true;
                    childElement.gameObject.GetComponent<LayoutElement>().minWidth = childTransform.size.x;
                }
            }

            if (childLayout.layoutGrow.HasValue && childLayout.layoutGrow != 0)
            {
                if (layoutMode == LayoutMode.Horizontal)
                {
                    nodeObject.GetComponent<HorizontalLayoutGroup>().childControlWidth = true;
                    childElement.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1;
                    childElement.gameObject.GetComponent<LayoutElement>().minWidth = 1;
                }
                else if (layoutMode == LayoutMode.Vertical)
                {
                    nodeObject.GetComponent<VerticalLayoutGroup>().childControlHeight = true;
                    childElement.gameObject.GetComponent<LayoutElement>().flexibleHeight = 1;
                    childElement.gameObject.GetComponent<LayoutElement>().minHeight = 1;
                }
            }
            else
            {
                if (layoutMode == LayoutMode.Horizontal)
                {
                    nodeObject.GetComponent<HorizontalLayoutGroup>().childControlWidth = true;
                    childElement.gameObject.GetComponent<LayoutElement>().minWidth = childTransform.size.x;
                }
                else
                {
                    nodeObject.GetComponent<VerticalLayoutGroup>().childControlHeight = true;
                    childElement.gameObject.GetComponent<LayoutElement>().minHeight = childTransform.size.y;
                }
            }
        }

        private static void AddContentSizeFitterIfNeeded(NodeObject nodeObject, FrameNode groupNode)
        {
            if (groupNode.layoutMode == LayoutMode.None)
                return;

            if (groupNode.primaryAxisSizingMode == AxisSizingMode.Auto ||
                groupNode.counterAxisSizingMode == AxisSizingMode.Auto)
            {
                nodeObject.gameObject.AddComponent<ContentSizeFitter>();
            }

            if (groupNode.primaryAxisSizingMode == AxisSizingMode.Auto)
            {
                if (groupNode.layoutMode == LayoutMode.Horizontal)
                {
                    nodeObject.gameObject.GetComponent<ContentSizeFitter>().horizontalFit =
                        ContentSizeFitter.FitMode.PreferredSize;
                }
                else
                {
                    nodeObject.gameObject.GetComponent<ContentSizeFitter>().verticalFit =
                        ContentSizeFitter.FitMode.PreferredSize;
                }
            }

            if (groupNode.counterAxisSizingMode == AxisSizingMode.Auto)
            {
                if (groupNode.layoutMode == LayoutMode.Horizontal)
                {
                    nodeObject.gameObject.GetComponent<ContentSizeFitter>().verticalFit =
                        ContentSizeFitter.FitMode.PreferredSize;
                }
                else
                {
                    nodeObject.gameObject.GetComponent<ContentSizeFitter>().horizontalFit =
                        ContentSizeFitter.FitMode.PreferredSize;
                }
            }
        }

        private static void AddLayoutComponentIfNeeded(NodeObject nodeObject, FrameNode groupNode)
        {
            var layoutMode = groupNode.layoutMode;
            if (layoutMode == LayoutMode.None)
                return;

            HorizontalOrVerticalLayoutGroup layoutGroup = null;

            if (layoutMode == LayoutMode.Horizontal)
            {
                layoutGroup = nodeObject.gameObject.AddComponent<HorizontalLayoutGroup>();

                if (groupNode.primaryAxisAlignItems == PrimaryAxisAlignItems.Min)
                {
                    if (groupNode.counterAxisAlignItems == CounterAxisAlignItems.Min)
                    {
                        layoutGroup.childAlignment = TextAnchor.UpperLeft;
                    }
                    else if (groupNode.counterAxisAlignItems == CounterAxisAlignItems.Max)
                    {
                        layoutGroup.childAlignment = TextAnchor.LowerLeft;
                    }
                    else if (groupNode.counterAxisAlignItems == CounterAxisAlignItems.Center)
                    {
                        layoutGroup.childAlignment = TextAnchor.MiddleLeft;
                    }
                }
                else if (groupNode.primaryAxisAlignItems == PrimaryAxisAlignItems.Max)
                {
                    if (groupNode.counterAxisAlignItems == CounterAxisAlignItems.Min)
                    {
                        layoutGroup.childAlignment = TextAnchor.UpperRight;
                    }
                    else if (groupNode.counterAxisAlignItems == CounterAxisAlignItems.Max)
                    {
                        layoutGroup.childAlignment = TextAnchor.LowerRight;
                    }
                    else if (groupNode.counterAxisAlignItems == CounterAxisAlignItems.Center)
                    {
                        layoutGroup.childAlignment = TextAnchor.MiddleRight;
                    }
                }
                else if (groupNode.primaryAxisAlignItems == PrimaryAxisAlignItems.Center)
                {
                    if (groupNode.counterAxisAlignItems == CounterAxisAlignItems.Min)
                    {
                        layoutGroup.childAlignment = TextAnchor.UpperCenter;
                    }
                    else if (groupNode.counterAxisAlignItems == CounterAxisAlignItems.Max)
                    {
                        layoutGroup.childAlignment = TextAnchor.LowerCenter;
                    }
                    else if (groupNode.counterAxisAlignItems == CounterAxisAlignItems.Center)
                    {
                        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
                    }
                }
            }
            else
            {
                layoutGroup = nodeObject.gameObject.AddComponent<VerticalLayoutGroup>();

                if (groupNode.primaryAxisAlignItems == PrimaryAxisAlignItems.Min)
                {
                    if (groupNode.counterAxisAlignItems == CounterAxisAlignItems.Min)
                    {
                        layoutGroup.childAlignment = TextAnchor.UpperLeft;
                    }
                    else if (groupNode.counterAxisAlignItems == CounterAxisAlignItems.Max)
                    {
                        layoutGroup.childAlignment = TextAnchor.UpperRight;
                    }
                    else if (groupNode.counterAxisAlignItems == CounterAxisAlignItems.Center)
                    {
                        layoutGroup.childAlignment = TextAnchor.UpperCenter;
                    }
                }
                else if (groupNode.primaryAxisAlignItems == PrimaryAxisAlignItems.Max)
                {
                    if (groupNode.counterAxisAlignItems == CounterAxisAlignItems.Min)
                    {
                        layoutGroup.childAlignment = TextAnchor.LowerLeft;
                    }
                    else if (groupNode.counterAxisAlignItems == CounterAxisAlignItems.Max)
                    {
                        layoutGroup.childAlignment = TextAnchor.LowerRight;
                    }
                    else if (groupNode.counterAxisAlignItems == CounterAxisAlignItems.Center)
                    {
                        layoutGroup.childAlignment = TextAnchor.LowerCenter;
                    }
                }
                else if (groupNode.primaryAxisAlignItems == PrimaryAxisAlignItems.Center)
                {
                    if (groupNode.counterAxisAlignItems == CounterAxisAlignItems.Min)
                    {
                        layoutGroup.childAlignment = TextAnchor.MiddleLeft;
                    }
                    else if (groupNode.counterAxisAlignItems == CounterAxisAlignItems.Max)
                    {
                        layoutGroup.childAlignment = TextAnchor.MiddleRight;
                    }
                    else if (groupNode.counterAxisAlignItems == CounterAxisAlignItems.Center)
                    {
                        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
                    }
                }
            }

            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childScaleWidth = false;
            layoutGroup.childScaleHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            // Set padding.
            nodeObject.GetComponent<LayoutGroup>().padding = new RectOffset(
                (int)groupNode.paddingLeft,
                (int)groupNode.paddingRight,
                (int)groupNode.paddingTop,
                (int)groupNode.paddingBottom);

            // Set spacing.
            layoutGroup.spacing = groupNode.itemSpacing;
        }

        private static void AddGridIfNeeded(NodeObject nodeObject, FrameNode frameNode)
        {
            if (frameNode.layoutGrids.Count == 2)
            {
                var gridView = nodeObject.gameObject.AddComponent<GridLayoutGroup>();
                foreach (var grid in frameNode.layoutGrids)
                {
                    if (grid.pattern == Pattern.Columns)
                    {
                        gridView.spacing = new Vector2(grid.gutterSize, gridView.spacing.y);
                        gridView.padding.left = (int) grid.offset;
                        gridView.padding.right = (int) grid.offset;
                        gridView.cellSize = new Vector2(frameNode.size.x-((grid.count-1)*grid.gutterSize+2*grid.offset), gridView.cellSize.y);
                    }
                    
                    else if (grid.pattern == Pattern.Rows)
                    {
                        gridView.spacing = new Vector2(gridView.spacing.x, grid.gutterSize);
                        gridView.padding.top = (int) grid.offset;
                        gridView.padding.bottom = (int) grid.offset;
                        gridView.cellSize = new Vector2(gridView.cellSize.x, frameNode.size.y-((grid.count-1)*grid.gutterSize+2*grid.offset));
                    }
                }
            }
        }
    }
}