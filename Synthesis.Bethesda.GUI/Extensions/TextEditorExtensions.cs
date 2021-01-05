using ICSharpCode.AvalonEdit;
using Noggog;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Synthesis.Bethesda.GUI.Views
{
    public static class TextEditorEx
    {
        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.RegisterAttached("AutoScrollToEnd",
                typeof(bool), typeof(TextEditorEx),
                new PropertyMetadata(false, HookupAutoScrollToEnd));

        public static readonly DependencyProperty AutoScrollHandlerProperty =
            DependencyProperty.RegisterAttached("AutoScrollToEndHandler",
                typeof(TextEditorAutoScrollToEndHandler), typeof(TextEditorEx));

        public static readonly DependencyProperty DoScrollOnChangeProperty =
            DependencyProperty.RegisterAttached("DoScrollOnChange",
                typeof(bool), typeof(TextEditorEx));

        private static void HookupAutoScrollToEnd(DependencyObject d,
                DependencyPropertyChangedEventArgs e)
        {
            var textEditor = d as TextEditor;
            if (textEditor == null) return;

            SetAutoScrollToEnd(textEditor, (bool)e.NewValue);
        }

        public static bool GetAutoScrollToEnd(TextEditor instance)
        {
            return (bool)instance.GetValue(AutoScrollProperty);
        }

        public static void SetAutoScrollToEnd(TextEditor instance, bool value)
        {
            var oldHandler = (TextEditorAutoScrollToEndHandler)instance.GetValue(AutoScrollHandlerProperty);
            if (oldHandler != null)
            {
                oldHandler.Dispose();
                instance.SetValue(AutoScrollHandlerProperty, null);
            }
            instance.SetValue(AutoScrollProperty, value);
            if (value)
                instance.SetValue(AutoScrollHandlerProperty, new TextEditorAutoScrollToEndHandler(instance));
        }

        public static bool GetDoScrollOnChange(TextEditor instance)
        {
            return (bool)instance.GetValue(DoScrollOnChangeProperty);
        }

        public static void SetDoScrollOnChange(TextEditor instance, bool value)
        {
            instance.SetValue(DoScrollOnChangeProperty, value);
        }
    }

    public class TextEditorAutoScrollToEndHandler : DependencyObject, IDisposable
    {
        readonly TextEditor _editor;

        public TextEditorAutoScrollToEndHandler(TextEditor editor)
        {
            if (editor == null) { throw new ArgumentNullException(nameof(editor)); }

            _editor = editor;
            _editor.SetValue(TextEditorEx.DoScrollOnChangeProperty, true);
            _editor.TextChanged += TextChangedEvent;
            _editor.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(ScrollChanged));
        }

        private void ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // User scroll event : set or unset autoscroll mode
            if (e.ExtentHeightChange == 0)
            {
                _editor.SetValue(
                    TextEditorEx.DoScrollOnChangeProperty,
                    e.ViewportHeight + e.VerticalOffset >= e.ExtentHeight);
            }
        }

        private void TextChangedEvent(object? sender, EventArgs args)
        {
            if ((bool)_editor.GetValue(TextEditorEx.DoScrollOnChangeProperty))
            {
                _editor.ScrollToEnd();
            }
        }

        public void Dispose()
        {
            _editor.RemoveHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(ScrollChanged));
        }
    }
}