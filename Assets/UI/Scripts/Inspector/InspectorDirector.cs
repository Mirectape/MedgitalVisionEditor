using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public abstract class InspectorDirector : VisualElement
    {
        protected IInspectorBuilder _builder;

        public InspectorDirector()
        {
            _builder = new InspectorBuilder(this);
        }
    }
}
