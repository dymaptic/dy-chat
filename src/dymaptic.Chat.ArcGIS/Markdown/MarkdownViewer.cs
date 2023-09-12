using System;
using Markdig;

namespace dymaptic.Chat.ArcGIS.Markdown;

/// <summary>
/// Usually the image paths in the Markdown must be relative to the application or absolute.
/// This classes make it possible to change the root path.
/// </summary>
public class MarkdownViewer : Markdig.Wpf.MarkdownViewer
{
    protected new static readonly MarkdownPipeline DefaultPipeline = new MarkdownPipelineBuilder().UseSupportedExtensions().Build();

    protected override void RefreshDocument()
    {
        Document = Markdown != null ? Markdig.Wpf.Markdown.ToFlowDocument(Markdown, Pipeline ?? DefaultPipeline, new WpfRenderer()) : null;
    }
}


/// <summary>
/// This overrides the default Markdig renderer to add support for UseSoftlineBreakAsHardlineBreak which is used when setting up the default pipeline
/// </summary>
public static class MarkdownExtensions
{
    /// <summary>
    /// Uses all extensions supported by <c>Markdig.Wpf</c>.
    /// </summary>
    /// <param name="pipeline">The pipeline.</param>
    /// <returns>The modified pipeline</returns>
    public static MarkdownPipelineBuilder UseSupportedExtensions(this MarkdownPipelineBuilder pipeline)
    {
        if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));
        return pipeline
            .UseEmphasisExtras()
            .UseGridTables()
            .UsePipeTables()
            .UseTaskLists()
            .UseAutoLinks()
            .UseSoftlineBreakAsHardlineBreak();
    }
}



