namespace JsonAssertions;

/// <summary>The category of a <see cref="JsonDifference"/>.</summary>
public enum JsonDifferenceKind
{
    /// <summary>Both sides are the same JSON kind but carry different values (string, number, or
    /// boolean).</summary>
    Value,

    /// <summary>The two sides are different JSON kinds (for example a Number where an Object was
    /// expected).</summary>
    Kind,

    /// <summary>The expected document has a property the actual document does not.</summary>
    MissingProperty,

    /// <summary>The actual document has a property the expected document does not.</summary>
    UnexpectedProperty,

    /// <summary>Both sides are arrays of different lengths.</summary>
    ArrayLength,

    /// <summary>An expected array element has no equivalent element in the actual array (reported for
    /// order-insensitive array comparison).</summary>
    ArrayElementUnmatched,
}
