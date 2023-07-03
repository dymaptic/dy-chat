using System.Windows;
using System.Windows.Media;

namespace dymaptic.Chat.ArcGIS.Helpers
{
    internal static class WindowHelpers
    {
        /// <summary>
        /// Recursively finds the specified named parent in a control hierarchy
        /// </summary>
        /// <typeparam name="T">The type of the targeted Find</typeparam>
        /// <param name="child">The child control to start with</param>
        /// <param name="parentName">The name of the parent to find</param>
        /// <returns></returns>
        internal static T? FindParent<T>(DependencyObject? child, string parentName)
            where T : DependencyObject
        {
            if (child == null) return null;

            T? foundParent = null;
            var currentParent = VisualTreeHelper.GetParent(child);

            do
            {
                FrameworkElement? frameworkElement = currentParent as FrameworkElement;
                if (frameworkElement?.Name == parentName && frameworkElement is T)
                {
                    foundParent = (T?)currentParent ?? null;
                    break;
                }
                if (currentParent != null)
                    currentParent = VisualTreeHelper.GetParent(currentParent!);

            } while (currentParent != null);

            return foundParent;
        }
    }
}
