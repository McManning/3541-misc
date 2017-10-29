using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BezierSpline))]
public class BezierSplineInspector : Editor
{
    private BezierSpline spline;
    private Transform handleTransform;
    private Quaternion handleRotation;

    private const float handleSize = 0.04f;
    private const float pickSize = 0.06f;

    private int selectedIndex = -1;

    private void OnSceneGUI()
    {
        spline = target as BezierSpline;

        handleTransform = spline.transform;
        handleRotation = Quaternion.identity;

        // Adjust according to the editor using world or local pivots
        if (Tools.pivotRotation == PivotRotation.Local)
        {
            handleRotation = spline.transform.rotation;
        }
        
        Vector3 p0 = UpdatePoint(0);
        for (int i = 1; i < spline.ControlPointCount; i += 3)
        {
            Vector3 p1 = UpdatePoint(i);
            Vector3 p2 = UpdatePoint(i + 1);
            Vector3 p3 = UpdatePoint(i + 2);

            // Draw handles between control points and their tangents
            Handles.color = Color.gray;
            Handles.DrawLine(p0, p1);
            Handles.DrawLine(p2, p3);
            
            p0 = p3;
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        spline = target as BezierSpline;

        // Draw a custom inspector for the selected point
        if (selectedIndex >= 0 && selectedIndex < spline.ControlPointCount)
        {
            DrawSelectedPointInspector();
        }

        // Add a "Add Curve" button and add a curve on click
        if (GUILayout.Button("Add Curve"))
        {
            Undo.RecordObject(spline, "Add Curve");
            EditorUtility.SetDirty(spline);
            spline.AddCurve();
        }
    }

    /// <summary>
    /// Custom inspector panel for a selected point to allow direct 
    /// editing of the points vector position
    /// </summary>
    private void DrawSelectedPointInspector()
    {
        GUILayout.Label("Selected Point");
        EditorGUI.BeginChangeCheck();
        Vector3 point = EditorGUILayout.Vector3Field("Position", spline.GetControlPoint(selectedIndex));

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(spline, "Move Point");
            EditorUtility.SetDirty(spline);
            spline.SetControlPoint(selectedIndex, point);
        }
    }

    private Vector3 UpdatePoint(int index)
    {
        Handles.color = Color.white;
        if (index % 3 == 0)
        {
            // Color control points something else to differentiate them
            Handles.color = Color.cyan;
        }

        Vector3 point = handleTransform.TransformPoint(spline.GetControlPoint(index));
        float size = HandleUtility.GetHandleSize(point);

        // If we click a control point, make it the active point to be edited
        if (Handles.Button(point, handleRotation, size * handleSize, size * pickSize, Handles.DotHandleCap))
        {
            selectedIndex = index;

            // Force the editor to repaint when a point is selected
            Repaint();
        }
        if (selectedIndex == index)
        {
            EditorGUI.BeginChangeCheck();
            point = Handles.DoPositionHandle(point, handleRotation);

            if (EditorGUI.EndChangeCheck())
            {
                // Add the change to the undo stack and notify Unity
                // that a change has happened
                Undo.RecordObject(spline, "Move Point");
                EditorUtility.SetDirty(spline);

                // Apply changes to the point after transforming
                // from world space to local
                spline.SetControlPoint(index, handleTransform.InverseTransformPoint(point));
            }
        }

        return point;
    }
}
