using System.Collections;
using System.Windows.Controls;
using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace Auto_Sheets.Behaviors
{
    public class MultiSelectBehavior: Behavior<ListBox>
    {
        public IList SelectedItems
        {
            get { return (IList)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register("SelectedItems", typeof(IList), typeof(MultiSelectBehavior), new PropertyMetadata(null));

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.SelectionChanged += OnSelectionChanged;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
            {
                AssociatedObject.SelectionChanged -= OnSelectionChanged;
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedItems == null) return;

            foreach (var item in e.RemovedItems)
                SelectedItems.Remove(item);

            foreach (var item in e.AddedItems)
                SelectedItems.Add(item);
        }
    }
}

