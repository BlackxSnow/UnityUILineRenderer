using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace UILineRenderer
{
    [Serializable]
    public class BezierPoint
    {
        public bool TransformAsTarget;
        public RectTransform Target;
        public Vector2 Position;
        public bool PreControl;
        public Vector2 PreControlOffset;
        public bool PostControl;
        public Vector2 PostControlOffset;
    }

    [CustomPropertyDrawer(typeof(BezierPoint))]
    public class BezierPointDrawer : PropertyDrawer
    {
        static bool init;
        static object lockobj = new object();
        static string folderPath;
        protected static void Initialise()
        {
            lock (lockobj)
            {
                if (init) return;

                string filePath = new System.Diagnostics.StackTrace(true).GetFrame(0).GetFileName();
                filePath = Regex.Replace(filePath, "BezierPoint.cs", "");
                folderPath = Regex.Replace(filePath, @".+?(?=Assets\\)", "");
                init = true;
            }
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (!init) Initialise();

            VisualElement RootElement = new VisualElement();

            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(folderPath + "BezierPointDrawer.uxml");
            RootElement = visualTree.CloneTree(property.propertyPath);

            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(folderPath + "BezierPointDrawer.uss");
            RootElement.styleSheets.Add(styleSheet);

            SerializedProperty lineType = property.serializedObject.FindProperty("lineType");
            SerializedProperty lineEvent = property.serializedObject.FindProperty("OnLineTypeChanged");
            SerializedProperty transformAsTarget = property.FindPropertyRelative("TransformAsTarget");
            SerializedProperty preControl = property.FindPropertyRelative("PreControl");
            SerializedProperty postControl = property.FindPropertyRelative("PostControl");

            VisualElement targetBool = RootElement.Q<VisualElement>("TransformAsTarget");
            VisualElement transform = RootElement.Q<VisualElement>("TransformTarget");
            VisualElement position = RootElement.Q<VisualElement>("Position");
            targetBool.RegisterCallback<ChangeEvent<bool>>(
                e =>
                {
                    transform.style.display = (DisplayStyle)Convert.ToInt32(!e.newValue);
                    position.style.display = (DisplayStyle)Convert.ToInt32(e.newValue);
                });


            VisualElement preBool = RootElement.Q<VisualElement>("PreBool");
            VisualElement preOffset = RootElement.Q<VisualElement>("PreOffset");
            preBool.RegisterCallback<ChangeEvent<bool>>(
                e =>
                {
                    preOffset.style.display = (DisplayStyle)Convert.ToInt32(!e.newValue);
                });

            VisualElement postBool = RootElement.Q<VisualElement>("PostBool");
            VisualElement postOffset = RootElement.Q<VisualElement>("PostOffset");
            postBool.RegisterCallback<ChangeEvent<bool>>(
                e =>
                {
                    postOffset.style.display = (DisplayStyle)Convert.ToInt32(!e.newValue);
                });

            Type t = lineEvent.serializedObject.targetObject.GetType();
            FieldInfo fi = t.GetField("OnLineTypeChanged");
            UnityEvent<UILine.LineTypeEnum> onLineChange = fi.GetValue(lineEvent.serializedObject.targetObject) as UnityEvent<UILine.LineTypeEnum>;
            BezierPoint[] pointArray = property.serializedObject.targetObject.GetType().GetField("BezierControlPoints").GetValue(property.serializedObject.targetObject) as BezierPoint[];
            int pointIndex = Convert.ToInt32(Regex.Match(property.propertyPath, @"\[(\d*)\]").Groups[1].Value);
            int pointLength = pointArray.Length;
            BezierPoint thisPoint = pointArray[pointIndex];
            onLineChange.AddListener(new UnityAction<UILine.LineTypeEnum>(LType =>
            {
                if (LType == UILine.LineTypeEnum.PointToPoint || LType == UILine.LineTypeEnum.PointToPointPolygon)
                {
                    preOffset.style.display = preBool.style.display = postOffset.style.display = postBool.style.display = DisplayStyle.None;
                }
                else
                {
                    if (pointIndex != 0)
                    { preBool.style.display = DisplayStyle.Flex; }
                    else
                    { preBool.style.display = DisplayStyle.None; }

                    if (pointIndex != pointLength - 1)
                    { postBool.style.display = DisplayStyle.Flex; }
                    else
                    { postBool.style.display = DisplayStyle.None; }

                    if (thisPoint.PreControl && pointIndex != 0)
                    { preOffset.style.display = DisplayStyle.Flex; }
                    if (thisPoint.PostControl && pointIndex != pointLength - 1)
                    { postOffset.style.display = DisplayStyle.Flex; }
                }
            }));

            transform.style.display = (DisplayStyle)Convert.ToInt32(!transformAsTarget.boolValue);
            position.style.display = (DisplayStyle)Convert.ToInt32(transformAsTarget.boolValue);
            preOffset.style.display = (DisplayStyle)Convert.ToInt32(!preControl.boolValue);
            postOffset.style.display = (DisplayStyle)Convert.ToInt32(!postControl.boolValue);

            bool isLinePTP = lineType.enumValueIndex == (int)UILine.LineTypeEnum.PointToPoint || lineType.enumValueIndex == (int)UILine.LineTypeEnum.PointToPointPolygon;

            preBool.style.display = postBool.style.display = isLinePTP ? DisplayStyle.None : DisplayStyle.Flex;
            if (isLinePTP)
            {
                preOffset.style.display = postOffset.style.display = DisplayStyle.None;
            }
            else
            {
                if (preControl.boolValue)
                { preOffset.style.display = DisplayStyle.Flex; }
                if (postControl.boolValue)
                { postOffset.style.display = DisplayStyle.Flex; }
            }

            if (pointIndex == 0) preOffset.style.display = preBool.style.display = DisplayStyle.None;
            if (pointIndex == pointLength - 1) postOffset.style.display = postBool.style.display = DisplayStyle.None;

            //preOffset.style.display = postOffset.style.display = transform.style.display = position.style.display = DisplayStyle.None;


            return RootElement;
        }
    }
}
