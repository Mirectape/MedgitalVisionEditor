using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace UI
{
    public class Workspace : VisualElement
    {
        #region Style fields
    
        private static readonly string _maximizedStyle = "maximized-panel";

        #endregion
        
        #region Properties
    
        public static Workspace Instance { get; private set; }
        public Inspector Inspector { get; private set; }
        
        #endregion
        
        #region Private fields
        
        private List<Splitter> _splittersList;
        private List<VisualElement> _resizibleList = new List<VisualElement>();
        private List<VisualElement> _listPre = new List<VisualElement>();
        private List<VisualElement> _listPost = new List<VisualElement>();
        private Splitter _draggingSplitter;
        
        // Maximized Window
        private GroupLayout _expandableScenePanels;
        private VisualElement _maximizedPanel;
        private VisualElement _tempPlugMaximizedPanel;

        #endregion

        public Workspace()
        {
            Instance = this;
            
            // Создаем стандартную структуру

            _expandableScenePanels = new GroupLayout(FlexDirection.Row);
            _expandableScenePanels.style.position = Position.Relative;
            
            GroupLayout vertical1 = new GroupLayout(FlexDirection.Column);
            ViewportPanel axial = SliceProjectionViewManager.CreateView(SliceProjectionAxis.Axial);
            ViewportPanel coronal = SliceProjectionViewManager.CreateView(SliceProjectionAxis.Coronal);
            vertical1.Add(axial);
            vertical1.Add(coronal);
            _expandableScenePanels.Add(vertical1);
            
            GroupLayout vertical2 = new GroupLayout(FlexDirection.Column);
            ViewportPanel perspective = new PerspectiveView();
            ViewportPanel sagittal = SliceProjectionViewManager.CreateView(SliceProjectionAxis.Sagittal);
            vertical2.Add(perspective);
            vertical2.Add(sagittal);
            _expandableScenePanels.Add(vertical2);
            
            Inspector = new Inspector();

            if (axial == null || coronal == null || sagittal == null)
            {
                throw new NullReferenceException("Не удалось создать проеции срезов");
            }
            
            this.Add(_expandableScenePanels);
            this.Add(Inspector);
            
            // Определяем все элементы IResizible

            foreach (var elem in this.Query<VisualElement>().ToList())
            {
                if (elem is IResizible)
                {
                    _resizibleList.Add(elem);
                }
            }

            // Заполняем сплиттерами
            _splittersList = new List<Splitter>();
            BuildSplitters(this);
            
            this.RegisterCallback<GeometryChangedEvent>(UpdateMinSizesOnGeometryChange);
            this.RegisterCallback<GeometryChangedEvent>(WorkspaceChangeSize);
        }
        
        private void UpdateMinSizesOnGeometryChange(GeometryChangedEvent evt)
        {
            this.UnregisterCallback<GeometryChangedEvent>(UpdateMinSizesOnGeometryChange);

            // Обновление минимальных размеров для всех GroupLayout в иерархии
            foreach (var child in this.Children())
            {
                UpdateMinSizeForGroupLayouts(child);
            }
        }

        private void UpdateMinSizeForGroupLayouts(VisualElement currentElement)
        {
            if (currentElement is GroupLayout groupLayout)
            {
                // Рекурсивный обход дочерних элементов
                foreach (var child in currentElement.Children())
                {
                    UpdateMinSizeForGroupLayouts(child);
                }
                
                // Обновление минимального размера, если элемент является GroupLayout
                groupLayout.RecalculateMinSize();
            }
        }
        

        private void WorkspaceChangeSize(GeometryChangedEvent evt)
        {
            foreach (var element in _resizibleList)
            {
                if (element.parent is GroupLayout layout)
                {
                    element.style.flexBasis = 
                        layout.Direction == FlexDirection.Column || layout.Direction == FlexDirection.ColumnReverse 
                        ? element.resolvedStyle.height 
                        : element.resolvedStyle.width;
                }
                else if(element.parent == this)
                {
                    element.style.flexBasis = element.resolvedStyle.width;
                }
            }
        }
        
        private void BuildSplitters(VisualElement parent)
        {
            // Получите общее количество дочерних элементов
            int childCount = parent.childCount;

            bool isVert = parent == this;

            if (parent is GroupLayout parentLayout)
            {
                isVert = parentLayout.Direction is FlexDirection.Row or FlexDirection.RowReverse;
            }
            
            for (int i = childCount - 1; i >= 0; i--)
            {
                var child = parent[i];

                // Рекурсивный проход
                if (child is GroupLayout childGroup)
                    BuildSplitters(child);

                // Если не последний элемент
                if (i != childCount - 1)
                {
                    if (child is IResizible)
                    {
                        // Если группа горизонтальная или сам workspace, то напавление сплитера вертикальное и наоборот
                        SplitterDirection direction = (isVert) 
                            ? SplitterDirection.Vertical 
                            : SplitterDirection.Horizontal;
                        // Создаем разделитель
                        Splitter splitter = new Splitter(parent, direction);
                        splitter.OnDragStartHandler += OnDragStart;
                        // Размещаем сплиттер за элементом
                        splitter.PlaceInFront(child);
                        _splittersList.Add(splitter);
                    }
                }
            }
        }

        private void OnDragStart(Splitter splitter)
        {
            if (_draggingSplitter != null)
            {
                Debug.LogError("Drag splitter already use");
                return;
            }
            
            _draggingSplitter = splitter;
            
            // Получите индекс текущего splitter в списке дочерних элементов
            int splitterIndex = splitter.parent.IndexOf(splitter);

            // Получите левый/верхний элемент (элемент перед splitter'ом)
            VisualElement leftElement = splitter.parent.ElementAt(splitterIndex - 1);

            // Получите правый/нижний элемент (элемент после splitter'а)
            VisualElement rightElement = splitter.parent.ElementAt(splitterIndex + 1);
            
            _listPre.Clear();
            _listPost.Clear();

            _listPre.Add(leftElement);
            _listPost.Add(rightElement);
            
            splitter.OnDragUpdateHandler += OnDragUpdate;
            splitter.OnDragEndHandler += OnDragEnd;
        }
        
        
        private void OnDragUpdate(Vector2 position)
        {
            if (_draggingSplitter.Direction == SplitterDirection.Vertical)
            {
                ApplyDeltaToElements(_listPre, _listPost, position.x, true);
            }
            else
            {
                ApplyDeltaToElements(_listPre, _listPost, position.y, false);
            }
        }

        private void ApplyDeltaToElements(List<VisualElement> preElements, List<VisualElement> postElements, float position, bool isVertical)
        {
            float minDelta = float.MaxValue;
            minDelta = CalculateMinDelta(preElements, position, isVertical, minDelta);
            minDelta = CalculateMinDelta(postElements, position, isVertical, minDelta, true);

            UpdateElementSizes(preElements, minDelta, isVertical);
            UpdateElementSizes(postElements, -minDelta, isVertical);
        }

        private float CalculateMinDelta(List<VisualElement> elements, float position, bool isVertical, float minDelta, bool isPost = false)
        {
            foreach (var element in elements)
            {
                var eRect = element.worldBound;
                float minSize = isVertical ? element.resolvedStyle.minWidth.value : element.resolvedStyle.minHeight.value;
                float newSize = Mathf.Max(minSize, (isVertical ? position - eRect.x : position - eRect.y));
                float deltaSize = newSize - (isVertical ? eRect.width : eRect.height);

                if (!isPost)
                {
                    if (deltaSize < minDelta)
                    {
                        minDelta = deltaSize;
                    }
                }
                else
                {
                    float availableSize = (isVertical ? eRect.width : eRect.height) - minSize;
                    if (availableSize < minDelta)
                    {
                        minDelta = availableSize;
                    }
                }
            }
            return minDelta;
        }

        private void UpdateElementSizes(List<VisualElement> elements, float delta, bool isVertical)
        {
            foreach (var element in elements)
            {
                float minSize = isVertical ? element.resolvedStyle.minWidth.value : element.resolvedStyle.minHeight.value;
                float newSize = element.resolvedStyle.flexBasis.value + delta;

                if (newSize < minSize)
                {
                    element.style.flexShrink = 0;
                    element.style.flexBasis = minSize;
                }
                else
                {
                    if (element.style.flexShrink != 1 && element != Inspector) element.style.flexShrink = 1;
                    element.style.flexBasis = element.resolvedStyle.flexBasis.value + delta;
                }
            }
        }

        private void OnDragEnd(Splitter splitter)
        {
            splitter.OnDragUpdateHandler -= OnDragUpdate;
            splitter.OnDragEndHandler -= OnDragEnd;

            _listPost.Clear();
            _listPre.Clear();
            _draggingSplitter = null;
            
            foreach (var s in _splittersList)
            {
                s.ResetAnchor();
            }
        }

        public  void ResetMaximizePanel()
        {
            if (_maximizedPanel == null || _tempPlugMaximizedPanel == null) return;
            
            _maximizedPanel.RemoveFromClassList(_maximizedStyle);
            
            var parentPanel = _tempPlugMaximizedPanel.parent;
            parentPanel.Insert(parentPanel.IndexOf(_tempPlugMaximizedPanel), _maximizedPanel);
            _tempPlugMaximizedPanel.RemoveFromHierarchy();

            _maximizedPanel = null;
            _tempPlugMaximizedPanel = null;
            
            SetEnabledChild(_expandableScenePanels, true);
        }
        
        public void MaximizePanel(VisualElement maxPanel)
        {
            if (maxPanel == null || maxPanel == _maximizedPanel) return;
            if (_maximizedPanel != null)
            {
                Debug.LogWarning($"Panel {_maximizedPanel.name} currently maximized. Operation cancel.");
                return;
            }
            
            var parentPanel = maxPanel.parent;
            
            if (parentPanel == null)
            {
                Debug.LogWarning($"Parent is null. Operation cancel.");
                return;
            }

            _tempPlugMaximizedPanel = new VisualElement();
            _maximizedPanel = maxPanel;
            
            CopyStyles(_maximizedPanel, _tempPlugMaximizedPanel);
            parentPanel.Insert(parentPanel.IndexOf(_maximizedPanel), _tempPlugMaximizedPanel);
            _expandableScenePanels.Add(_maximizedPanel);
            _maximizedPanel.AddToClassList(_maximizedStyle);

            SetEnabledChild(_expandableScenePanels, false);
        }

        private void SetEnabledChild(VisualElement parentElement, bool value)
        {
            foreach (var element in parentElement.Query<VisualElement>().ToList())
            {
                if (element != parentElement && element != _maximizedPanel)
                {
                    if (element is ViewportPanel vp)
                    {
                        vp.SetPanelEnabled(value);
                    }
                    else if (element is IResizible)
                    {
                        element.SetEnabled(value);
                    }
                    else if (element is Splitter splitter)
                    {
                        splitter.SetEnabled(value);
                    }
                }
            }
        }
        
        private void CopyStyles(VisualElement from, VisualElement to)
        {
            to.style.flexBasis = from.resolvedStyle.flexBasis.value;
            to.style.flexShrink = from.resolvedStyle.flexShrink;
            to.style.flexGrow = from.resolvedStyle.flexGrow;
            to.style.width = from.resolvedStyle.width;
            to.style.height = from.resolvedStyle.height;
            to.style.position = from.resolvedStyle.position;
            to.style.minHeight = from.resolvedStyle.minHeight.value;
            to.style.minWidth = from.resolvedStyle.minWidth.value;
        }
    }
}