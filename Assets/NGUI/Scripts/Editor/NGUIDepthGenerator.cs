using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.TreeViewExamples;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;

namespace nguiextra
{
    public class DepthLayer
    {
        public int Layer = 0;
        public List<UIWidget> uIWidgets = new List<UIWidget>();
        DepthDrawCall emptyDrawCall = DepthDrawCall.CreateNewDC(null,true);

        class DepthDrawCall
        {
            public List<UIWidget> uIWidgets = new List<UIWidget>();
            //public UIDrawCall DrawCall;
            private Material material;
            private Shader shader;
            private Texture mainTexture;
            public bool isEmpty = false;

            public static DepthDrawCall CreateNewDC(UIWidget w, bool Empty = false)
            {
                DepthDrawCall ret = null;
                if (Empty)
                {
                    ret = new DepthDrawCall();
                    ret.isEmpty = true;
                }
                else if (w != null && w.drawCall != null)
                {
                    ret = new DepthDrawCall();
                    //ret.DrawCall = w.drawCall;
                    ret.mainTexture = w.mainTexture;
                    ret.shader = w.shader;
                    ret.material = w.material;
                }
                if(w != null && ret != null)
                {
                    ret.TryAdd(w);
                }
                return ret;
            }
            
            public bool TryAdd(UIWidget w)
            {
                if(isEmpty)
                {
                    if(w != null && !uIWidgets.Contains(w))
                    {
                        uIWidgets.Add(w);
                        return true;
                    }
                    return true;
                }
                if(w!= null)
                {
                    if (w.drawCall == null)
                    {
                        return false;
                    }
                    if (uIWidgets.Contains(w))
                    {
                        return true;
                    }
                    else
                    {
                        if(w.material == material &&
                            w.shader == shader &&
                            w.mainTexture == mainTexture)
                        {
                            uIWidgets.Add(w);
                            return true;
                        }
                        return false;
                    }
                }
                return true;
            }

            public int GenerateDepth(NGUIDepthGenerator.DepthGenerateStrategy depthGenerateStrategy, int depthStart)
            {
                if(depthGenerateStrategy == NGUIDepthGenerator.DepthGenerateStrategy.Combine)
                {
                    depthStart = AutSetDepth_Combine(depthStart);
                }
                else
                {
                    depthStart = AutSetDepth_Increase(depthStart);
                }
                return depthStart;
            }
            private int AutSetDepth_Combine(int currentDepth)
            {
                foreach (var wi in uIWidgets)
                {
                    wi.depth = currentDepth ;
                }
                return currentDepth + 1;
            }

            private int AutSetDepth_Increase(int currentDepth)
            {
                foreach (var wi in uIWidgets)
                {
                    wi.depth = currentDepth++;
                }
                return currentDepth;
            }

        }

        public DepthLayer(int layer)
        {
            Layer = layer;
        }

        public void Add(UIWidget w)
        {
            if (!uIWidgets.Contains(w))
            {
                uIWidgets.Add(w);
            }
        }

        List<DepthDrawCall> GenerateDrawCalls()
        {
            List<DepthDrawCall> depthDrawCalls = new List<DepthDrawCall>();
            foreach(var w in uIWidgets)
            {
                bool hasAdd = false;
                foreach(var dc in depthDrawCalls)
                {
                    if(dc.TryAdd(w))
                    {
                        hasAdd = true;
                        break;
                    }
                }
                if(!hasAdd)
                {
                    var ndc = DepthDrawCall.CreateNewDC(w);
                    if(ndc != null)
                    {
                        depthDrawCalls.Add(ndc);
                    }
                    else
                    {
                        emptyDrawCall.TryAdd(w);
                    }
                }
            }

            return depthDrawCalls;
        }

        public int GenerateDepth(NGUIDepthGenerator.DepthGenerateStrategy depthGenerateStrategy, int depthStart)
        {
            var dcs = GenerateDrawCalls();
            foreach(var dc in dcs)
            {
                ChecksAndWarning(dc);
                depthStart = dc.GenerateDepth(depthGenerateStrategy, depthStart);
            }
            depthStart = emptyDrawCall.GenerateDepth(depthGenerateStrategy, depthStart);
            return depthStart;
        }

