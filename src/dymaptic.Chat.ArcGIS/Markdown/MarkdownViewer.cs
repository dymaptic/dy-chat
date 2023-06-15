using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace dymaptic.Chat.ArcGIS.Markdown;

/// <summary>
/// Usually the image paths in the Markdown must be relative to the application or absolute.
/// This classes make it possible to change the root path.
/// </summary>
public class MarkdownViewer : Markdig.Wpf.MarkdownViewer
{

    protected override void RefreshDocument()
    {
        Document = Markdown != null ? Markdig.Wpf.Markdown.ToFlowDocument(Markdown, Pipeline ?? DefaultPipeline, new WpfRenderer()) : null;
    }
}



