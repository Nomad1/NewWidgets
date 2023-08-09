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

        [WidgetParameter("width", typeof(float), UnitType.Length)]
        Width, // part of size, width and height are not inherited

        [WidgetParameter("height", typeof(float), UnitType.Length)]
        Height, // part of size,width and height are not inherited

        [Obsolete] // no longer used, only for compatibility
        [WidgetParameter("size", "size", typeof(Vector2), UnitType.Length, WidgetParameterInheritance.Initial,
                                         typeof(Vector2SplitProcessor), "width", "height")] // non CSS
        Size,

        [WidgetParameter("x", "x", typeof(float), UnitType.Length, WidgetParameterInheritance.Initial, typeof(DefaultProcessor), "left")] // Panorama UI, writes to left instead
        [WidgetParameter("left", typeof(float), UnitType.Length)] // CSS
        Left, // part of position

        [WidgetParameter("y", "y", typeof(float), UnitType.Length, WidgetParameterInheritance.Initial, typeof(DefaultProcessor), "top")] // Panorama UI, writes to top instead
        [WidgetParameter("top", typeof(float), UnitType.Length)] // CSS
        Top, // part of position
        [WidgetParameter("z", "z", typeof(int), UnitType.None, WidgetParameterInheritance.Initial, typeof(DefaultProcessor), "z-index")] // Panorama UI, writes to z-index instead
        [WidgetParameter("z-index", typeof(int), UnitType.None)] // CSS
        ZIndex,

        [Obsolete]
        [WidgetParameter("position", "position", typeof(Vector2), UnitType.Length, WidgetParameterInheritance.Initial,
                                                 typeof(Vector2SplitProcessor), "left", "top")] // split. No longer used
        Position,

        // Common
        [WidgetParameter("opacity", typeof(float), UnitType.Percent)]
        Opacity, // opacity is a special value that should not be inherited but multiplied with parent


        [Obsolete]
        [WidgetParameter("clip", "--clip", typeof(bool), UnitType.None, WidgetParameterInheritance.Initial, typeof(OverflowProcessor), "overflow")] // instead of `visible` and `hidden` we use `true` and `false`. It doesn't cover `scroll` option although
        Clip,
        [WidgetParameter("overflow", "overflow", typeof(WidgetOverflow))] // instead of `visible` and `hidden` we use `true` and `false`. It doesn't cover `scroll` option although
        Overflow,
        [WidgetParameter("clip_margin", "--clip-margin", typeof(Margin), UnitType.Length)] // clip margin is a Margin type, while CSS clip is a rect. We can't convert one to another
        ClipMargin,
        [WidgetParameter("padding", typeof(Margin), UnitType.Length)] // padding is of type Margin
        Padding,

        // Background

        [WidgetParameter("back_color", "background-color", typeof(uint), UnitType.Color)] // unlike HTML it doesn't supports transparency yet
        BackColor,
        [WidgetParameter("back_image", "background-image", typeof(string), UnitType.Url)]
        BackImage,
        [WidgetParameter("back_style", "background-repeat", typeof(WidgetBackgroundStyle))] // we have own repeat modes so this needs to be worked out
                                                                                            // TODO: another parameter that converts to one of our styles
        BackStyle,


        [WidgetParameter("back_depth", "--background-depth", typeof(WidgetBackgroundDepth))] // nothing like that in HTML
        BackDepth,
        [WidgetParameter("back_scale", "background-size", typeof(float), UnitType.Percent)] // right now its a single percentage value. TODO: another property to support two values and exact length
        BackScale,
        [WidgetParameter("back_angle", "--background-rotation", typeof(float))]
        BackAngle,
        [WidgetParameter("back_pivot", "background-position", typeof(Vector2), UnitType.Percent)] // pivot point. in CSS there are different constants and possibility to set xpos and ypos, it's only percents in our implementation
        BackPivot,
        [WidgetParameter("back_padding", "--background-padding", typeof(Margin), UnitType.Length)]
        BackPadding,
        [WidgetParameter("back_opacity", "background-color-opacity",  typeof(float), UnitType.Percent)] // Panorama UI compat, invalid in CSS
        BackOpacity,

        // Text

        [WidgetParameter("font", "font-family", typeof(Font), UnitType.None, WidgetParameterInheritance.Inherit)]
        Font,
        [WidgetParameter("font_size", "font-size", typeof(float), UnitType.FontUnits, WidgetParameterInheritance.Inherit)]
        FontSize,
        [WidgetParameter("text_color", "color", typeof(uint), UnitType.Color, WidgetParameterInheritance.Inherit)]
        TextColor,
        [WidgetParameter("line_spacing", "line-height", typeof(float), UnitType.Percent, WidgetParameterInheritance.Inherit)]
        LineSpacing,
        [WidgetParameter("text_align", "text-align", typeof(WidgetAlign), UnitType.None, WidgetParameterInheritance.Inherit)] // TODO: more alignment options
        TextAlign,
        //[WidgetParameter("text_padding", "--text-padding", typeof(Margin), UnitType.Length)] // changed to "padding"
        //TextPadding,
        [WidgetParameter("richtext", "--richtext", typeof(bool))]
        RichText,

        // Image // migrated to background styles

        //[WidgetParameter("image", typeof(string), UnitType.Url)] // image name
        //Image,
        //[WidgetParameter("image_style", "--image-style", typeof(WidgetBackgroundStyle))]
        //ImageStyle,
        //[WidgetParameter("image_angle", "--image-rotation", typeof(float))]
        //ImageAngle,
        //[WidgetParameter("image_pivot", "--image-position", typeof(Vector2), UnitType.Percent)]
        //ImagePivot,
        //[WidgetParameter("image_padding", "--image-padding", typeof(Margin), UnitType.Length)] // changed to "padding"
        //ImagePadding,
        //[WidgetParameter("image_color", "--image-color", typeof(uint), UnitType.Color)]
        //ImageColor,
        //[WidgetParameter("image_opacity", "--image-opacity", typeof(float), UnitType.Percent)]
        //ImageOpacity,

        // Text edit

        [WidgetParameter("cursor_color", "--cursor-color", typeof(uint), UnitType.Color)]
        CursorColor,
        [WidgetParameter("cursor_char", "--cursor_char")]
        CursorChar,
        [WidgetParameter("mask_char", "--mask_char")]
        MaskChar,


        // Button

        [WidgetParameter("button_layout", "--button-layout", typeof(WidgetButtonLayout))]
        ButtonLayout,
        //[WidgetParameter("button_image_padding", "--button-image-padding", typeof(Margin), UnitType.Length)]
        //ButtonImagePadding,
        //[WidgetParameter("button_text_padding", "--button-text-padding",  typeof(Margin), UnitType.Length)]
        //ButtonTextPadding,
        [WidgetParameter("button_animate_scale", "--button-animate-scale", typeof(float), UnitType.Percent)]
        ButtonAnimateScale,
        [WidgetParameter("button_animate_pivot", "--button-animate-pivot", typeof(Vector2), UnitType.Percent)]
        ButtonAnimatePivot,
        [WidgetParameter("button_animate_time", "--button-animate-time", typeof(int))]
        ButtonAnimateTime,

        // Font
        [WidgetParameter("font_resource", "--font-resource", typeof(string), UnitType.Url)]
        FontResource,
        [WidgetParameter("font_spacing", "--font-spacing", typeof(float))]
        FontSpacing,
        [WidgetParameter("font_shift", "--font-shift", typeof(int))]
        FontShift,
        [WidgetParameter("font_leading", "--font-leading", typeof(int))]
        FontLeading,
        [WidgetParameter("font_baseline", "--font-baseline", typeof(int))]
        FontBaseline,

        // Sprite
        [WidgetParameter("sprite_tile_x", "--sprite-tile-x", typeof(int))]
        SpriteTileX,
        [WidgetParameter("sprite_tile_y", "--sprite-tile-y", typeof(int))]
        SpriteTileY,

        Max
    }


    internal class DefaultProcessor : IParameterProcessor
    {
        private WidgetParameterIndex m_target;

        private Type m_type;
        private string m_targetName;
        private UnitType m_unitType;

        public void Init(string target, Type type, UnitType unitType, string[] parameters)
        {
            m_targetName = (parameters != null && parameters.Length >= 1) ? parameters[0] : target;
            m_type = type;
            m_unitType = unitType;
        }

        public void Process(IDictionary<WidgetParameterIndex, object> data, string stringValue)
        {
            if (m_target == 0)
            {
                m_target = WidgetParameterMap.GetIndexByName(m_targetName);
                Debug.Assert(m_target != 0);
            }

            object value = ConversionHelper.ParseValue(m_type, m_unitType, stringValue);

            data[m_target] = value;
        }
    }

    internal class Vector2SplitProcessor : IParameterProcessor
    {
        private string m_targetNameX;
        private string m_targetNameY;
        private WidgetParameterIndex m_targetX;
        private WidgetParameterIndex m_targetY;
        private UnitType m_unitType;

        public void Init(string target, Type type, UnitType unitType, string[] parameters)
        {
            Debug.Assert(parameters != null && parameters.Length == 2);

            m_unitType = unitType;

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

            Vector2 value = (Vector2)ConversionHelper.ParseValue(typeof(Vector2), m_unitType, stringValue);

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
        private UnitType m_unitType;

        public void Init(string target, Type type, UnitType unitType, string[] parameters)
        {
            Debug.Assert(parameters != null && parameters.Length == 3);

            m_unitType = unitType;

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

            Vector3 value = (Vector3)ConversionHelper.ParseValue(typeof(Vector3), m_unitType, stringValue);

            data[m_targetX] = value.X;
            data[m_targetY] = value.Y;
            data[m_targetZ] = value.Z;
        }
    }

    internal class OverflowProcessor : IParameterProcessor
    {
        private WidgetParameterIndex m_target;
        private string m_targetName;

        public void Init(string target, Type type, UnitType unitType, string[] parameters)
        {
            m_targetName = (parameters != null && parameters.Length >= 1) ? parameters[0] : target;
        }

        public void Process(IDictionary<WidgetParameterIndex, object> data, string stringValue)
        {
            if (m_target == 0)
            {
                m_target = WidgetParameterMap.GetIndexByName(m_targetName);
                Debug.Assert(m_target != 0);
            }

            bool value = (bool)ConversionHelper.ParseValue(typeof(bool), UnitType.None, stringValue);

            data[m_target] = value ? WidgetOverflow.Hidden : WidgetOverflow.Visible;
        }
    }


}