        private void ChecksAndWarning(DepthDrawCall depthDrawCall)
        {
            // if all function,warning
            bool allFunc = true;
            foreach(var w in depthDrawCall.uIWidgets)
            {
                if(!w.mIsFunction)
                {
                    allFunc = false;
                    break;
                }
            }
            if(allFunc && depthDrawCall.uIWidgets.Count > 0)
            {
                string widgetsname = "";
                int i = 1;
                foreach (var w in depthDrawCall.uIWidgets)
                {
                    widgetsname += string.Format("\t({0}){1}\n", i, w.name);
                }
                Debug.LogErrorFormat("Layer Check:[层级内全为功能控件！建议优化] Layer = {0}\n" +
                "优化策略：\n\t(1)UIPanel隔离\n\t(2)增加冗余非视窗内的控件以维持DrawCall不rebuild\n"+
                "Widgets:\n{1}"
                , Layer,widgetsname);
            }
        }
    }

    class UIWidgetTreeViewItem: TreeViewItem
    {
        public UIWidget widget;
        public bool visibleCache;
        public UIWidgetTreeViewItem(int id, int depth, string displayName)
            :base(id,depth,displayName)
        {
        }

        public static UIWidgetTreeViewItem UICreateNew(UIWidget w)
        {
            if (w == null) return null;
            UIWidgetTreeViewItem wv = new UIWidgetTreeViewItem(
                w.gameObject.GetInstanceID(),
                -1,
                w.gameObject.name);
            wv.widget = w;
            wv.visibleCache = w.gameObject.activeSelf;
            return wv;
        }
    }

    class DepthGeneratorTreeView : TreeView
    {
        const float kRowHeights = 20f;
        const float kToggleWidth = 18f;
        public bool showControls = true;
        UIPanel[] panelsRoot;

