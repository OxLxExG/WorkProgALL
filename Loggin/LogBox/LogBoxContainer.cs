using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

namespace Loggin
{

    public static class LogBoxContainer
    {
        private static readonly ConcurrentDictionary<string, RichTextBox> _dic = new();
        public static RichTextBox GetOrCteate(string ID)
        {
            if (_dic.TryGetValue(ID, out var box)) return box;
            Application.Current.Dispatcher.Invoke(() => 
            {
                box = new RichTextBox
                {
                    IsUndoEnabled = false,
                    UndoLimit = 0,
                    IsReadOnly = true,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Document = new FlowDocument(new Paragraph()),
                    Tag = false,
                };
                box.TextChanged += (s, e) =>
                {
                    if (!(bool)box.Tag)
                    {
                        box.Tag = true;
                        Task.Delay(250, CancellationToken.None).ContinueWith(t =>
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                box.ScrollToEnd();
                                box.Tag = false;
                            });
                        });
                    }
                };
                _dic[ID] = box;

            });
            return box!;
        }
        public static void Remove(string ID)
        {
            _dic.Remove(ID, out var _);
        }
    }
}
