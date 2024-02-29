using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Components
{
    public class PopupMenu
    {
        #region Styles

        private static readonly string _popupStyle = "popup-menu";
        private static readonly string _overlayStyle = "popup-menu__overlay";
        private static readonly string _menuStyle = "popup-menu__menu";
        private static readonly string _menuItemStyle = "popup-menu__item";
        private static readonly string _titleItemStyle = "item__title";
        private static readonly string _iconItemStyle = "item__icon";
        private static readonly string _iconRegularStyle = "fa";  
        
        #endregion
      
        #region Private fields

        private VisualElement m_TargetElement;
        private VisualElement m_PanelRootVisualContainer;
        private VisualElement m_Overlay;
        private VisualElement m_MenuContainer;
        private List<VisualElement> m_MenuItems = new List<VisualElement>(); // Список элементов меню
        
        #endregion
        
        public PopupMenu(VisualElement target, VisualElement panelRootVisualContainer = null)
        {
            if (target == null && panelRootVisualContainer == null)
            {
                Debug.LogError("Target and panelRoot is null");
                return;
            }
            
            m_TargetElement = target;

            if (m_TargetElement != null)
            {
                m_TargetElement.RegisterCallback<AttachToPanelEvent>(TargetAttach);
            }
            
            this.m_PanelRootVisualContainer = panelRootVisualContainer;

            m_Overlay = new VisualElement();
            m_Overlay.AddToClassList(_overlayStyle);
            m_MenuContainer = new VisualElement();
            m_MenuContainer.AddToClassList(_menuStyle);
            m_Overlay.Add(m_MenuContainer);
            
            m_Overlay.RegisterCallback<ClickEvent>(evt => Hide());
        }
        
        private void TargetAttach(AttachToPanelEvent evt)
        {
            if (m_PanelRootVisualContainer == null)
            {
                m_PanelRootVisualContainer = GetRootVisualContainer(m_TargetElement);
            }
            
            m_TargetElement.UnregisterCallback<AttachToPanelEvent>(TargetAttach);
        }

        private void MenuChangeGeometry(GeometryChangedEvent evt)
        {
            if (m_TargetElement == null) return;

            Rect nRect = evt.newRect;
            Rect tRect = m_TargetElement.worldBound;

            float xCoord = (tRect.x + tRect.width) - nRect.width;
            
            m_MenuContainer.style.left = (xCoord < 0) ? tRect.x : xCoord;
            m_MenuContainer.style.top =
                (tRect.y + tRect.height + nRect.height) > m_PanelRootVisualContainer.worldBound.height
                    ? tRect.y - nRect.height : tRect.y + tRect.height;
            
            m_MenuContainer.UnregisterCallback<GeometryChangedEvent>(MenuChangeGeometry);
        }
        
        public void AppendAction(string actionName, Action action, string icon = null)
        {
            VisualElement menuItem = GetItem(action, actionName, icon);
            m_MenuContainer.Add(menuItem);
            m_MenuItems.Add(menuItem);
        }

        public void InsertAction(int atIndex, string actionName, Action action, string icon = null)
        {
            VisualElement menuItem = GetItem(action, actionName, icon);
            if (atIndex < 0 || atIndex > m_MenuItems.Count)
            {
                atIndex = m_MenuItems.Count; // Добавить в конец, если индекс вне диапазона
            }
            m_MenuContainer.Insert(atIndex, menuItem);
            m_MenuItems.Insert(atIndex, menuItem);
        }
        
        public void AppendSeparator(string subMenuPath = null)
        {
            var separator = new VisualElement();
            separator.AddToClassList("menu-separator");
            m_MenuContainer.Add(separator);
            m_MenuItems.Add(separator);
        }
        
        public void RemoveItemAt(int index)
        {
            if(index >= 0 && index < m_MenuItems.Count) {
                var item = m_MenuItems[index];
                m_MenuContainer.Remove(item);
                m_MenuItems.RemoveAt(index);
            }
        }
        
        public void ClearItems()
        {
            foreach(var item in m_MenuItems) {
                m_MenuContainer.Remove(item);
            }
            m_MenuItems.Clear();
        }

        public VisualElement GetItem(Action action, string title, string icon = null)
        {
            VisualElement item = new VisualElement();
            item.AddToClassList(_menuItemStyle);

            Label iconElement = new Label(icon == null ? "" : icon);
            iconElement.pickingMode = PickingMode.Ignore;
            iconElement.focusable = false;
            iconElement.AddToClassList(_iconRegularStyle);
            iconElement.AddToClassList(_iconItemStyle);
            item.Add(iconElement);

            Label titleElement = new Label(title);
            titleElement.pickingMode = PickingMode.Ignore;
            titleElement.focusable = false;
            titleElement.AddToClassList(_titleItemStyle);
            item.Add(titleElement);

            item.RegisterCallback<ClickEvent>(evt =>
            {
                action?.Invoke();
                Hide();
            });
            
            return item;
        } 
        
        public void Show()
        {
            // Добавляем m_Overlay и m_MenuContainer в m_PanelRootVisualContainer только если они еще не добавлены
            if (!m_PanelRootVisualContainer.Contains(m_Overlay))
            {
                m_PanelRootVisualContainer.Add(m_Overlay);
                m_MenuContainer.style.left = 0;
                m_MenuContainer.style.top = 0;
                m_MenuContainer.RegisterCallback<GeometryChangedEvent>(MenuChangeGeometry);
            }
        }

        public void Hide()
        {
            m_Overlay.RemoveFromHierarchy();
        }

        private static VisualElement GetRootVisualContainer(VisualElement element)
        {
            VisualElement rootVisualContainer = null;
            for (VisualElement parentElement = element; parentElement != null; parentElement = parentElement.hierarchy.parent)
            {
                if (parentElement.ClassListContains("unity-ui-document__root") && parentElement.name == "UI-container")
                {
                    rootVisualContainer = parentElement;
                }
            }

            return rootVisualContainer;
        }
    }
}