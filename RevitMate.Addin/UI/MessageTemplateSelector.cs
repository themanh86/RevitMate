using System.Windows;
using System.Windows.Controls;

namespace RevitMate.Addin.UI
{
    /// <summary>
    /// Chooses the chat item template: a confirmation card for
    /// <see cref="PlanCardViewModel"/>, the standard bubble for everything else.
    /// </summary>
    public sealed class MessageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate MessageTemplate { get; set; }
        public DataTemplate PlanTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            return item is PlanCardViewModel ? PlanTemplate : MessageTemplate;
        }
    }
}
