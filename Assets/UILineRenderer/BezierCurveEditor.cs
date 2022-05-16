using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace UILineRenderer
{
    [CustomEditor(typeof(UILine))]
    public class BezierCurveEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement container = new VisualElement();

            SerializedProperty material = serializedObject.FindProperty("m_Material");
            SerializedProperty sprite = serializedObject.FindProperty("m_Sprite");
            SerializedProperty color = serializedObject.FindProperty("m_Color");
            SerializedProperty lineType = serializedObject.FindProperty("lineType");
            SerializedProperty resolution = serializedObject.FindProperty("BezierResolution");
            SerializedProperty polyResolution = serializedObject.FindProperty("PolygonResolution");
            SerializedProperty size = serializedObject.FindProperty("Size");
            SerializedProperty polySize = serializedObject.FindProperty("PolygonSize");
            SerializedProperty points = serializedObject.FindProperty("BezierControlPoints");
            SerializedProperty skipPoly = serializedObject.FindProperty("SkipFirstPoly");

            SerializedProperty lineEvent = serializedObject.FindProperty("OnLineTypeChanged");
            Type t = lineEvent.serializedObject.targetObject.GetType();
            FieldInfo fi = t.GetField("OnLineTypeChanged");
            UnityEvent<UILine.LineTypeEnum> onLineChange = fi.GetValue(lineEvent.serializedObject.targetObject) as UnityEvent<UILine.LineTypeEnum>;

            PropertyField line = new PropertyField(lineType);
            PropertyField resolutionField = new PropertyField(resolution, "Curve resolution");
            PropertyField polyResField = new PropertyField(polyResolution, "Polygon resolution");
            PropertyField polySizeField = new PropertyField(polySize, "Polygon size");
            PropertyField skipPolyField = new PropertyField(skipPoly, "Skip first polygon?");

            container.Add(new PropertyField(material));
            container.Add(new PropertyField(sprite));
            container.Add(new PropertyField(color));
            container.Add(line);
            container.Add(resolutionField);
            container.Add(polyResField);
            container.Add(new PropertyField(size, "Line width"));
            container.Add(polySizeField);
            container.Add(skipPolyField);
            container.Add(new PropertyField(points, "Points"));

            resolutionField.style.display = (DisplayStyle)Convert.ToInt32(lineType.enumValueIndex != (int)UILine.LineTypeEnum.Bezier && lineType.enumValueIndex != (int)UILine.LineTypeEnum.BezierPointToPoint);
            polyResField.style.display = (DisplayStyle)Convert.ToInt32(lineType.enumValueIndex != (int)UILine.LineTypeEnum.PointToPointPolygon);
            polySizeField.style.display = (DisplayStyle)Convert.ToInt32(lineType.enumValueIndex != (int)UILine.LineTypeEnum.PointToPointPolygon);
            skipPolyField.style.display = (DisplayStyle)Convert.ToInt32(lineType.enumValueIndex != (int)UILine.LineTypeEnum.PointToPointPolygon);
            

            line.RegisterCallback<ChangeEvent<string>>(
            e => {
                UILine.LineTypeEnum newType = (UILine.LineTypeEnum)Enum.Parse(typeof(UILine.LineTypeEnum), e.newValue.Replace(" ", ""));
                onLineChange?.Invoke(newType);
                if (newType == UILine.LineTypeEnum.PointToPoint)
                {
                    resolutionField.style.display = DisplayStyle.None;
                }
                else
                {
                    resolutionField.style.display = DisplayStyle.Flex;
                }

                if (newType == UILine.LineTypeEnum.PointToPointPolygon)
                {
                    resolutionField.style.display = DisplayStyle.None;
                    polyResField.style.display = DisplayStyle.Flex;
                    polySizeField.style.display = DisplayStyle.Flex;
                    skipPolyField.style.display = DisplayStyle.Flex;
                }
                else
                {
                    polyResField.style.display = DisplayStyle.None;
                    polySizeField.style.display = DisplayStyle.None;
                    skipPolyField.style.display = DisplayStyle.None;
                }
            });

            serializedObject.ApplyModifiedProperties();
            serializedObject.UpdateIfRequiredOrScript();
            return container;
        }
    }
}
