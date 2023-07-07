using System.Collections;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Serilog.Events;

namespace dymaptic.Chat.Server.Logging;

/// <summary>
///     A formatter for creating structured XML log files.
/// </summary>
internal sealed class XmlTextFormatter : TextFormatter
{
    public XmlTextFormatter()
    {
        _writerSettings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            IndentChars = new string(' ', 3),
            OmitXmlDeclaration = true,
            NewLineOnAttributes = false,
            NewLineChars = Environment.NewLine
        };
    }

    public override void Format(LogEvent logEvent, TextWriter writer)
    {
        //Start with the event
        var element = new XElement("LogEvent");

        //Timestamp is an attribute
        element.Add(new XAttribute("Timestamp",
            logEvent.Timestamp.DateTime.ToString("yyyy/MM/dd HH:mm:ss", FormatProvider)));

        //As is level
        element.Add(new XAttribute("Level", logEvent.Level));

        //Message
        string? message = logEvent.RenderMessage(FormatProvider);
        element.Add(new XElement("Message", message));

        //Exception?
        if (logEvent.Exception != null)
        {
            element.Add(CreateExceptionXElement(logEvent.Exception));
        }

        //If the template is different than the message and there are non-scalar properties, also write the Template
        string? template = logEvent.MessageTemplate.Text;

        if ((template != message) && logEvent.Properties.Any(pair => pair.Value is not ScalarValue))
        {
            element.Add(new XElement("Template", template));
        }

        //Properties?
        if (logEvent.Properties.Count > 0)
        {
            //Create our root
            var propertiesElement = new XElement("Properties");

            //Process properties
            foreach (KeyValuePair<string, LogEventPropertyValue> property in logEvent.Properties)
            {
                string key = property.Key;

              
                LogEventPropertyValue value = property.Value;
                XElement propEle = CreatePropertyXElement(key, value);
                propertiesElement.Add(propEle);
            }

            //Add to main
            element.Add(propertiesElement);
        }

        //Write the xml
        using (var xmlWriter = XmlWriter.Create(writer, _writerSettings))
        {
            element.Save(xmlWriter);
        }

        //Make sure there's a blank line between
        writer.WriteLine();
        writer.WriteLine();
    }

    private static XElement CreateExceptionXElement(Exception exception)
    {
        Type exType = exception.GetType();

        var element = new XElement("Exception");
        element.AddAttribute("Type", exType.Name);

        if (!string.IsNullOrWhiteSpace(exception.Message))
        {
            element.AddElement("Message", exception.Message);
        }

        if (!string.IsNullOrWhiteSpace(exception.Source))
        {
            element.AddElement("Source", exception.Source);
        }

        if (exception.TargetSite != null)
        {
            element.AddElement("TargetSite", RenderMethod(exception.TargetSite));
        }

        if (!string.IsNullOrWhiteSpace(exception.StackTrace))
        {
            element.AddElement("StackTrace", exception.StackTrace);
        }

        if (exception.Data.Count > 0)
        {
            var dataElement = new XElement("Data");

            foreach (DictionaryEntry entry in exception.Data)
            {
                var entryElement = new XElement("Entry");
                entryElement.AddAttribute("Key", entry.Key);
                entryElement.AddAttribute("Value", entry.Value ?? "NULL");
                dataElement.Add(entryElement);
            }
        }

        if (exception.HResult != 0)
        {
            element.AddElement("HResult", $"0x{exception.HResult:X}");

            // We can generate a helplink if one isn't provided
            if (string.IsNullOrWhiteSpace(exception.HelpLink))
            {
                element.AddElement("HelpLink", $"https://errorcodelookup.com/?type=hresult&code={exception.HResult}");
            }
        }

        if (!string.IsNullOrWhiteSpace(exception.HelpLink))
        {
            element.AddElement("HelpLink", exception.HelpLink);
        }

        if (exception.InnerException != null)
        {
            XElement innerElement = CreateExceptionXElement(exception.InnerException);
            innerElement.Name = "InnerException";
            element.Add(innerElement);
        }

        if (exception is AggregateException aggregateException)
        {
            var innerExceptionsElement = new XElement("InnerExceptions");

            foreach (Exception innerException in aggregateException.InnerExceptions)
            {
                XElement innerElement = CreateExceptionXElement(innerException);
                innerElement.Name = "InnerException";
                innerExceptionsElement.Add(innerElement);
            }

            element.Add(innerExceptionsElement);
        }

        PropertyInfo[] properties = exType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo property in properties)
        {
            // Skip all common properties
            if (IgnoredExceptionPropertyNames.Contains(property.Name))
            {
                continue;
            }

            // If it is not common, it is worth mentioning, even if null/empty
            var propertyElement = new XElement(SanitizeName(property.Name),
                property.GetValue(exception));
            element.Add(propertyElement);
        }

        return element;
    }

    private static string SanitizeName(string name)
    {
        var element = new XElement(name);

        return element.Name.LocalName;
    }

    private XElement Render(LogEventProperty property)
    {
        return CreatePropertyXElement(property.Name, property.Value);
    }

    private XElement CreateEventIdElement(LogEventPropertyValue value)
    {
        var eventId = value as StructureValue;
        object? id = (eventId?.Properties[0].Value as ScalarValue)?.Value;
        object? name = (eventId?.Properties[1].Value as ScalarValue)?.Value;

        return new XElement("Event",
            new XAttribute("Id", id!),
            new XAttribute("Name", name!));
    }

    /// <remarks>
    ///     SourceContext is a standard log event property.
    ///     It is the full name (Type.FullName) of the object where the event originated.
    /// </remarks>
    private XElement? CreateSourceContextElement(LogEventPropertyValue value)
    {
        var element = new XElement("SourceContext");

        // SourceContext is always a ScalarValue
        if (value is ScalarValue scalar)
        {
            // Should be a string
            if (scalar.Value is string typeName)
            {
                // We want to clean up Microsoft* and System* namespaces
                // to only show the name of the type and not the full namespace
                if (typeName.StartsWith("Microsoft") ||
                    typeName.StartsWith("System"))
                {
                    // Try to find the last dot, we want everything to the right
                    int idx = typeName.LastIndexOf('.') + 1;

                    if (idx > 0)
                    {
                        element.Add(typeName[idx..]);
                    }
                    else
                    {
                        element.Add(typeName);
                    }
                }
                else
                {
                    element.Add(typeName);
                }
            }

            // If not, just add whatever it is
            else
            {
                element.Add(scalar.Value);
            }

            return element;
        }

        // I'm not sure if we can get here
        return null;
    }

    private XElement CreatePropertyXElement(string name, LogEventPropertyValue value)
    {
        // Special EventId Handling
        if (name == nameof(EventId))
        {
            return CreateEventIdElement(value);
        }

        // Special SourceContext Handling
        if (name == "SourceContext")
        {
            XElement? scElement = CreateSourceContextElement(value);

            if (scElement != null)
            {
                return scElement;
            }

            // Otherwise, we want to fall through to default handling
        }

        string xName = SanitizeName(name);

        if (value is ScalarValue scalar)
        {
            var vElement = new XElement(xName, scalar.Value);
            Type? valueType = scalar.Value?.GetType();

            if ((valueType?.FullName != null) && !valueType.FullName.StartsWith("System"))
            {
                vElement.Add(new XAttribute("Type", valueType.Name));
            }

            return vElement;
        }

        if (value is StructureValue structure)
        {
            var element = new XElement(xName);

            if (!string.IsNullOrWhiteSpace(structure.TypeTag))
            {
                element.Add(new XAttribute("TypeTag", structure.TypeTag));
            }

            foreach (LogEventProperty? p in structure.Properties)
            {
                element.Add(Render(p));
            }

            return element;
        }

        if (value is DictionaryValue dict)
        {
            var element = new XElement(xName);
            element.Add(new XAttribute("Type", "IDictionary"));

            foreach (KeyValuePair<ScalarValue, LogEventPropertyValue> pair in dict.Elements)
            {
                var entryElement = new XElement("Entry");
                XElement keyElement = CreatePropertyXElement("Key", pair.Key);
                XElement valueElement = CreatePropertyXElement("Value", pair.Value);
                entryElement.Add(keyElement, valueElement);
                element.Add(entryElement);
            }

            return element;
        }

        if (value is SequenceValue seq)
        {
            var element = new XElement(xName);
            Type? valueType = null;

            if (seq.Elements.Count > 0)
            {
                // Fast lookup of a common type
                foreach (Type? type in seq.Elements.OfType<ScalarValue>()
                    .Select(sv => sv.Value?.GetType()))
                {
                    if (valueType is null)
                    {
                        valueType = type;
                    }
                    else if (type != valueType)
                    {
                        valueType = typeof(object);

                        break;
                    }
                }
            }

            element.Add(new XAttribute("Type", $"{valueType?.Name}[]"));

            foreach (LogEventPropertyValue? p in seq.Elements)
            {
                XElement pElement = CreatePropertyXElement("Entry", p);
                element.Add(pElement);
            }

            return element;
        }

        var xValue = value.ToString(string.Empty, FormatProvider);

        return new XElement(xName, xValue);
    }

    private readonly XmlWriterSettings _writerSettings;
}