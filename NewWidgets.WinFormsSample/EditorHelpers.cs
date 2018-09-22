using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using RunServer.Common;
using System.Drawing;

namespace SpaceAdventure.Content
{
    public static class GameTime
    {
        public static long Now { get { return Environment.TickCount; } }
    }
    
    [TypeConverter(typeof(StringConverter))]
    public class EditableString
    {
        private string m_value;

        public EditableString()
        {

        }

        public EditableString(string value)
        {
            m_value = value;
        }

        public static implicit operator string(EditableString str)
        {
            return str.m_value;
        }

        public static implicit operator EditableString(string str)
        {
            return new EditableString(str);
        }

        public override string ToString()
        {
            return m_value;
        }
    }

    public class Vector2Converter : TypeConverter
    {
        /// <summary>
        /// Creates a new instance of Vector2Converter
        /// </summary>
        public Vector2Converter()
        {
        }

        /// <summary>
        /// Boolean, true if the source type is a string
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;
            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Converts the specified string into a Vector2
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string)
            {
                try
                {
                    string s = (string)value;
                    string[] converterParts = s.Split(';');
                    float x = 0;
                    float y = 0;
                    if (converterParts.Length > 1)
                    {
                        x = float.Parse(converterParts[0].Trim().Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
                        y = float.Parse(converterParts[1].Trim().Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else if (converterParts.Length == 1)
                    {
                        x = float.Parse(converterParts[0].Trim().Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
                        y = 0;
                    }
                    else
                    {
                        x = 0F;
                        y = 0F;
                    }
                    return new Vector2(x, y);
                }
                catch
                {
                    //throw new ArgumentException("Cannot convert [" + value.ToString() + "] to pointF");
                    //fall back to old value
                    return context.Instance;
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Converts the PointF into a string
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (value is Vector2)
                {
                    Vector2 pt = (Vector2)value;
                    return string.Format("{0}; {1}", pt.X, pt.Y);
                }
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    [Editor(typeof(EmptyClassEditor), typeof(UITypeEditor))]
    public class EditableList<T> : ICustomTypeDescriptor
    {
        private readonly List<T> m_list = new List<T>();

        public EditableList()
        {
        }

        public EditableList(IEnumerable<T> collection)
        {
            m_list.AddRange(collection);
        }

        public void AddRange(IEnumerable<T> collection)
        {
            m_list.AddRange(collection);
        }

        [Browsable(false)]
        public IList<T> List
        {
            get { return m_list; }
        }

        [Browsable(false)]
        public T this[int index]
        {
            get { return m_list[index]; }
            set { m_list[index] = value; }
        }

        [Browsable(false)]
        public int Count
        {
            get { return m_list.Count; }
        }

        [DisplayName("[0]")]
        [NotNullCondition]
        [ReadOnly(false)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public object Item0
        {
            get { return Count > 0 ? (object)this[0] : null; }
        }

        [DisplayName("[1]")]
        [NotNullCondition]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public object Item1
        {
            get { return Count > 1 ? (object)this[1] : null; }
        }

        [DisplayName("[2]")]
        [NotNullCondition]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public object Item2
        {
            get { return Count > 2 ? (object)this[2] : null; }
        }

        [DisplayName("[3]")]
        [NotNullCondition]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public object Item3
        {
            get { return Count > 3 ? (object)this[3] : null; }
        }

        [DisplayName("[4]")]
        [NotNullCondition]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public object Item4
        {
            get { return Count > 4 ? (object)this[4] : null; }
        }

        [DisplayName("[5]")]
        [NotNullCondition]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public object Item5
        {
            get { return Count > 5 ? (object)this[5] : null; }
        }

        [DisplayName("[6]")]
        [NotNullCondition]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public object Item6
        {
            get { return Count > 6 ? (object)this[6] : null; }
        }

        [DisplayName("[7]")]
        [NotNullCondition]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public object Item7
        {
            get { return Count > 7 ? (object)this[7] : null; }
        }

        [DisplayName("[8]")]
        [NotNullCondition]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public object Item8
        {
            get { return Count > 8 ? (object)this[8] : null; }
        }

        [DisplayName("[9]")]
        [NotNullCondition]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public object Item9
        {
            get { return Count > 9 ? (object)this[9] : null; }
        }

        public override string ToString()
        {
            return string.Format("({0} items)", Count);
        }

        #region ICustomTypeDescriptor Members

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return DynamicConditionHelper.GetFilteredProperties(this, attributes);
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return DynamicConditionHelper.GetFilteredProperties(this, new Attribute[0]);
        }

        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        #endregion

    }

    public class EmptyClassEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            var property = context.GetType().GetProperty("Expanded");

            if (property != null)
                property.SetValue(context, property.GetValue(context, null).Equals(false), null);

            return base.EditValue(context, provider, value);
        }
    }

    #region Conditions

    public struct DynamicCondition
    {
        private readonly string m_targetProperty;
        private readonly object m_targetValue;
        private readonly bool m_invert;
        private readonly bool m_flags;

        public bool Flags
        {
            get { return m_flags; }
        }

        public bool Invert
        {
            get { return m_invert; }
        }

        public string TargetProperty
        {
            get { return m_targetProperty; }
        }

        public object TargetValue
        {
            get { return m_targetValue; }
        }

        public DynamicCondition(string targetProperty, object targetValue, bool invert, bool flags)
        {
            m_targetProperty = targetProperty;
            m_targetValue = targetValue;
            m_invert = invert;
            m_flags = flags;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class DynamicConditionAttribute : Attribute
    {
        private readonly DynamicCondition[] m_conditions;

        public DynamicCondition[] Conditions
        {
            get { return m_conditions; }
        }

        public DynamicConditionAttribute(string targetProperty, object targetValue)
            : this(targetProperty, targetValue, false, false)
        {
        }

        public DynamicConditionAttribute(string targetProperty, object targetValue, bool invert, bool flags)
        {
            m_conditions = new[] { new DynamicCondition(targetProperty, targetValue, invert, flags) };
        }

        public DynamicConditionAttribute(params object[] conditions)
        {
            m_conditions = new DynamicCondition[conditions.Length / 4];
            for (int i = 0; i < conditions.Length / 4; i++)
            {
                m_conditions[i] = new DynamicCondition((string)conditions[i * 4], conditions[i * 4 + 1], (bool)conditions[i * 4 + 2],
                                                (bool)conditions[i * 4 + 3]);
            }
        }
    }

    public static class DynamicConditionHelper
    {
        /// <exception cref="Exception"><c>Exception</c>.</exception>
        public static PropertyDescriptorCollection GetFilteredProperties(object obj, Attribute[] attributes)
        {
            PropertyDescriptorCollection collection = TypeDescriptor.GetProperties(obj, attributes, true);

            PropertyDescriptorCollection result = new PropertyDescriptorCollection(null);

            foreach (PropertyDescriptor pd in collection)
            {
                int checks = 0;
                int conditions = 0;

                foreach (Attribute attribute in pd.Attributes)
                {
                    DynamicConditionAttribute dc = attribute as DynamicConditionAttribute;

                    if (dc != null)
                    {
                        foreach (DynamicCondition condition in dc.Conditions)
                        {
                            conditions++;

                            PropertyDescriptor descriptor = collection[condition.TargetProperty];

                            if (descriptor == null)
                                throw new Exception(string.Format("Target Property {0} not found in element type {1}",
                                                                  condition.TargetProperty, obj.GetType()));

                            int compareResult;
                            if (condition.Flags)
                            {
                                int value = Convert.ToInt32(descriptor.GetValue(obj));
                                compareResult = (value & Convert.ToInt32(condition.TargetValue)) == 0 ? 1 : 0;
                            }
                            else
                                compareResult = Comparer.Default.Compare(descriptor.GetValue(obj), condition.TargetValue);

                            if (condition.Invert ? compareResult != 0 : compareResult == 0)
                                checks++;
                        }
                    }

                    NotNullConditionAttribute nc = attribute as NotNullConditionAttribute;

                    if (nc != null)
                    {
                        conditions++;

                        if (pd.GetValue(obj) != null)
                        {
                            checks++;
                        }
                    }
                }

                if (conditions == checks)
                    result.Add(pd);
            }

            return result;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class NotNullConditionAttribute : Attribute
    {
    }

    #endregion


    #region enum editor
    public class FlagCheckedListBox : CheckedListBox
    {
        private System.ComponentModel.Container m_components = null;
        private Type m_enumType;
        private Enum m_enumValue;


        public FlagCheckedListBox()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            // TODO: Add any initialization after the InitForm call

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_components != null)
                    m_components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code
        private void InitializeComponent()
        {
            // 
            // FlaggedCheckedListBox
            // 
            this.CheckOnClick = true;

        }
        #endregion

        // Adds an integer value and its associated description
        public FlagCheckedListBoxItem Add(int v, string c)
        {
            FlagCheckedListBoxItem item = new FlagCheckedListBoxItem(v, c);
            Items.Add(item);
            return item;
        }

        public FlagCheckedListBoxItem Add(FlagCheckedListBoxItem item)
        {
            Items.Add(item);
            return item;
        }

        protected override void OnItemCheck(ItemCheckEventArgs e)
        {
            base.OnItemCheck(e);

            if (isUpdatingCheckStates)
                return;

            // Get the checked/unchecked item
            FlagCheckedListBoxItem item = Items[e.Index] as FlagCheckedListBoxItem;
            // Update other items
            UpdateCheckedItems(item, e.NewValue);
        }

        // Checks/Unchecks items depending on the give bitvalue
        protected void UpdateCheckedItems(int value)
        {

            isUpdatingCheckStates = true;

            // Iterate over all items
            for (int i = 0; i < Items.Count; i++)
            {
                FlagCheckedListBoxItem item = Items[i] as FlagCheckedListBoxItem;

                if (item.value == 0)
                {
                    SetItemChecked(i, value == 0);
                }
                else
                {

                    // If the bit for the current item is on in the bitvalue, check it
                    if ((item.value & value) == item.value && item.value != 0)
                        SetItemChecked(i, true);
                    // Otherwise uncheck it
                    else
                        SetItemChecked(i, false);
                }
            }

            isUpdatingCheckStates = false;

        }

        // Updates items in the checklistbox
        // composite = The item that was checked/unchecked
        // cs = The check state of that item
        protected void UpdateCheckedItems(FlagCheckedListBoxItem composite, CheckState cs)
        {

            // If the value of the item is 0, call directly.
            if (composite.value == 0)
                UpdateCheckedItems(0);


            // Get the total value of all checked items
            int sum = 0;
            for (int i = 0; i < Items.Count; i++)
            {
                FlagCheckedListBoxItem item = Items[i] as FlagCheckedListBoxItem;

                // If item is checked, add its value to the sum.
                if (GetItemChecked(i))
                    sum |= item.value;
            }

            // If the item has been unchecked, remove its bits from the sum
            if (cs == CheckState.Unchecked)
                sum = sum & (~composite.value);
            // If the item has been checked, combine its bits with the sum
            else
                sum |= composite.value;

            // Update all items in the checklistbox based on the final bit value
            UpdateCheckedItems(sum);

        }

        private bool isUpdatingCheckStates = false;

        // Gets the current bit value corresponding to all checked items
        public int GetCurrentValue()
        {
            int sum = 0;

            for (int i = 0; i < Items.Count; i++)
            {
                FlagCheckedListBoxItem item = Items[i] as FlagCheckedListBoxItem;

                if (GetItemChecked(i))
                    sum |= item.value;
            }

            return sum;
        }

        // Adds items to the checklistbox based on the members of the enum
        private void FillEnumMembers()
        {
            foreach (string name in Enum.GetNames(m_enumType))
            {
                object val = Enum.Parse(m_enumType, name);
                int intVal = (int)Convert.ChangeType(val, typeof(int));

                Add(intVal, name);
            }
        }

        // Checks/unchecks items based on the current value of the enum variable
        private void ApplyEnumValue()
        {
            int intVal = (int)Convert.ChangeType(m_enumValue, typeof(int));
            UpdateCheckedItems(intVal);

        }

        [DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
        public Enum EnumValue
        {
            get
            {
                object e = Enum.ToObject(m_enumType, GetCurrentValue());
                return (Enum)e;
            }
            set
            {

                Items.Clear();
                m_enumValue = value; // Store the current enum value
                m_enumType = value.GetType(); // Store enum type
                FillEnumMembers(); // Add items for enum members
                ApplyEnumValue(); // Check/uncheck items depending on enum value

            }
        }


    }

    // Represents an item in the checklistbox
    public class FlagCheckedListBoxItem
    {
        public FlagCheckedListBoxItem(int v, string c)
        {
            value = v;
            caption = c;
        }

        public override string ToString()
        {
            return caption;
        }

        // Returns true if the value corresponds to a single bit being set
        public bool IsFlag
        {
            get
            {
                return ((value & (value - 1)) == 0);
            }
        }

        // Returns true if this value is a member of the composite bit value
        public bool IsMemberFlag(FlagCheckedListBoxItem composite)
        {
            return (IsFlag && ((value & composite.value) == value));
        }

        public int value;
        public string caption;
    }


    // UITypeEditor for flag enums
    public class FlagEnumUIEditor : UITypeEditor
    {
        // The checklistbox
        private FlagCheckedListBox flagEnumCB;

        public FlagEnumUIEditor()
        {
            flagEnumCB = new FlagCheckedListBox();
            flagEnumCB.BorderStyle = BorderStyle.None;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (context != null
                && context.Instance != null
                && provider != null)
            {

                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

                if (edSvc != null)
                {

                    Enum e = (Enum)Convert.ChangeType(value, context.PropertyDescriptor.PropertyType);
                    flagEnumCB.EnumValue = e;
                    edSvc.DropDownControl(flagEnumCB);
                    return flagEnumCB.EnumValue;

                }
            }
            return null;
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }

    }

    #endregion



    public class CheckBoxEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override bool GetPaintValueSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override void PaintValue(PaintValueEventArgs e)
        {
            Rectangle newBounds = new Rectangle(e.Bounds.Location, new Size(e.Bounds.Height, e.Bounds.Height));

            ControlPaint.DrawCheckBox(e.Graphics, newBounds, ButtonState.Flat | (Convert.ToBoolean(e.Value) ? ButtonState.Checked : ButtonState.Normal));

            e.Graphics.ExcludeClip(e.Bounds);
        }
    }

}