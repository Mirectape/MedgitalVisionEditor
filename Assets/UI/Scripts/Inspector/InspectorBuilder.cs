using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class InspectorBuilder : IInspectorBuilder
    {
        private VisualElement _container;

        public InspectorBuilder(VisualElement container)
        {
            _container = container;
        }
    }
}

