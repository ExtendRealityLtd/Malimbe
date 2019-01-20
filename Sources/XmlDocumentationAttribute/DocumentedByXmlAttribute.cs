namespace Malimbe.XmlDocumentationAttribute
{
    using System;

    /// <summary>
    /// Indicates that the field is documented by XML documentation comments.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DocumentedByXmlAttribute : Attribute
    {
    }
}
