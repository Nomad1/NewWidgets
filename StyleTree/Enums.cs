namespace StyleTree
{
    /// <summary>
    /// Operand that shows how two CSS selectors are combined
    /// </summary>
    internal enum StyleSelectorOperand
    {
        None = 0, // comma
        Inherit = 1, // E F an F element descendant of an E element
        Child = 2, // E > F an F element child of an E element
        DirectSibling = 3, // E + F  an F element immediately preceded by an E element
        Sibling = 4, // E ~ F   an F element preceded by an E element
    }
}
