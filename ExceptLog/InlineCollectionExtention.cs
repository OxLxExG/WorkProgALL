using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;

namespace TextBlockLogging
{
    public static class InlineCollectionExtention
    {
        public static void InsertRange(this InlineCollection Inlines, IEnumerable<Inline> insert)
        {
            Inlines.InsertBefore(Inlines.FirstInline, insert.First());
            var f = Inlines.FirstInline;
            foreach (var c in insert.Skip(1))
            {
                Inlines.InsertAfter(f, c);
                f = c;
            }
        }
    }
}
