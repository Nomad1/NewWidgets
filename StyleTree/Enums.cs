namespace StyleTree
{
    /// <summary>
    /// Operand that shows how two CSS selectors are combined
    /// </summary>
    internal enum StyleSelectorOperator
    {
        None = 1, // comma
        Inherit = 2, // E F an F element descendant of an E element
        Child = 3, // E > F an F element child of an E element
        DirectSibling = 4, // E + F  an F element immediately preceded by an E element
        Sibling = 5, // E ~ F   an F element preceded by an E element
    }
}