        enum CustomColumns
        {
            Name,
            Value1,
            Value2,
            Value3,
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 150,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("功能", "是否是功能控件."),
                    headerTextAlignment = TextAlignment.Right,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 40,
                    minWidth = 20,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("层级", "视觉曾今[0,..),同一层级会尝试合并"),
                    headerTextAlignment = TextAlignment.Right,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 40,
                    minWidth = 20,
                    autoResize = true,
                    allowToggleVisibility = true
                },
                 new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("当前深度", "当前深度值"),
                    headerTextAlignment = TextAlignment.Right,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 40,
                    minWidth = 20,
                    autoResize = true,
                    allowToggleVisibility = true
                }
            };

            Assert.AreEqual(columns.Length, Enum.GetValues(typeof(CustomColumns)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");

            var state = new MultiColumnHeaderState(columns);
            return state;
        }

        public DepthGeneratorTreeView(TreeViewState state, MultiColumnHeader multicolumnHeader, UIPanel[] panels)
            : base(state, multicolumnHeader)
        {
            panelsRoot = panels;
            rowHeight = kRowHeights;
            columnIndexForTreeFoldouts = 0;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset = (kRowHeights - EditorGUIUtility.singleLineHeight) * 0.5f; // center foldout in the row since we also center content. See RowGUI
            extraSpaceBeforeIconAndLabel = kToggleWidth;
            //multicolumnHeader.sortingChanged += OnSortingChanged;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            return new TreeViewItem { id = 0, depth = -1 };
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = GetRows() ?? new List<TreeViewItem>(200);

            rows.Clear();

            if (panelsRoot != null)
            {
                foreach (var panel in panelsRoot)
                {
                    if (panel == null) continue;
                    var go = panel.gameObject;
                    var item = CreateTreeViewItemForGameObject(go);
                    root.AddChild(item);
                    rows.Add(item);
                    if (go.transform.childCount > 0)
                    {
                        if (IsExpanded(item.id))
                        {
                            AddChildrenRecursive(go, item, rows);
                        }
                        else
                        {
                            item.children = CreateChildListForCollapsedParent();
                        }
                    }
                }
            }


            SetupDepthsFromParentsAndChildren(root);
            return rows;
        }

        void AddChildrenRecursive(GameObject go, TreeViewItem item, IList<TreeViewItem> rows)
        {
            int childCount = go.transform.childCount;

            item.children = new List<TreeViewItem>(childCount);
            for (int i = 0; i < childCount; ++i)
            {
                var childTransform = go.transform.GetChild(i);
                var childItem = CreateTreeViewItemForGameObject(childTransform.gameObject);
                item.AddChild(childItem);
                rows.Add(childItem);

                if (childTransform.childCount > 0)
                {
                    if (IsExpanded(childItem.id))
                    {
                        AddChildrenRecursive(childTransform.gameObject, childItem, rows);
                    }
                    else
                    {
                        childItem.children = CreateChildListForCollapsedParent();
                    }
                }
            }
        }

        static TreeViewItem CreateTreeViewItemForGameObject(GameObject gameObject)
        {
            // We can use the GameObject instanceID for TreeViewItem id, as it ensured to be unique among other items in the tree.
            // To optimize reload time we could delay fetching the transform.name until it used for rendering (prevents allocating strings 
            // for items not rendered in large trees)
            // We just set depth to -1 here and then call SetupDepthsFromParentsAndChildren at the end of BuildRootAndRows to set the depths.
            return new TreeViewItem(gameObject.GetInstanceID(), -1, gameObject.name);
        }

        protected override IList<int> GetAncestors(int id)
        {
            // The backend needs to provide us with this info since the item with id
            // may not be present in the rows
            var transform = GetGameObject(id).transform;

            List<int> ancestors = new List<int>();
            while (transform.parent != null)
            {
                ancestors.Add(transform.parent.gameObject.GetInstanceID());
                transform = transform.parent;
            }

            return ancestors;
        }

        protected override IList<int> GetDescendantsThatHaveChildren(int id)
        {
            Stack<Transform> stack = new Stack<Transform>();

            var start = GetGameObject(id).transform;
            stack.Push(start);

            var parents = new List<int>();
            while (stack.Count > 0)
            {
                Transform current = stack.Pop();
                parents.Add(current.gameObject.GetInstanceID());
                for (int i = 0; i < current.childCount; ++i)
                {
                    if (current.childCount > 0)
                        stack.Push(current.GetChild(i));
                }
            }

            return parents;
        }

        GameObject GetGameObject(int instanceID)
        {
            return (GameObject)EditorUtility.InstanceIDToObject(instanceID);
        }

        // Custom GUI

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item;
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, args.GetColumn(i), ref args);
            }
        }



        void CellGUI(Rect cellRect, TreeViewItem item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);
            Event evt = Event.current;
            var objs = Selection.gameObjects;
            // GameObject isStatic toggle 
            var gameObject = GetGameObject(args.item.id);
            if (gameObject == null)
                return;
            if (column == (int)CustomColumns.Name)
            {
                args.rowRect = cellRect;
                base.RowGUI(args);
            }
            else if (column == (int)CustomColumns.Value1)
            {
                EditorGUI.BeginChangeCheck();
                UIWidget w = gameObject.GetComponent<UIWidget>();
                if (w != null)
                {
                    bool isfunc = EditorGUI.Toggle(cellRect, w.mIsFunction);
                    if (isfunc != w.mIsFunction)
                    {
                        foreach (var obj in objs)
                        {
                            if (obj != null)
                            {
                                UIWidget ww = obj.GetComponent<UIWidget>();
                                if (ww != null)
                                {
                                    ww.mIsFunction = isfunc;
                                }
                            }
                        }
                        w.mIsFunction = isfunc;
                    }
                }
            }
            else if (column == (int)CustomColumns.Value2)
            {
                int v = 0;
                UIWidget w = gameObject.GetComponent<UIWidget>();
                if (w != null)
                {
                    if (int.TryParse(GUI.TextField(cellRect, w.mLayer.ToString()), out v))
                    {
                        if (w.mLayer != v)
                        {
                            foreach (var obj in objs)
                            {
                                if (obj != null)
                                {
                                    UIWidget ww = obj.GetComponent<UIWidget>();
                                    if (ww != null)
                                    {
                                        ww.mLayer = v;
                                    }
                                }
                            }
                        }
                        w.mLayer = v;
                    }
                }
            }
            else if(column == (int)CustomColumns.Value3)
            {
                UIWidget w = gameObject.GetComponent<UIWidget>();
                if (w != null)
                {
                    GUI.Label(cellRect, w.depth.ToString());
                }
                else
                {
                    UIPanel p = gameObject.GetComponent<UIPanel>();
                    if (p != null)
                    {
                        GUI.Label(cellRect, p.depth.ToString());
                    }
                }
            }

        }
        // Selection

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            Selection.instanceIDs = selectedIds.ToArray();
        }

        // Reordering

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return true;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();

            var sortedDraggedIDs = SortItemIDsInRowOrder(args.draggedItemIDs);

            List<UnityObject> objList = new List<UnityObject>(sortedDraggedIDs.Count);
            foreach (var id in sortedDraggedIDs)
            {
                UnityObject obj = EditorUtility.InstanceIDToObject(id);
                if (obj != null)
                    objList.Add(obj);
            }

            DragAndDrop.objectReferences = objList.ToArray();

            string title = objList.Count > 1 ? "<Multiple>" : objList[0].name;
            DragAndDrop.StartDrag(title);
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            // First check if the dragged objects are GameObjects
            var draggedObjects = DragAndDrop.objectReferences;
            var transforms = new List<Transform>(draggedObjects.Length);
            foreach (var obj in draggedObjects)
            {
                var go = obj as GameObject;
                if (go == null)
                {
                    return DragAndDropVisualMode.None;
                }

                transforms.Add(go.transform);
            }

            // Filter out any unnecessary transforms before the reparent operation
            RemoveItemsThatAreDescendantsFromOtherItems(transforms);

            // Reparent
            if (args.performDrop)
            {
                switch (args.dragAndDropPosition)
                {
                    case DragAndDropPosition.UponItem:
                    case DragAndDropPosition.BetweenItems:
                        Transform parent = args.parentItem != null ? GetGameObject(args.parentItem.id).transform : null;

                        if (!IsValidReparenting(parent, transforms))
                            return DragAndDropVisualMode.None;

                        foreach (var trans in transforms)
                            trans.SetParent(parent);

                        if (args.dragAndDropPosition == DragAndDropPosition.BetweenItems)
                        {
                            int insertIndex = args.insertAtIndex;
                            for (int i = transforms.Count - 1; i >= 0; i--)
                            {
                                var transform = transforms[i];
                                insertIndex = GetAdjustedInsertIndex(parent, transform, insertIndex);
                                transform.SetSiblingIndex(insertIndex);
                            }
                        }
                        break;

                    case DragAndDropPosition.OutsideItems:
                        foreach (var trans in transforms)
                        {
                            trans.SetParent(null); // make root when dragged to empty space in treeview
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Reload();
                SetSelection(transforms.Select(t => t.gameObject.GetInstanceID()).ToList(), TreeViewSelectionOptions.RevealAndFrame);
            }

            return DragAndDropVisualMode.Move;
        }

        int GetAdjustedInsertIndex(Transform parent, Transform transformToInsert, int insertIndex)
        {
            if (transformToInsert.parent == parent && transformToInsert.GetSiblingIndex() < insertIndex)
                return --insertIndex;
            return insertIndex;
        }

        bool IsValidReparenting(Transform parent, List<Transform> transformsToMove)
        {
            if (parent == null)
                return true;

            foreach (var transformToMove in transformsToMove)
            {
                if (transformToMove == parent)
                    return false;

                if (IsHoveredAChildOfDragged(parent, transformToMove))
                    return false;
            }

            return true;
        }


        bool IsHoveredAChildOfDragged(Transform hovered, Transform dragged)
        {
            Transform t = hovered.parent;
            while (t)
            {
                if (t == dragged)
                    return true;
                t = t.parent;
            }
            return false;
        }


        // Returns true if there is an ancestor of transform in the transforms list
        static bool IsDescendantOf(Transform transform, List<Transform> transforms)
        {
            while (transform != null)
            {
                transform = transform.parent;
                if (transforms.Contains(transform))
                    return true;
            }
            return false;
        }

        static void RemoveItemsThatAreDescendantsFromOtherItems(List<Transform> transforms)
        {
            transforms.RemoveAll(t => IsDescendantOf(t, transforms));
        }
    }

    public class NGUIDepthGenerator : UnityEditor.EditorWindow
    {
        public enum DepthGenerateStrategy
        {
            Combine,
            Increse,
        }
        private int panelCount = 0;
        private UIPanel[] panels = new UIPanel[0];
        [SerializeField] TreeViewState m_TreeViewState; // Serialized in the window layout file so it survives assembly reloading
        [SerializeField] MultiColumnHeaderState m_MultiColumnHeaderState;
        TreeView m_TreeView;
        SearchField m_SearchField;
        private bool m_Initialized = false;
        private bool m_bRefresh = false;
        private bool m_bShowGenerate = false;
        private int m_nStartDepth = 0;
        private DepthGenerateStrategy depthGenerateStrategy = DepthGenerateStrategy.Combine;
        
        private void OnGUI()
        {
            Event ev = Event.current;
            GUILayout.BeginVertical();
            DrawOptions();
            GUILayout.Space(10);
            GUILayout.BeginVertical();
            //DoToolbar();
            InitIfNeeded();
            DoTreeView();
            GUILayout.EndVertical();
            GUILayout.EndVertical();
        }

        void DrawOptions()
        {
            GUILayout.Label("Options");
            GUILayout.BeginVertical();
            if (GUILayout.Button("刷新"))
            {
                m_bRefresh = true;
                m_Initialized = false;
            }
            m_bShowGenerate = GUILayout.Toggle(m_bShowGenerate, "显示生成项");
            if (m_bShowGenerate)
            {
                if(panels == null || !m_Initialized)
                {
                    EditorUtility.DisplayDialog("警告", "请先设置Panel", "确定");
                    m_bShowGenerate = false;
                }
                else
                {
                    m_nStartDepth = EditorGUILayout.IntField("起始深度", m_nStartDepth);
                    if (m_nStartDepth < 0) m_nStartDepth = 0;
                    depthGenerateStrategy = (DepthGenerateStrategy)EditorGUILayout.EnumPopup("深度分配策略", depthGenerateStrategy);
                    if (GUILayout.Button("生成深度"))
                    {
                        GenerateDepthForPanels();
                    }
                }
            }
            GUILayout.Label("选择UIPanels");
            panelCount = EditorGUILayout.IntField(panelCount);
            if (panelCount < 0) panelCount = 0;
            if (panelCount != panels.Length)
            {
                UIPanel[] panels = new UIPanel[panelCount];
                for (int i = 0; i < this.panels.Length && i < panelCount; ++i)
                {
                    panels[i] = this.panels[i];
                }
                this.panels = panels;
            }
            for (int i = 0; i < panels.Length; ++i)
            {
                panels[i] = EditorGUILayout.ObjectField((UnityEngine.Object)panels[i], typeof(UIPanel), true) as UIPanel;
            }

            GUILayout.EndVertical();
        }

        [MenuItem("NGUI/Open/NGUI Depth Generator")]
        static void ShowWindow()
        {
            var window = GetWindow<NGUIDepthGenerator>();
            window.titleContent = new GUIContent("NGUI Depth Generator");
            window.Show();
        }

        void OnSelectionChange()
        {
            if (m_TreeView != null)
                m_TreeView.SetSelection(Selection.instanceIDs);
            Repaint();
        }

        void OnHierarchyChange()
        {
            if (m_TreeView != null)
                m_TreeView.Reload();
            Repaint();
        }

        void DoTreeView()
        {
            if (m_Initialized)
            {
                Rect rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
                m_TreeView.OnGUI(rect);
            }
        }

        Rect multiColumnTreeViewRect
        {
            get { return new Rect(20, 64, position.width - 40, position.height - 60); }
        }

        void InitIfNeeded()
        {
            if (!m_Initialized && m_bRefresh)
            {
                // Check if it already exists (deserialized from window layout file or scriptable object)
                if (m_TreeViewState == null)
                    m_TreeViewState = new TreeViewState();

                bool firstInit = m_MultiColumnHeaderState == null;
                var headerState = DepthGeneratorTreeView.CreateDefaultMultiColumnHeaderState(multiColumnTreeViewRect.width);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState, headerState);
                m_MultiColumnHeaderState = headerState;

                var multiColumnHeader = new MultiColumnHeader(headerState);
                if (firstInit)
                    multiColumnHeader.ResizeToFit();

                m_TreeView = new DepthGeneratorTreeView(m_TreeViewState, multiColumnHeader, panels);

                m_SearchField = new SearchField();
                m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;

                m_bRefresh = false;
                m_Initialized = true;
            }
        }

        void DoToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.EndHorizontal();
        }

        void GenerateDepthForPanels()
        {
            List<UIWidget> widgets = new List<UIWidget>();
            List<UIPanel> panelsl = new List<UIPanel>();
            if (panels != null)
            {
                foreach (var panel in panels)
                {
                    if (panel != null)
                    {
                        if (!panelsl.Contains(panel))
                        {
                            panelsl.Add(panel);
                        }
                    }
                }
            }
            foreach (var panel in panelsl)
            {
                panel.depth = m_nStartDepth;
                var ws = panel.gameObject.GetComponentsInChildren<UIWidget>(true);
                foreach (var w in ws)
                {
                    if (!widgets.Contains(w))
                    {
                        widgets.Add(w);
                    }
                }
            }
            // generate layers
            SortedDictionary<int, DepthLayer> layers = new SortedDictionary<int, DepthLayer>();
            foreach(var w in widgets)
            {
                if(!layers.ContainsKey(w.mLayer))
                {
                    layers.Add(w.mLayer, new DepthLayer(w.mLayer));
                }
                layers[w.mLayer].Add(w);
            }
            // generate depth
            int depth = m_nStartDepth + 1;
            foreach(var layer in layers)
            {
                depth = layer.Value.GenerateDepth(depthGenerateStrategy, depth);
            }
            EditorUtility.DisplayDialog("OK", "OK", "确定");
        }
    }

}
