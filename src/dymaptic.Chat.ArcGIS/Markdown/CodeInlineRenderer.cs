// Copyright (c) Nicolas Musset. All rights reserved.
// This file is licensed under the MIT license. 
// See the LICENSE.md file in the project root for more information.

using Markdig.Syntax.Inlines;
using System;
using System.Windows;
using System.Windows.Documents;

namespace dymaptic.Chat.ArcGIS.Markdown
{
    public class CodeInlineRenderer : Markdig.Renderers.Wpf.Inlines.CodeInlineRenderer
    {
        protected override void Write(Markdig.Renderers.WpfRenderer renderer, CodeInline obj)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            var run = new Run(obj.Content);
            run.SetResourceReference(FrameworkContentElement.StyleProperty, "CodeStyleKey");
            renderer.WriteInline(run);
        }
    }
}
