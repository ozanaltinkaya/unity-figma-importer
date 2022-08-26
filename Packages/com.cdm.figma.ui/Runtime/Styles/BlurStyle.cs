﻿using Cdm.Figma.UI.Styles.Properties;
using Cdm.Figma.UI.Utils;
using UnityEngine;

namespace Cdm.Figma.UI.Styles
{
    public class BlurStyle : Style
    {
        public StylePropertyBool visible = new StylePropertyBool(true);
        public StylePropertyFloat radius = new StylePropertyFloat(4f);
        public StylePropertyBlurType type = new StylePropertyBlurType(BlurType.Layer);

        public override void SetStyle(GameObject gameObject, StyleArgs args)
        {
            var blur = gameObject.GetOrAddComponent<BlurBehaviour>();
            if (blur != null)
            {
                if (visible.enabled)
                    blur.enabled = visible.value;

                if (radius.enabled)
                    blur.radius = radius.value;

                if (type.enabled)
                    blur.type = type.value;
            }
        }

        public override void SetStyleAsSelector(GameObject gameObject, StyleArgs args)
        {
            SetStyleAsSelector<BlurStyleSetter>(gameObject, args);
        }

        protected override void MergeTo(Style other, bool force)
        {
            if (other is BlurStyle otherStyle)
            {
                OverwriteProperty(visible, otherStyle.visible, force);
                OverwriteProperty(radius, otherStyle.radius, force);
                OverwriteProperty(type, otherStyle.type, force);
            }
        }
    }
}