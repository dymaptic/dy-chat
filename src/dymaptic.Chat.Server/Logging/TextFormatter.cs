using Serilog.Events;
using Serilog.Formatting;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace dymaptic.Chat.Server.Logging;

/// <summary>
///     A base class for <see cref="HtmlTextFormatter" /> and <see cref="XmlTextFormatter" />
/// </summary>
internal abstract class TextFormatter : ITextFormatter
{
    static TextFormatter()
    {
        IgnoredExceptionPropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            nameof(Exception.Message),
            nameof(Exception.Data),
            nameof(Exception.Source),
            nameof(Exception.TargetSite),
            nameof(Exception.StackTrace),
            nameof(Exception.HResult),
            nameof(Exception.HelpLink),
            nameof(Exception.InnerException),
            nameof(AggregateException.InnerExceptions)
        };
    }

    protected TextFormatter()
    {
        FormatProvider = CultureInfo.CurrentCulture;
    }

    public IFormatProvider FormatProvider { get; init; }

    public abstract void Format(LogEvent logEvent, TextWriter writer);

    protected static string RenderMethod(MethodBase method)
    {
        var text = new StringBuilder();

        if (method is MethodInfo methodInfo)
        {
            text.Append(methodInfo.ReturnType.Name)
                .Append(' ')
                .Append((methodInfo.ReflectedType ?? methodInfo.DeclaringType)?.Name)
                .Append('.')
                .Append(methodInfo.Name)
                .Append('(');
        }
        else
        {
            Debug.Assert(method is ConstructorInfo);

            text.Append(method.DeclaringType?.Name)
                .Append(".ctor(");
        }

        text.AppendJoin(',', method.GetParameters().Select(p => p.ParameterType.Name))
            .Append(')');

        return text.ToString();
    }

    protected static readonly HashSet<string> IgnoredExceptionPropertyNames;
}