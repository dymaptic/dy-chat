using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Markdig.Syntax;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;

namespace dymaptic.Chat.ArcGIS.Markdown;

public class CodeBlockRenderer : Markdig.Renderers.Wpf.CodeBlockRenderer
{
    protected override void Write(Markdig.Renderers.WpfRenderer renderer, CodeBlock obj)
    {
        if (renderer == null) throw new ArgumentNullException(nameof(renderer));

        //This is kinda hackish, we should probably have this as a resource and load is that way so we don't have to programatically create it.

        var table = new Table();
        var tableRow = new TableRow();
        var tableRowGroup = new TableRowGroup();
        var tableCell = new TableCell();

        tableRowGroup.Rows.Add(tableRow);

        tableRow.Cells.Add(tableCell);

        table.RowGroups.Add(tableRowGroup);
        table.Columns.Add(new TableColumn());

        renderer.Push(table);
        var codeType = "";

        //we are converting from markdown/markdig code names to avalonedit code names
        switch (((Markdig.Syntax.FencedCodeBlock)obj).Info)
        {
            case "csharp":
                codeType = "C#";
                break;
            case "js":
            default:
                codeType = "JavaScript";
                break;
        }

        var highlighter = HighlightingManager.Instance.GetDefinition(codeType);

        var textEditor = new TextEditor()
        {
            IsManipulationEnabled = false,
            AllowDrop = false,
            IsReadOnly = true,
            SyntaxHighlighting = highlighter,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
        };
        textEditor.SetResourceReference(TextEditor.BackgroundProperty, "TextBox.Static.Background");
        textEditor.SetResourceReference(TextEditor.ForegroundProperty, "MessageTextColor");
        textEditor.Document = new TextDocument(obj.Lines.ToString());

        var border = new Border();
        border.BorderThickness = new Thickness(2);
        border.CornerRadius = new CornerRadius(2);
        border.SetResourceReference(Border.BorderBrushProperty, "TextBox.Static.Background");
        border.Child = textEditor;

        var container = new BlockUIContainer();
        ((IAddChild)container).AddChild(border);
        ((IAddChild)tableCell).AddChild(container);

        var grid = new Grid
        {
            Width = 20D,
            Height = 20D,
            Margin = new Thickness(-5)
        };
        grid.SetResourceReference(Grid.BackgroundProperty, "CopyIconBrush");

        var copyButton = new Button();
        copyButton.SetResourceReference(Control.StyleProperty, "IconButtonStyle");
        copyButton.HorizontalAlignment = HorizontalAlignment.Left;
        copyButton.Margin = new Thickness(2, 0, 0, 2);
        copyButton.Width = 20D;
        copyButton.Height = 20D;
        copyButton.FontWeight = FontWeights.Bold;
        copyButton.ToolTip = "Copy Text";
        var command = new Binding("DataContext.CopyMessageCommand") { ElementName = "DockPane" };
        BindingOperations.SetBinding(copyButton, Button.CommandProperty, command);

        //this sets the codeblock text to be the command parameter
        copyButton.CommandParameter = obj.Lines.ToString();

        copyButton.SetResourceReference(Button.BorderThicknessProperty, "0");
        copyButton.SetResourceReference(Button.VisibilityProperty, "{Binding Content, Converter={StaticResource NullVisibilityConverter}, FallbackValue=Visible}");
        copyButton.Content = grid;

        var row2 = new TableRow();
        var cell2 = new TableCell();
        var container2 = new BlockUIContainer();
        row2.Cells.Add(cell2);
        cell2.Blocks.Add(container2);
        tableRowGroup.Rows.Add(row2);

        ((IAddChild)container2).AddChild(copyButton);
        renderer.Pop();
    }
}
