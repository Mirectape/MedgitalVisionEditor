using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UIElements;

namespace UI.Components
{
    public class Loader : AnimatedSpriteElement
    {
        #region Style fields
    
        private static readonly string _loaderStyle = "loader"; 
        private static readonly string _showStyle = "loader--show"; 

        #endregion
        
        public Loader() : base(SpriteAtlasLoader.LoadSpriteAtlas("Sprites/DotsLoader"), 2.5f)
        {
            this.AddToClassList(_loaderStyle);
        }

        public void Show()
        {
            if (IsAnimationRunning) return;
            
            this.StartAnimation();
            this.AddToClassList(_showStyle);
        }

        public void Hide()
        {
            if (!IsAnimationRunning) return;
            
            this.StopAnimation();
            this.RemoveFromClassList(_showStyle);
        }
    }
}
