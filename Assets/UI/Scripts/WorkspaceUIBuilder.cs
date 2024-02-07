using System;
using UI;
using UnityEngine;
using UnityEngine.UIElements;

public class WorkspaceUIBuilder : MonoBehaviour
{
    #region Styles
    
    private static string bodyStyle = "body";
    private static string flexRowStyle = "flex-row";

    #endregion
    
    #region Properties

    public static VisualElement Body => _body;
    public static VisualElement Workspace => _workspace;
        
    #endregion
    
    #region Private fields

    [SerializeField] private UIDocument _uiDocument;
    private static VisualElement _body;
    private static TopBar _topBar;
    private static TabBar _tabBar;
    private static Workspace _workspace;


    #endregion

    private void Awake()
    {
        if (_uiDocument == null)
        {
            throw new NullReferenceException("UIDocument not assign");
        }

        _body = _uiDocument.rootVisualElement;
        _body.AddToClassList(bodyStyle);


        _topBar = new TopBar();
        _workspace = new Workspace();
        _tabBar = new TabBar(_workspace.Inspector.ContentContainer);
        
        _body.Add(_workspace);
        _body.Add(_tabBar);
        _body.Add(_topBar);
    }
}
