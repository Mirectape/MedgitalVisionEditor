using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Components
{
    public class Hierarchy : VisualElement
    {
        protected interface ITreeHierarchyElement
        {
            string Name { get; }
        }
        
        protected class Volume : ITreeHierarchyElement
        {
            public string Name { get; private set; }

            public readonly IReadOnlyList<ITreeHierarchyElement> Elements;

            public Volume(string name, IReadOnlyList<ITreeHierarchyElement> elements)
            {
                this.Name = name;
                this.Elements = elements;
            }
        }
        
        protected static readonly List<Volume> volumes = new List<Volume>
        {
            /*
            new Volume(
                "Volume 1",
                new List<ITreeHierarchyElement>
                {
                    new Segmentation("Segmentation A"),
                    new Model("Model A"),
                    new Marker("Marker A"),
                    new Transformation("Transformation A")
                }),
            new Volume(
                "Volume 1",
                new List<ITreeHierarchyElement>
                {
                    new Segmentation("Segmentation A"),
                    new Model("Model A"),
                    new Marker("Marker A"),
                    new Transformation("Transformation A")
                })
                */
        };
        
        protected static IList<TreeViewItemData<ITreeHierarchyElement>> GetVolumeTreeRoots()
        {
            int id = 0;
            var roots = new List<TreeViewItemData<ITreeHierarchyElement>>();
            foreach (var volume in volumes)
            {
                var elementsInVolume = new List<TreeViewItemData<ITreeHierarchyElement>>();
                foreach (var element in volume.Elements)
                {
                    elementsInVolume.Add(new TreeViewItemData<ITreeHierarchyElement>(id++, element));
                }

                roots.Add(new TreeViewItemData<ITreeHierarchyElement>(id++, volume, elementsInVolume));
            }
            return roots;
        }
        
        #region Style fields
    
        private static readonly string _logoStyle = "logo";

        #endregion

        #region Private fields

        private TreeView _treeView;
        
        #endregion
        
        public Hierarchy()
        {
            _treeView = new TreeView();
        }
    }
}