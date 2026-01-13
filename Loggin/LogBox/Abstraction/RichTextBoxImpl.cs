#region Copyright 2021-2023 C. Augusto Proiete & Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using Serilog.Debugging;
using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Threading;

namespace Serilog.Sinks.RichTextBox.Abstraction
{
    internal class RichTextBoxImpl : IRichTextBox
    {
        private readonly System.Windows.Controls.RichTextBox _richTextBox;

        public RichTextBoxImpl(System.Windows.Controls.RichTextBox richTextBox)
        {
            _richTextBox = richTextBox ?? throw new ArgumentNullException(nameof(richTextBox));
        }

        public void Write(string xamlParagraphText)
        {
            Paragraph parsedParagraph;

            try
            {
                parsedParagraph = (Paragraph) XamlReader.Parse(xamlParagraphText);
            }
            catch (XamlParseException ex)
            {
                SelfLog.WriteLine($"Error parsing `{xamlParagraphText}` to XAML: {ex.Message}");
                throw;
            }

            var inlines = parsedParagraph.Inlines.ToList();
            parsedParagraph.Inlines.Clear(); // inlines Parent = null

            var p = (Paragraph) _richTextBox.Document.Blocks.FirstBlock!;
            //if (p.Inlines.Count > 200)
            //{
            //    p.Inlines.Clear();
            //}
            while (p.Inlines.Count > 200) p.Inlines.Remove(p.Inlines.FirstInline);

            p.Inlines.AddRange(inlines);

            //void InsertRange()
            //{
            //    var f = p.Inlines.FirstInline;
            //    foreach (var c in inlines)
            //    {
            //        p.Inlines.InsertBefore(f, c);
            //    }
            //}
            //if (p.Inlines.Count == 0)
            //{
            //    p.Inlines.AddRange(inlines);
            //}
            //else
            //{
            //    if (CheckAccess())
            //        InsertRange();
            //    else
            //        p.Dispatcher.Invoke(DispatcherPriority.Normal, InsertRange);
            //}
        }

        public bool CheckAccess()
        {
            return _richTextBox.CheckAccess();
        }

        public DispatcherOperation BeginInvoke(DispatcherPriority priority, Delegate method, object arg)
        {
            return _richTextBox.Dispatcher.BeginInvoke(priority, method, arg);
        }
    }
}
