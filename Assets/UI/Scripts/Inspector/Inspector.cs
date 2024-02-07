using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class Inspector : PanelBase, IResizible
    {
        #region Properties

        public VisualElement ContentContainer
        {
            get => _scrollView.contentContainer;
        }
        
        #endregion
        
        #region Private fields

        private ScrollView _scrollView;
        
        #endregion
        
        public Inspector()
        {
            _scrollView = new ScrollView(ScrollViewMode.Vertical);
            
            this.Add(_scrollView);
        }
    }
}
