using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.AvalonDock.Layout;

namespace Main.Views.Pane
{
    public class LayoutInitializer : ILayoutUpdateStrategy
    {
        private void AssignFloatings(LayoutContent layoutContent, VMBaseForm model) 
        {
            layoutContent.FloatingLeft = model.FloatingLeft;
            layoutContent.FloatingTop = model.FloatingTop;
            layoutContent.FloatingHeight = model.FloatingHeight;
            layoutContent.FloatingWidth = model.FloatingWidth;
        }
        public void AfterInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableShown)
        {
            
        }
        public void AfterInsertDocument(LayoutRoot layout, LayoutDocument documentShown)
        {
            
        }
        public bool BeforeInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableToShow, ILayoutContainer destinationContainer)
        {
            //AD wants to add the anchorable into destinationContainer
            //just for test provide a new anchorablepane 
            //if the pane is floating let the manager go ahead
            //LayoutAnchorablePane destPane = destinationContainer as LayoutAnchorablePane;                

            if (anchorableToShow.IsHidden && !anchorableToShow.IsVisible)
            {
                return false;
            }
            if (anchorableToShow.Content is ToolVM tvm 
                && !anchorableToShow.IsHidden 
                && !anchorableToShow.IsVisible)
            {
                // anchorableToShow.Closed += tvm.Closed;
                // anchorable spec
                anchorableToShow.CanDockAsTabbedDocument = tvm.CanDockAsTabbedDocument;
                anchorableToShow.CanAutoHide = tvm.CanAutoHide;
                anchorableToShow.CanHide = tvm.CanHide;
                anchorableToShow.AutoHideHeight = tvm.AutoHideHeight;
                anchorableToShow.AutoHideMinHeight = tvm.AutoHideMinHeight;
                anchorableToShow.AutoHideWidth = tvm.AutoHideWidth;
                anchorableToShow.AutoHideMinWidth = tvm.AutoHideMinWidth;
                // content anchorable + doc
                AssignFloatings(anchorableToShow, tvm);
                //присоединение вручную
                if (destinationContainer != null &&  destinationContainer.FindParent<LayoutFloatingWindow>() != null)
                {
                    var g = new LayoutAnchorGroup();
                    g.Children.Add(anchorableToShow);
                    if (tvm.ShowStrategy == null || (tvm.ShowStrategy == ShowStrategy.Left)) layout.LeftSide.Children.Add(g);
                    else if (tvm.ShowStrategy == ShowStrategy.Right) layout.RightSide.Children.Add(g);
                    else if (tvm.ShowStrategy == ShowStrategy.Top) layout.TopSide.Children.Add(g);
                    else layout.BottomSide.Children.Add(g); 
                }
                else
                {
                    anchorableToShow.AddToLayout(layout.Manager, (AnchorableShowStrategy?)tvm.ShowStrategy ?? AnchorableShowStrategy.Most);
                }
                return true;
            }
            
            //var toolsPane = layout.Descendents().OfType<LayoutAnchorablePane>().FirstOrDefault(d => d.Name == "ToolsPane");
            //if (toolsPane != null)
            //{
            //    toolsPane.Children.Add(anchorableToShow);
            //    return true;
            //}

            return false;

        }
        public bool BeforeInsertDocument(LayoutRoot layout, LayoutDocument documentToShow, ILayoutContainer destinationContainer)
        {
            if (documentToShow.Content is DocumentVM dvm)
            {
                documentToShow.CanMove = dvm.CanMove;
                AssignFloatings(documentToShow, dvm);
            }
            return false;
        }
    }   
}
