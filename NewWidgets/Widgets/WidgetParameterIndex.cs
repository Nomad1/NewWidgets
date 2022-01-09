using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    // List of default parameters. We need to use it as Enum to get fast access instead of dictionary search
    internal enum WidgetParameterIndex
    {
        // Invalid
        None,

        // Size and position

        [WidgetParameter("width", typeof(float))]
        Width, // part of size, width and height are not inherited

        [WidgetParameter("height", typeof(float))]
        Height, // part of size,width and height are not inherited

        [Obsolete] // no longer used, only for compatibility
        [WidgetParameter("size", "size", typeof(Vector2), WidgetParameterInheritance.Initial,
                                         typeof(Vector2SplitProcessor), "width", "height")] // non CSS
        Size,

        [WidgetParameter("x", "x", typeof(float), WidgetParameterInheritance.Initial, typeof(DefaultProcessor), "left")] // Panorama UI, writes to left instead
        [WidgetParameter("left", typeof(float))] // CSS
        Left, // part of position

        [WidgetParameter("y", "y", typeof(float), WidgetParameterInheritance.Initial, typeof(DefaultProcessor), "top")] // Panorama UI, writes to top instead
        [WidgetParameter("top", typeof(float))] // CSS
        Top, // part of position
        [WidgetParameter("z", "z", typeof(float), WidgetParameterInheritance.Initial, typeof(DefaultProcessor), "z-index")] // Panorama UI, writes to z-index instead
        [WidgetParameter("z-index", typeof(float))] // CSS. TODO: change to int
        ZIndex,

        [Obsolete]
        [WidgetParameter("position", "position", typeof(Vector3), WidgetParameterInheritance.Initial,
                                                 typeof(Vector3SplitProcessor), "left", "top", "z-index")] // split
        Position,

        // Common
        [WidgetParameter("opacity", typeof(float))]
        Opacity, // opacity is a special value that should not be inherited but multiplied with parent


        [WidgetParameter("clip", "overflow", typeof(bool))] // instead of `visible` and `hidden` we use `true` and `false`. It doesn't cover `scroll` option although
        Clip,
        [WidgetParameter("clip_margin", "clip", typeof(Margin))] // clip margin is a Margin type, not a rect. TODO:
        ClipMargin,
        [WidgetParameter("padding", typeof(Margin))] // padding is of type Margin
        Padding,

        // Background

        [WidgetParameter("back_color", "background-color", typeof(uint))] // unlike HTML it doesn't supports transparency yet. TODO: wrapper type for Color
        BackColor,
        [WidgetParameter("back_image", "background-image", typeof(string))]
        BackImage,
        [WidgetParameter("back_style", "background-repeat", typeof(WidgetBackgroundStyle))] // we have own repeat modes so this needs to be worked out
                                                                                            // TODO: another parameter that converts to one of our styles
        BackStyle,


        [WidgetParameter("back_depth", "-nw-back-depth", typeof(WidgetBackgroundDepth))] // nothing like that in HTML
        BackDepth,
        [WidgetParameter("back_scale", "background-size", typeof(float))] // right now its a single percentage value. TODO: another property to support two values and exact length
        BackScale,
        [WidgetParameter("back_angle", "-nw-back-angle", typeof(float))]
        BackAngle,
        [WidgetParameter("back_pivot", "-nw-back-pivot", typeof(Vector2))] // pivot + padding are powerful but in CSS there is only background-origin, TODO: implement it
        BackPivot,
        [WidgetParameter("back_padding", "-nw-back-padding", typeof(Margin))]
        BackPadding,
        [WidgetParameter("back_opacity", "background-color-opacity",  typeof(float))] // Panorama UI compat
        BackOpacity,

        // Text

        [WidgetParameter("font", "font-family", typeof(Font), WidgetParameterInheritance.Inherit)]
        Font,
        [WidgetParameter("font_size", "font-size", typeof(float), WidgetParameterInheritance.Inherit)]
        FontSize,
        [WidgetParameter("text_color", "color", typeof(uint), WidgetParameterInheritance.Inherit)]
        TextColor,
        [WidgetParameter("line_spacing", "line-height", typeof(float), WidgetParameterInheritance.Inherit)]
        LineSpacing,
        [WidgetParameter("text_align", "text-align", typeof(WidgetAlign), WidgetParameterInheritance.Inherit)]
        TextAlign,
        [WidgetParameter("text_padding", "-nw-text-padding", typeof(Margin))]
        TextPadding,
        [WidgetParameter("richtext", "-nw-richtext", typeof(bool))]
        RichText,

        // Image

        [WidgetParameter("image")] // image name
        Image,
        [WidgetParameter("image_style", "-nw-image_style", typeof(WidgetBackgroundStyle))]
        ImageStyle,
        [WidgetParameter("image_angle", "-nw-image_angle", typeof(float))]
        ImageAngle,
        [WidgetParameter("image_pivot", "-nw-image_pivot", typeof(Vector2))]
        ImagePivot,
        [WidgetParameter("image_padding", "-nw-image-padding", typeof(Margin))] // change to "padding"?
        ImagePadding,
        [WidgetParameter("image_color", "-nw-image-color", typeof(uint))]
        ImageColor,
        [WidgetParameter("image_opacity", "-nw-image-opacity", typeof(float))]
        ImageOpacity,

        // Text edit

        [WidgetParameter("cursor_color", "-nw-cursor-color", typeof(uint))]
        CursorColor,
        [WidgetParameter("cursor_char", "-nw-cursor_char")]
        CursorChar,
        [WidgetParameter("mask_char", "-nw-mask_char")]
        MaskChar,


        // Button

        [WidgetParameter("button_layout", "-nw-button-layout",  typeof(WidgetButtonLayout))]
        ButtonLayout,
        [WidgetParameter("button_image_padding", "-nw-button-image-padding", typeof(Margin))]
        ButtonImagePadding,
        [WidgetParameter("button_text_padding", "-nw-button-text-padding",  typeof(Margin))]
        ButtonTextPadding,
        [WidgetParameter("button_animate_scale", "-nw-button-animate-scale", typeof(float))]
        ButtonAnimateScale,
        [WidgetParameter("button_animate_pivot", "-nw-button-animate-pivot", typeof(Vector2))]
        ButtonAnimatePivot,
        [WidgetParameter("button_animate_time", "-nw-button-animate-time", typeof(int))]
        ButtonAnimateTime,

        Max = ButtonAnimateTime + 1
    }


    internal class DefaultProcessor : IParameterProcessor
    {
        private WidgetParameterIndex m_target;

        private Type m_type;
        private string m_targetName;

        public void Init(string target, Type type, string [] parameters)
        {
            m_targetName = (parameters != null && parameters.Length >= 1) ? parameters[0] : target;
            m_type = type;
        }

        public void Process(IDictionary<WidgetParameterIndex, object> data, string stringValue)
        {
            if (m_target == 0)
            {
                m_target = WidgetParameterMap.GetIndexByName(m_targetName);
                Debug.Assert(m_target != 0);
            }

            object value = ConversionHelper.ParseValue(m_type, stringValue);

            data[m_target] = value;
        }
    }

    internal class Vector2SplitProcessor : IParameterProcessor
    {
        private string m_targetNameX;
        private string m_targetNameY;
        private WidgetParameterIndex m_targetX;
        private WidgetParameterIndex m_targetY;

        public void Init(string target, Type type, string[] parameters)
        {
            Debug.Assert(parameters != null && parameters.Length == 2);

            m_targetNameX = parameters[0];
            m_targetNameY = parameters[1];
        }

        public void Process(IDictionary<WidgetParameterIndex, object> data, string stringValue)
        {
            if (m_targetX == 0)
            {
                m_targetX = WidgetParameterMap.GetIndexByName(m_targetNameX);
                Debug.Assert(m_targetX != 0);

                m_targetY = WidgetParameterMap.GetIndexByName(m_targetNameY);
                Debug.Assert(m_targetY != 0);
            }

            Vector2 value = (Vector2)ConversionHelper.ParseValue(typeof(Vector2), stringValue);

            data[m_targetX] = value.X;
            data[m_targetY] = value.Y;
        }
    }

    internal class Vector3SplitProcessor : IParameterProcessor
    {
        private string m_targetNameX;
        private string m_targetNameY;
        private string m_targetNameZ;
        private WidgetParameterIndex m_targetX;
        private WidgetParameterIndex m_targetY;
        private WidgetParameterIndex m_targetZ;

        public void Init(string target, Type type, string[] parameters)
        {
            Debug.Assert(parameters != null && parameters.Length == 3);

            m_targetNameX = parameters[0];
            m_targetNameY = parameters[1];
            m_targetNameZ = parameters[2];
        }

        public void Process(IDictionary<WidgetParameterIndex, object> data, string stringValue)
        {
            if (m_targetX == 0)
            {
                m_targetX = WidgetParameterMap.GetIndexByName(m_targetNameX);
                Debug.Assert(m_targetX != 0);

                m_targetY = WidgetParameterMap.GetIndexByName(m_targetNameY);
                Debug.Assert(m_targetY != 0);

                m_targetZ = WidgetParameterMap.GetIndexByName(m_targetNameZ);
                Debug.Assert(m_targetZ != 0);
            }

            Vector3 value = (Vector3)ConversionHelper.ParseValue(typeof(Vector3), stringValue);

            data[m_targetX] = value.X;
            data[m_targetY] = value.Y;
            data[m_targetZ] = value.Z;
        }
    }

}

