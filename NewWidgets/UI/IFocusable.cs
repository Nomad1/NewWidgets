namespace NewWidgets.UI
{
    /// <summary>
    /// Base interface for focusable controls like buttons and edits
    /// TODO: There is some mystery here because focusing is implmented both in UI and Widgets layer. Not sure why and do we need UI part at all
    /// </summary>
    public interface IFocusable
    {
        bool IsFocusable { get; }

        bool IsFocused { get; }

        void SetFocused(bool focus);
        void Press();
    }
}

