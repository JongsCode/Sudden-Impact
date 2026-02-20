using UnityEngine;
using System.Collections;
using UnityEditor;
using TMPro;

[CustomEditor(typeof(FieldofView))]
public class FieldofViewEditor : Editor
{
    private void OnSceneGUI()
    {
        FieldofView fow = (FieldofView)target;
        Handles.color = Color.white;
        Handles.DrawWireArc(fow.transform.position, Vector3.up, Vector3.forward, 360f, fow.viewRadius);
        Handles.DrawWireArc(fow.transform.position, Vector3.up, Vector3.forward, 360f, fow.roundRadius);
        Vector3 viewAngleA = fow.DirFromAngle(-fow.viewAngle / 2, false);
        Vector3 viewAngleB = fow.DirFromAngle(fow.viewAngle / 2, false);
        Vector3 roundAngleA = fow.DirFromAngle(-fow.roundAngle / 2 + 180, false);
        Vector3 roundAngleB = fow.DirFromAngle(fow.roundAngle / 2 + 180, false);

        Handles.DrawLine(fow.transform.position, fow.transform.position + viewAngleA * fow.viewRadius);
        Handles.DrawLine(fow.transform.position, fow.transform.position + viewAngleB * fow.viewRadius);
        Handles.color = Color.blue;
        Handles.DrawLine(fow.transform.position, fow.transform.position + roundAngleA * fow.roundRadius);
        Handles.DrawLine(fow.transform.position, fow.transform.position + roundAngleB * fow.roundRadius);


        Handles.color = Color.red;
        foreach(Transform target in fow.visibleTargets)
        {
            Handles.DrawLine(fow.transform.position, target.position);
        }
    }
}
