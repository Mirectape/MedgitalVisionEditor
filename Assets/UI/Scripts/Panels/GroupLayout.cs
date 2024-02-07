using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class GroupLayout : VisualElement, IGroup, IResizible
    {
        #region Properties
    
        public FlexDirection Direction { 
            get => _flexDirection;
            set => SetDirection(value);
        }
        
        #endregion
        
        #region Private fields

        private FlexDirection _flexDirection;
        
        #endregion

        public GroupLayout(FlexDirection flexDirection)
        {
            this.style.flexDirection = _flexDirection = flexDirection;
        }

        public void SetDirection(FlexDirection flexDirection)
        {
            if (flexDirection == _flexDirection) return;
            this.style.flexDirection = _flexDirection = flexDirection;
            RecalculateMinSize();
        }
        
        public void RecalculateMinSize()
        {
            float minWidth = 0f;
            float minHeight = 0f;
            
            foreach (var child in this.Children())
            {
                if (Direction == FlexDirection.Column || Direction == FlexDirection.ColumnReverse)
                {
                    minWidth = Mathf.Max(minWidth, child.resolvedStyle.minWidth.value);
                    minHeight += child.resolvedStyle.minHeight.value;
                }
                else
                {
                    minWidth += child.resolvedStyle.minWidth.value;
                    minHeight = Mathf.Max(minHeight, child.resolvedStyle.minHeight.value);
                }
            }
            
            this.style.minWidth = minWidth;
            this.style.minHeight = minHeight;
        }
    }
}
