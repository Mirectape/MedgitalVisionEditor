using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Components
{
    public class SliceSlider : Slider
    {
        #region Syles

        private static readonly string _sliderStyle = "slice-projection-slider";

        #endregion

        public SliceSlider(float start, float end, SliderDirection dir, float pageSize = 0F) : base(start, end, dir, pageSize)
        {
            this.AddToClassList(_sliderStyle);
        }
    }
}