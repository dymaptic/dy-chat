using System.Xml.Linq;

namespace dymaptic.Chat.Server.Logging;

/// <summary>
///     Extensions on <see cref="XElement" />
/// </summary>
public static class XmlExtensions
{
    /// <summary>
    ///     Adds an <see cref="XAttribute" /> to this <see cref="XElement" /> with the given <paramref name="name" /> and
    ///     <paramref name="value" />
    /// </summary>
    public static void AddAttribute(this XElement xElement, XName name, object value)
    {
        var attr = new XAttribute(name, value);
        xElement.Add(attr);
    }

    /// <summary>
    ///     Adds a child <see cref="XElement" /> to this <see cref="XElement" /> with the given <paramref name="name" /> and
    ///     <paramref name="content" />
    /// </summary>
    public static void AddElement(this XElement xElement, XName name, object? content)
    {
        var child = new XElement(name, content);
        xElement.Add(child);
    }
}