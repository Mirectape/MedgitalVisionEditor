using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class TabBar : VisualElement
    {
        #region Syles

        private static readonly string _tabsContainerStyle = "tabs-container";
        
        #endregion
        
        #region Private fields

        private VisualElement _container;
        private Tab _currentSelectedTab;

        private List<Tab> _topTabs = new List<Tab>
        {
            new Tab("\uf1b3", new Label("Volumes")), // Volumes
            new Tab("\ue4e2", new Label("Calibration")), // Calibration
            new Tab("\uf044", new Label("Segmentation")), // Segmentation
            new Tab("\uf61f", new Label("Models")), // Models
            new Tab("\uf5ae", new Label("Crops and geometry" +
                                        " Crops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometryCrops and geometry"
            )), // Crops and geometry
            new Tab("\uf8dd", new Label("Network"))  // Network
        };
        
        private List<Tab> _bottomTabs = new List<Tab>
        {
            new Tab("\uf0f3", new Label("Notifications")), // Notifications
            new Tab("\uf013", new Label("Settings"))  // Settings
        };

        #endregion
    
        public TabBar(VisualElement container)
        {
            if (container == null) Debug.LogError("TabBar container is null");

            _container = container;

            VisualElement topTabsContainer = new VisualElement();
            VisualElement bottomTabsContainer = new VisualElement();
            
            this.Add(topTabsContainer);
            this.Add(bottomTabsContainer);

            foreach (var tab in _topTabs)
            {
                topTabsContainer.Add(tab);
                _container.Add(tab.ContentParent);
                tab.Value = false;
                tab.RegisterCallback<ClickEvent>(OnTabClick);
            }
            
            foreach (var tab in _bottomTabs)
            {
                bottomTabsContainer.Add(tab);
                _container.Add(tab.ContentParent);
                tab.Value = false;
                tab.RegisterCallback<ClickEvent>(OnTabClick);
            }
        }

        private void OnTabClick(ClickEvent evt)
        {
            if (evt.target is Tab tab)
            {
                if (tab != _currentSelectedTab)
                {
                    foreach (var listTab in _topTabs.Concat(_bottomTabs))
                    {
                        if (listTab != tab)
                        {
                            listTab.Value = false;
                        }
                    }

                    tab.Value = true;
                    _currentSelectedTab = tab;
                }
            }
        }
    }
}

