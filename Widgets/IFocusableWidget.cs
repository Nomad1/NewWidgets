namespace NewWidgets.Widgets
{
    public interface IFocusableWidget
    {
        bool IsFocusable { get; }
        bool IsFocused { get; }
        void SetFocused(bool focus);
    }
}
