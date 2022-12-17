using System.Linq;
using Cdm.Figma.UI.Styles;
using Cdm.Figma.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Cdm.Figma.UI
{
    public class VectorConvertArgs
    {
        public Sprite sourceSprite { get; set; }
        public bool generateSprite { get; set; } = true;
    }

    public abstract class VectorNodeConverter<TNode, TFigmaNode> : NodeConverter<TNode> 
        where TNode : VectorNode 
        where TFigmaNode : FigmaNode
    {
        protected override FigmaNode Convert(FigmaNode parentObject, TNode vectorNode, NodeConvertArgs args)
        {
            var figmaNode = Convert(parentObject, vectorNode, args, new VectorConvertArgs());
            if (figmaNode != null && vectorNode.isMask)
            {
                args.importer.LogWarning("Vector node with mask is not supported.", figmaNode);
            }
            return figmaNode;
        }
        
        protected FigmaNode Convert(FigmaNode parentObject, TNode vectorNode, NodeConvertArgs args,
            VectorConvertArgs vectorConvertArgs)
        {
            var figmaNode = args.importer.CreateFigmaNode<TFigmaNode>(vectorNode);
            figmaNode.SetTransform(vectorNode);

            // Every vector's parent will ALWAYS be INodeTransform
            figmaNode.SetLayoutConstraints((INodeTransform)vectorNode.parent);

            GenerateStyles(figmaNode, vectorNode, args, vectorConvertArgs);

            figmaNode.ApplyStyles();
            return figmaNode;
        }

        private void GenerateStyles(FigmaNode nodeObject, TNode vectorNode, NodeConvertArgs args,
            VectorConvertArgs vectorConvertArgs)
        {
            if (vectorConvertArgs.generateSprite)
            {
                if ((vectorNode.fills.Any() || vectorNode.strokes.Any()))
                {
                    if (vectorConvertArgs.sourceSprite == null)
                    {
                        if (!args.importer.generatedAssets.TryGet<Sprite>(vectorNode.id, out var sprite))
                        {
                            sprite = NodeSpriteGenerator.GenerateSprite(
                                vectorNode, SpriteGenerateType.Path, args.importer.spriteOptions);
                            if (sprite != null)
                            {
                                args.importer.generatedAssets.Add(vectorNode.id, sprite);
                                args.importer.generatedAssets.Add(vectorNode.id, sprite.texture);
                            }
                        }

                        vectorConvertArgs.sourceSprite = sprite;
                    }
                }

                var style = new ImageStyle();
                style.componentEnabled.enabled = true;
                style.componentEnabled.value = vectorConvertArgs.sourceSprite != null;

                style.sprite.enabled = true;
                style.sprite.value = vectorConvertArgs.sourceSprite;

                style.imageType.enabled = true;
                style.imageType.value = vectorNode is INodeRect ? Image.Type.Sliced : Image.Type.Simple;
                nodeObject.styles.Add(style);
            }

            {
                var style = new CanvasGroupStyle();
                style.alpha.enabled = true;
                style.alpha.value = vectorNode.opacity;
                nodeObject.styles.Add(style);
            }

            StyleGeneratorHelper.GenerateEffectsStyles(nodeObject, vectorNode.effects);
        }
    }

    public class VectorNodeConverter : VectorNodeConverter<VectorNode, FigmaNode>
    {
    }
}