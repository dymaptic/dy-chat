namespace dymaptic.Chat.ArcGIS.Markdown;

public class WpfRenderer : Markdig.Renderers.WpfRenderer
{

    /// <summary>
    /// Load first the custom renderer's
    /// </summary>
    protected override void LoadRenderers()
    {
        ObjectRenderers.Add(new CodeBlockRenderer());
        base.LoadRenderers();
    }
}


