using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Assets.NGUI.Scripts.Editor
{
    public class NGUIDepthSetterTools
    {
        public static bool CanDoDrawCall(UIWidget w)
        {
            if (w.drawCall != null) return true;
            //if (w.mainTexture == null && w.mainTexture == null) return false;
            return true;
        }
        public static bool IsDrawCallCanCombine(UIDrawCall left, UIDrawCall right)
        {
            if (left != null && right != null)
            {
                if (left.mainTexture == right.mainTexture
                    && left.shader == right.shader
                    && left.baseMaterial == right.baseMaterial)
                    return true;
            }
            return false;
        }
        public static bool CanDrawCallCombine(UIWidget left, UIWidget right)
        {
            if (CanDoDrawCall(left) && CanDoDrawCall(right))
            {
                if (IsDrawCallCanCombine(left.drawCall, right.drawCall)) return true;
            }
            return false;
        }
        public static bool CanDrawCallCombine(Material material,
            Texture texture,
            Shader shader,
            UIWidget right)
        {
            if (CanDoDrawCall(right))
            {
                if (right.material != material || right.shader != shader || right.mainTexture != texture)
                {
                    return false;
                }
                return true;
            }
            return false;
        }
    }
    public class NGUIDepthSetterSubWindow : UnityEditor.EditorWindow
    {
        public class MiniDrawCall
        {
            public enum DepthSortStrategy
            {
                MidSame,
                Increase
            }
            public struct WidgetInformation : IEquatable<WidgetInformation>
            {
                public UIWidget widget;
                public bool isSelect;

                public bool Equals(WidgetInformation other)
                {
                    return widget == other.widget;
                }
            }
            private Material material;
            private Shader shader;
            private Texture mainTexture;
            private bool isEmpty = false;
            public UIWidget beginWidget;
            public UIWidget endWidget;
            public DepthSortStrategy DepthSortStrategyIndex = DepthSortStrategy.MidSame;
            public static MiniDrawCall CreateNewDC(UIWidget w, bool Empty = false)
            {
                if (Empty)
                {
                    var ret = new MiniDrawCall();
                    ret.isEmpty = true;
                    return ret;
                }
                if (NGUIDepthSetterTools.CanDoDrawCall(w))
                {
                    var ret = new MiniDrawCall();
                    ret.widgetInformations.Add(new WidgetInformation() { widget = w });
                    ret.mainTexture = w.mainTexture;
                    ret.shader = w.shader;
                    ret.material = w.material;
                    return ret;
                }
                return null;
            }

            public List<WidgetInformation> widgetInformations = new List<WidgetInformation>();
            public bool CanCombine(UIWidget w)
            {
                if (NGUIDepthSetterTools.CanDoDrawCall(w))
                {
                    if (NGUIDepthSetterTools.CanDrawCallCombine(material, mainTexture, shader, w))
                    {
                        return true;
                    }
                }
                return false;
            }
            public bool Push(UIWidget w)
            {
                if (isEmpty)
                {
                    widgetInformations.Add(new WidgetInformation { widget = w, isSelect = false });
                    return true;
                }
                if (CanCombine(w))
                {
                    widgetInformations.Add(new WidgetInformation { widget = w, isSelect = false });
                    return true;
                }
                return false;
            }
            public bool IsEmpty()
            {
                return isEmpty;
            }
            public void Clear()
            {
                widgetInformations.Clear();
            }
            public bool IsContain(UIWidget w)
            {
                return widgetInformations.Contains(new WidgetInformation() { widget = w });
            }
            public bool IsValid()
            {
                return !isEmpty && widgetInformations.Count > 0;
            }

            public int AutoSetDepth(int currentDepth)
            {
                if (!IsValid()) return currentDepth;
                switch (DepthSortStrategyIndex)
                {
                    case DepthSortStrategy.MidSame:
                        return AutSetDepth_MidSame(currentDepth);
                    case DepthSortStrategy.Increase:
                        return AutSetDepth_Increase(currentDepth);
                    default:
                        break;
                }
                return currentDepth;
            }
            private int AutSetDepth_MidSame(int currentDepth)
            {
                if (beginWidget != null)
                {
                    beginWidget.depth = currentDepth;
                }
                foreach (var wi in widgetInformations)
                {
                    wi.widget.depth = currentDepth + 1;
                }
                if (endWidget != null)
                {
                    endWidget.depth = currentDepth + 2;
                }
                return currentDepth + 3;
            }
            private int AutSetDepth_Increase(int currentDepth)
            {
                foreach (var wi in widgetInformations)
                {
                    wi.widget.depth = currentDepth++;
                }
                return currentDepth;
            }
        }
        public List<MiniDrawCall> DrawCalls = new List<MiniDrawCall>();
        MiniDrawCall EmptyDrawCall = MiniDrawCall.CreateNewDC(null, true);
        Vector2 scrollViewPos = new Vector2(0, 0);
        private static EditorWindow GetWindowWithRectPrivate(Type t, Rect rect, bool utility, string title)
        {
            //UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(t);
            EditorWindow editorWindow = null;/*= (array.Length <= 0) ? null : ((EditorWindow)array[0]);*/
            if (!(bool)editorWindow)
            {
                editorWindow = (ScriptableObject.CreateInstance(t) as EditorWindow);
                editorWindow.minSize = new Vector2(rect.width, rect.height);
                editorWindow.maxSize = new Vector2(rect.width, rect.height);
                editorWindow.position = rect;
                if (title != null)
                {
                    editorWindow.titleContent = new GUIContent(title);
                }
                if (utility)
                {
                    editorWindow.ShowUtility();
                }
                else
                {
                    editorWindow.Show();
                }
            }
            else
            {
                editorWindow.Focus();
            }
            return editorWindow;
        }
        public static NGUIDepthSetterSubWindow CreateInstance(Rect rect, bool utility, string title)
        {
            var window = GetWindowWithRectPrivate(typeof(NGUIDepthSetterSubWindow), rect, utility, title) as NGUIDepthSetterSubWindow;
            window.Show();
            return window;
        }
        private void OnGUI()
        {
            GUILayout.BeginVertical();
            DrawLayerInfo();
            GUILayout.Space(10);
            DrawActions();
            GUILayout.Space(10);
            DrawDrawCalls();
            GUILayout.EndVertical();
        }

        void DrawLayerInfo()
        {
            var mainWindow = NGUIDepthSetterMain.mainWindow;
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Layer Order");
            int layerCount = mainWindow.GetLayersCount();
            int curLayer = mainWindow.GetLayerIndex(this);
            string[] names = new string[layerCount];
            for (int i = 0; i < layerCount; ++i)
            {
                names[i] = (i + 1).ToString();
            }
            int selectLayer = EditorGUILayout.Popup(curLayer, names);
            if (selectLayer != curLayer)
            {
                mainWindow.SwitchLayerIndex(curLayer, selectLayer);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        void DrawActions()
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button("将主面板选中项移入"))
            {
                AddMainPageSelects();
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("移除所有项"))
            {
                ClearAllWidgets(false);
            }
            if (GUILayout.Button("移除选中项"))
            {
                ClearAllWidgets(true);
            }
            if (GUILayout.Button("取消选取"))
            {
                UnSelectAll();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        void DrawDrawCalls()
        {
            GUILayout.BeginVertical();
            scrollViewPos = GUILayout.BeginScrollView(scrollViewPos);
            int count = DrawCalls.Count;
            for (int i = 0; i < count; ++i)
            {
                DrawDrawCall(i, count, DrawCalls[i]);
            }
            if (EmptyDrawCall.widgetInformations.Count > 0)
            {
                DrawDrawCall(-1, count, EmptyDrawCall);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
        string[] DepthSortStrategy = new string[]
        {
            "首尾+-1中间一样",
            "递增"
        };
        void DrawDrawCall(int i, int count, MiniDrawCall dc)
        {
            string key;
            string name;
            bool isEmpty = dc.IsEmpty();
            if (isEmpty)
            {
                key = "Empty DC";
                name = key;
            }
            else
            {
                key = "DC " + (i + 1).ToString();
                name = key + " of " + count.ToString();
            }
            if (NGUIEditorTools.DrawHeader(name, key))
            {
                GUI.color = new Color(0.8f, 0.8f, 0.8f);
                NGUIEditorTools.BeginContents();
                GUILayout.BeginVertical();
                {
                    GUI.color = new Color(0.8f, 0.8f, 0.7f);
                    GUILayout.Label("Options");
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Depth增加规则");
                    dc.DepthSortStrategyIndex = (MiniDrawCall.DepthSortStrategy)EditorGUILayout.EnumPopup(dc.DepthSortStrategyIndex);
                    GUILayout.EndHorizontal();
                    if (dc.DepthSortStrategyIndex == MiniDrawCall.DepthSortStrategy.MidSame)
                    {
                        GUILayout.BeginVertical();
                        var it = EditorGUILayout.ObjectField("Begin", (UnityEngine.Object)dc.beginWidget, typeof(UIWidget), true) as UIWidget;
                        if (dc.IsContain(it))
                        {
                            if (it != dc.endWidget)
                            {
                                dc.beginWidget = it;
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("警告", "不能与End相同", "确定");
                            }
                        }
                        else if (it != null)
                        {
                            EditorUtility.DisplayDialog("警告", "请使用同一个DrawCall里的UIWidget", "确定");
                        }
                        it = EditorGUILayout.ObjectField("End", (UnityEngine.Object)dc.endWidget, typeof(UIWidget), true) as UIWidget;
                        if (dc.IsContain(it))
                        {
                            if (it != dc.beginWidget)
                            {
                                dc.endWidget = it;
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("警告", "不能与Begin相同", "确定");
                            }
                        }
                        else if (it != null)
                        {
                            EditorUtility.DisplayDialog("警告", "请使用同一个DrawCall里的UIWidget", "确定");
                        }
                        GUILayout.EndVertical();
                    }
                }
                GUILayout.EndVertical();
                GUI.color = new Color(0.8f, 0.7f, 0.8f);
                GUILayout.BeginVertical();
                {
                    GUI.color = new Color(0.7f, 0.7f, 0.7f);
                    for (int j = 0; j < dc.widgetInformations.Count; ++j)
                    {
                        GUILayout.BeginHorizontal();
                        var w = dc.widgetInformations[j];
                        w.isSelect = GUILayout.Toggle(w.isSelect, "sel");
                        w.widget = EditorGUILayout.ObjectField(w.widget, typeof(UIWidget), true) as UIWidget;
                        dc.widgetInformations[j] = w;
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndVertical();
                NGUIEditorTools.EndContents();
            }
        }
        private void AddMainPageSelects()
        {
            var mainWindow = NGUIDepthSetterMain.mainWindow;
            var widgets = mainWindow.PopSelectWidgets();
            List<UIWidget> needRemoves = new List<UIWidget>();
            foreach (var v in widgets)
            {
                bool needContinue = false;
                foreach (var l in DrawCalls)
                {
                    if (l.Push(v))
                    {
                        needRemoves.Add(v);
                        needContinue = true;
                        break;
                    }
                }
                if (needContinue) continue;
                // new dc
                var dcn = MiniDrawCall.CreateNewDC(v);
                if (dcn != null)
                {
                    DrawCalls.Add(dcn);
                    needRemoves.Add(v);
                }
            }
            foreach (var v in needRemoves)
            {
                widgets.Remove(v);
            }
            // insert into empty dc
            foreach (var v in widgets)
            {
                EmptyDrawCall.Push(v);
            }
            mainWindow.Focus();
            this.Focus();
        }
        private void ClearAllWidgets(bool bIsSelect)
        {
            List<UIWidget> needRemove = new List<UIWidget>();
            List<UIWidget> l = new List<UIWidget>();
            foreach (var w in DrawCalls)
            {
                foreach (var wi in w.widgetInformations)
                {
                    if (wi.widget)
                    {
                        if (bIsSelect && !wi.isSelect)
                        {
                            continue;
                        }
                        if (!l.Contains(wi.widget))
                        {
                            needRemove.Add(wi.widget);
                            l.Add(wi.widget);
                        }
                    }
                }
            }
            foreach (var wi in EmptyDrawCall.widgetInformations)
            {
                if (wi.widget)
                {
                    if (bIsSelect && !wi.isSelect)
                    {
                        continue;
                    }
                    if (!l.Contains(wi.widget))
                    {
                        needRemove.Add(wi.widget);
                        l.Add(wi.widget);
                    }
                }
            }
            var mainWindow = NGUIDepthSetterMain.mainWindow;
            mainWindow.PushWidgets(l);
            if (!bIsSelect)
            {
                DrawCalls.Clear();
                EmptyDrawCall.Clear();
            }
            else
            {
                foreach (var w1 in needRemove)
                {
                    for (int i = 0; i < DrawCalls.Count; ++i)
                    {
                        var dc = DrawCalls[i];
                        for (int j = dc.widgetInformations.Count - 1; j >= 0; --j)
                        {
                            if (dc.widgetInformations[j].widget == w1)
                            {
                                dc.widgetInformations.RemoveAt(j);
                            }
                        }
                    }
                    for (int i = EmptyDrawCall.widgetInformations.Count - 1; i >= 0; --i)
                    {
                        if (EmptyDrawCall.widgetInformations[i].widget == w1)
                        {
                            EmptyDrawCall.widgetInformations.RemoveAt(i);
                        }
                    }
                }
            }
            RefreshDrawCalls();
            mainWindow.Focus();
            this.Focus();
        }
        private void UnSelectAll()
        {
            for (int i = 0; i < DrawCalls.Count; ++i)
            {
                for (int j = 0; j < DrawCalls[i].widgetInformations.Count; ++j)
                {
                    var w = DrawCalls[i].widgetInformations[j];
                    w.isSelect = false;
                    DrawCalls[i].widgetInformations[j] = w;
                }
            }
            for (int j = 0; j < EmptyDrawCall.widgetInformations.Count; ++j)
            {
                var w = EmptyDrawCall.widgetInformations[j];
                w.isSelect = false;
                EmptyDrawCall.widgetInformations[j] = w;
            }
        }

        private void RefreshDrawCalls()
        {
            foreach (var w in DrawCalls)
            {
                if (!w.IsContain(w.beginWidget))
                {
                    w.beginWidget = null;
                }
                if (!w.IsContain(w.endWidget))
                {
                    w.endWidget = null;
                }
            }
        }
        private void OnDestroy()
        {
            var mainWindow = NGUIDepthSetterMain.mainWindow;
            ClearAllWidgets(false);
            if (mainWindow)
            {
                mainWindow.RemoveSubWindow(this);
            }
        }

        private Vector2 minimalSize = new Vector2(160, 20);
        public Vector2 CacheSize = new Vector2(160, 20);
        private void OnLostFocus()
        {
            var size = this.maxSize;
            if (size != minimalSize)
            {
                CacheSize = size;
            }
            this.maxSize = minimalSize;
            this.minSize = minimalSize;
        }

        private void OnFocus()
        {
            if (CacheSize != minimalSize)
            {
                this.maxSize = CacheSize;
                this.minSize = CacheSize;
            }
        }

    }

    public class NGUIDepthSetterMain : UnityEditor.EditorWindow
    {
        public static NGUIDepthSetterMain mainWindow;
        public List<NGUIDepthSetterSubWindow> subWindows = new List<NGUIDepthSetterSubWindow>();
        public struct WidgetInformation : IEquatable<WidgetInformation>
        {
            public UIWidget widget;
            public bool isSelect;
            public WidgetInformation(UIWidget w)
            {
                widget = w;
                isSelect = false;
            }

            public bool Equals(WidgetInformation other)
            {
                return widget == other.widget;
            }

            public void SetSelect(bool s)
            {
                isSelect = s;
            }
        }
        private List<WidgetInformation> widgets = new List<WidgetInformation>();
        private int panelCount = 0;
        private UIPanel[] panel = new UIPanel[0];
        [MenuItem("NGUI/Open/NGUI Depth Setter")]
        static void Init()
        {
            NGUIDepthSetterMain window = (NGUIDepthSetterMain)EditorWindow.GetWindow(typeof(NGUIDepthSetterMain), false, "NGUI Depth Setter");
            window.Clear();
            window.ClearPanels();
            window.Show();
        }
        void CloseLayers()
        {
            for (int i = subWindows.Count - 1; i >= 0; --i)
            {
                subWindows[i].Close();
            }
        }
        void ClearPanels()
        {
            panelCount = 0;
            panel = new UIPanel[0];
        }
        void Clear()
        {
            CloseLayers();
            widgets.Clear();
        }
        private void OnGUI()
        {
            Event ev = Event.current;
            GUILayout.BeginVertical();
            DrawOptions();
            GUILayout.Space(10);
            DrawActions();
            GUILayout.Space(10);
            DrawWidgets();
            GUILayout.Space(10);
            GUILayout.EndVertical();
        }

        void DrawOptions()
        {
            GUILayout.Label("Options");
            GUILayout.BeginVertical();
            GUILayout.Label("选择UIPanels");
            panelCount = EditorGUILayout.IntField(panelCount);
            if (panelCount < 0) panelCount = 0;
            if (panelCount != panel.Length)
            {
                UIPanel[] panels = new UIPanel[panelCount];
                for (int i = 0; i < panel.Length && i < panelCount; ++i)
                {
                    panels[i] = panel[i];
                }
                panel = panels;
            }
            for (int i = 0; i < panel.Length; ++i)
            {
                panel[i] = EditorGUILayout.ObjectField((UnityEngine.Object)panel[i], typeof(UIPanel), true) as UIPanel;
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("获取UIWidgets"))
            {
                Clear();
                for (int i = 0; i < panel.Length; ++i)
                {
                    var p = panel[i];
                    if (p)
                    {
                        var ws = p.GetComponentsInChildren<UIWidget>(true);
                        foreach (var w in ws)
                        {
                            widgets.Add(new WidgetInformation(w));
                        }
                    }
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginVertical();
            if (GUILayout.Button("自动生成depth"))
            {
                AutoGenerateDepth();
            }
            GUILayout.EndVertical();
            GUILayout.EndVertical();
            //GUILayout.FlexibleSpace();
        }

        void DrawActions()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+Layer"))
            {
                Rect rect = new Rect(10, 0, 300, 500);
                rect.x = subWindows.Count * 10 + 10;
                rect.y = subWindows.Count * 10 + 10;
                var sw = NGUIDepthSetterSubWindow.CreateInstance(rect, false, "Layer");
                subWindows.Add(sw);
            }
            if (GUILayout.Button("清空Layers"))
            {
                CloseLayers();
            }
            if (GUILayout.Button("取消选取"))
            {
                for (int i = 0; i < widgets.Count; ++i)
                {
                    var v = widgets[i];
                    v.SetSelect(false);
                    widgets[i] = v;
                }
            }
            if (GUILayout.Button("勾选场景里选中项"))
            {
                SelectTheSceneSelects();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        Vector2 scrollViewPos = new Vector2(0, 0);
        void DrawWidgets()
        {
            GUILayout.BeginVertical();
            scrollViewPos = GUILayout.BeginScrollView(scrollViewPos);
            int count = widgets.Count;
            for (int i = 0; i < count; ++i)
            {
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                var w = widgets[i];
                w.isSelect = GUILayout.Toggle(w.isSelect, "sel");
                GUILayout.Label(w.widget.GetType().Name);
                w.widget = EditorGUILayout.ObjectField(w.widget, typeof(UIWidget), true) as UIWidget;
                widgets[i] = w;
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
        public void OnDestroy()
        {
            Clear();
        }
        void OnEnable() { mainWindow = this; }
        void OnDisable() { Clear(); mainWindow = null; }
        void OnSelectionChange() { Repaint(); }
        public List<UIWidget> PopSelectWidgets()
        {
            List<UIWidget> ret = new List<UIWidget>();
            foreach (var w in widgets)
            {
                if (w.isSelect)
                {
                    ret.Add(w.widget);
                }
            }
            for (int i = widgets.Count - 1; i >= 0; --i)
            {
                if (widgets[i].isSelect)
                {
                    widgets.RemoveAt(i);
                }
            }
            return ret;
        }
        public void PushWidgets(List<UIWidget> l)
        {
            foreach (var w in l)
            {
                if (!widgets.Contains(new WidgetInformation(w)))
                {
                    widgets.Add(new WidgetInformation(w));
                }
            }
        }
        public void RemoveSubWindow(NGUIDepthSetterSubWindow subWin)
        {
            if (subWin)
            {
                //TODO
                //refresh all
                for (int i = 0; i < mainWindow.subWindows.Count; ++i)
                {
                    mainWindow.subWindows[i].Focus();
                }
                mainWindow.subWindows.Remove(subWin);
            }
        }

        void SelectTheSceneSelects()
        {
            var sels = Selection.transforms;
            foreach (var st in sels)
            {
                var uw = st.GetComponent<UIWidget>();
                if (uw)
                {
                    var twi = new WidgetInformation() { widget = uw };
                    int index = widgets.FindIndex(s => s.Equals(twi));
                    if (index >= 0)
                    {
                        var wi = widgets[index];
                        wi.isSelect = true;
                        widgets[index] = wi;
                    }
                }
            }
        }

        void AutoGenerateDepth()
        {
            int currentDepth = 0;
            foreach (var sw in subWindows)
            {
                foreach (var dc in sw.DrawCalls)
                {
                    currentDepth = dc.AutoSetDepth(currentDepth);
                }
            }
        }

        public int GetLayersCount()
        {
            return subWindows.Count;
        }
        public int GetLayerIndex(NGUIDepthSetterSubWindow subWin)
        {
            return subWindows.IndexOf(subWin);
        }

        public void SwitchLayerIndex(int curIndex, int tarIndex)
        {
            var cur = subWindows[curIndex];
            var tar = subWindows[tarIndex];
            subWindows[curIndex] = tar;
            subWindows[tarIndex] = cur;
            subWindows[curIndex].Focus();
            subWindows[tarIndex].Focus();
        }
    }
}
