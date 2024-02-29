using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Components
{
    public class IconButton : Button
    {
        #region Syles

        private static readonly string _iconRegularStyle = "fa";
        private static readonly string _iconSolidStyle = "fas";
        private static readonly string _iconButtonStyle = "icon-button";
        
        #endregion
        
        public IconButton(Action action, string icon) : base(action)
        {
            this.text = icon;
            this.AddToClassList(_iconRegularStyle);
            this.AddToClassList(_iconButtonStyle);
        }
    }
}

