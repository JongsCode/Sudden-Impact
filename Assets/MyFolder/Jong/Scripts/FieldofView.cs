using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldofView : MonoBehaviour
{
    public struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float distance;
        public float angle;

        public ViewCastInfo(bool _hit, Vector3 _point, float _distance, float _angle)
        {
            hit = _hit;
            point = _point;
            distance = _distance;
            angle = _angle;
        }
    }

    public struct EdgeInfo
    {
        public Vector3 pointA;
        public Vector3 pointB;

        public EdgeInfo(Vector3 _pointA, Vector3 _pointB)
        {
            pointA = _pointA;
            pointB = _pointB;
        }
    }
    public float viewRadius;
    public float roundRadius;
    [Range(0,360)]
    public float viewAngle;
    public float roundAngle;

    public LayerMask targetMask;
    public LayerMask obstacleMask;

    public List<Transform> visibleTargets = new List<Transform>();

    public float meshResolution;
    public int edgeResolveIterations;
    public float edgeDistanceThreshold;
    public float roundEdgeDistanceThreshold;
    public float maskCutawayDistance = 0.15f;

    public MeshFilter viewMeshFilter;
    public MeshFilter roundMeshFilter;
    private Mesh viewMesh;
    private Mesh roundMesh;

    // Ghost Object 구현
    List<GhostItem> ghostItems = new List<GhostItem>();
    
    private void Start()
    {
        viewMesh = new Mesh();
        viewMesh.name = "View Mesh";
        viewMeshFilter.mesh = viewMesh;

        roundMesh = new Mesh();
        roundMesh.name = "Round Mesh";
        roundMeshFilter.mesh = roundMesh;
        //StartCoroutine("FindTargetsWithDelay",0.2f);
    }

    private void LateUpdate()
    {
        DrawFieldOfView();
        DrawFieldOfViewRound();
        FindVisibleTargets();
        FindGhostTargets();
    }
    private IEnumerator FindTargetsWithDelay(float _delay)
    {
        while(true)
        {
            FindVisibleTargets();
            yield return new WaitForSeconds(_delay);
        }
    }

    public bool CheckVisible(Transform _target)
    {
        if (_target == null)
            return false;
        Vector3 dirToTarget = (_target.position - transform.position).normalized;
        float distToTarget = Vector3.Distance(transform.position, _target.position);
        bool checkView = Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2 && (distToTarget <= viewRadius); ;
        bool checkRound = distToTarget <= roundRadius;
        if (checkView || checkRound)
        {
            if (!Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask))
            {
                return true;
            }
        }
        return false;
    }
    private void FindVisibleTargets()
    {
        foreach (Transform visibleTarget in visibleTargets)
        {
            //TODO : 시야 내에 없을 시 처리할 데이터 처리
            //MeshRenderer meshRender = visibleTarget.GetComponent<MeshRenderer>();
            //if (meshRender != null)
            //{
            //    meshRender.enabled = false;
            //}
        }
        visibleTargets.Clear();
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);
        for (int i = 0; i < targetsInViewRadius.Length; ++i)
        {
            Transform target = targetsInViewRadius[i].transform;
            if(CheckVisible(target))
            { 
                    visibleTargets.Add(target);
                    // TODO : 시야내에 들어왔을 처리할 데이터 처리
                    //foreach(Transform visibleTarget in visibleTargets)
                    //{
                    //    MeshRenderer meshRender = visibleTarget.GetComponent<MeshRenderer>();
                    //    if(meshRender != null)
                    //    {
                    //        meshRender.enabled = true;
                    //    }
                    //}
              
            }
        }
    }

    private void FindGhostTargets()
    {
        if (ghostItems.Count == 0) return;
        for(int i = ghostItems.Count -1; i>= 0; --i)
        {
            if (ghostItems[i] == null)
            {
                ghostItems.RemoveAt(i); 
                continue; 
            }
            GhostItem ghostitem = ghostItems[i];
            if (CheckVisible(ghostitem.transform))
            {
                ghostitem.DeleteItem();
                ghostItems.RemoveAt(i);
            }
            
        }
    }

    public void RegisterGhostItems(GhostItem _ghostItem)
    {
        if (!ghostItems.Contains(_ghostItem))
        {
            ghostItems.Add(_ghostItem);
        }
    }
    private void DrawFieldOfView()
    {
        int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);
        float stepAngleSize = viewAngle / stepCount;
        List<Vector3> viewPoints = new List<Vector3>();
        ViewCastInfo oldViewCast = new ViewCastInfo();
        for (int i = 0; i <= stepCount; ++i)
        {
            float angle = transform.eulerAngles.y - (viewAngle / 2) + (stepAngleSize * i);
            //Debug.DrawLine(transform.position, transform.position + DirFromAngle(angle, true) * viewRadius, Color.red);
            ViewCastInfo newViewCast = ViewCast(angle);

            if (i > 0)
            {
                bool edgeDistanceThresholdExceeded = Mathf.Abs(oldViewCast.distance - newViewCast.distance) > edgeDistanceThreshold;

                if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDistanceThresholdExceeded))
                {
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
                    if(edge.pointA != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointA);
                    }
                    if (edge.pointB != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointB);
                    }
                }
            }
            viewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }
        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int triangleCount = (vertexCount - 2) * 3;
        int[] triangles = new int[triangleCount];
        Color[] colors = new Color[vertexCount];
        colors[0] = new Color(1f, 1f, 1f, 1f);
        vertices[0] = Vector3.zero;
        for (int i = 0; i < vertexCount - 1; ++i)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i] + transform.forward * maskCutawayDistance);
            float currentDistance = Vector3.Distance(vertices[0], vertices[i + 1]);
            float alpha = Mathf.Clamp01(1f - currentDistance / viewRadius);
            colors[i + 1] = new Color(1f, 1f, 1f, alpha);
            if (i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.colors = colors;
        viewMesh.RecalculateNormals();
    }

    private void DrawFieldOfViewRound()
    {
        int stepCount = Mathf.RoundToInt(roundAngle * meshResolution);
        float stepAngleSize = roundAngle / stepCount;
        List<Vector3> roundPoints = new List<Vector3>();
        ViewCastInfo oldViewCast = new ViewCastInfo();
        for (int i = 0; i <= stepCount; ++i)
        {
            float angle = transform.eulerAngles.y + 180 - (roundAngle / 2) + (stepAngleSize * i);
            //Debug.DrawLine(transform.position, transform.position + DirFromAngle(angle, true) * viewRadius, Color.red);
            ViewCastInfo newViewCast = RoundCast(angle);

            if (i > 0)
            {
                bool edgeDistanceThresholdExceeded = Mathf.Abs(oldViewCast.distance - newViewCast.distance) > roundEdgeDistanceThreshold;

                if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDistanceThresholdExceeded))
                {
                    EdgeInfo edge = FindRoundEdge(oldViewCast, newViewCast);
                    if (edge.pointA != Vector3.zero)
                    {
                        roundPoints.Add(edge.pointA);
                    }
                    if (edge.pointB != Vector3.zero)
                    {
                        roundPoints.Add(edge.pointB);
                    }
                }
            }

            roundPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }
        int vertexCount = roundPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int triangleCount = (vertexCount - 2) * 3;
        int[] triangles = new int[triangleCount];
        Color[] colors = new Color[vertexCount];
        colors[0] = new Color(1f, 1f, 1f, 1f);
        vertices[0] = Vector3.zero;
        for (int i = 0; i < vertexCount - 1; ++i)
        {
            vertices[i + 1] = transform.InverseTransformPoint(roundPoints[i]);
            float currentDistance = Vector3.Distance(vertices[0], vertices[i + 1]);
            float alpha = Mathf.Clamp01(1f - currentDistance / viewRadius);
            colors[i + 1] = new Color(1f, 1f, 1f, alpha);
            if (i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        roundMesh.Clear();
        roundMesh.vertices = vertices;
        roundMesh.triangles = triangles;
        roundMesh.colors = colors;
        roundMesh.RecalculateNormals();
    }

    public Vector3 DirFromAngle(float _angleDegree, bool _angleIsGlobal)
    {
        if(!_angleIsGlobal)
        {
            _angleDegree += transform.eulerAngles.y;
        }
        float degreeToRadian = _angleDegree * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(degreeToRadian), 0f, Mathf.Cos(degreeToRadian));
    }

    private ViewCastInfo ViewCast(float _globalAngle)
    {
        Vector3 dir = DirFromAngle(_globalAngle, true);
        RaycastHit hit;

        if(Physics.Raycast(transform.position, dir, out hit, viewRadius, obstacleMask))
        {
            return new ViewCastInfo(true, hit.point, hit.distance, _globalAngle);
        }
        else
        {
            return new ViewCastInfo(false, transform.position + dir * viewRadius, viewRadius, _globalAngle);
        }
    }
    private ViewCastInfo RoundCast(float _globalAngle)
    {
        Vector3 dir = DirFromAngle(_globalAngle, true);
        RaycastHit hit;

        if (Physics.Raycast(transform.position, dir, out hit, roundRadius, obstacleMask))
        {
            return new ViewCastInfo(true, hit.point, hit.distance, _globalAngle);
        }
        else
        {
            return new ViewCastInfo(false, transform.position + dir * roundRadius, roundRadius, _globalAngle);
        }
    }

    private EdgeInfo FindEdge(ViewCastInfo _minViewCast, ViewCastInfo _maxViewCast)
    {
        float minAngle = _minViewCast.angle;
        float maxAngle = _maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for(int i = 0; i < edgeResolveIterations; ++i)
        {
            float angle = (minAngle + maxAngle) / 2;
            ViewCastInfo newViewCast = ViewCast(angle);

            bool edgeDistanceThresholdExceeded = Mathf.Abs(_minViewCast.distance - newViewCast.distance) > edgeDistanceThreshold;

            if (newViewCast.hit == _minViewCast.hit && !edgeDistanceThresholdExceeded)
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            }
            else
            {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }
        return new EdgeInfo(minPoint, maxPoint);
    }
    private EdgeInfo FindRoundEdge(ViewCastInfo _minViewCast, ViewCastInfo _maxViewCast)
    {
        float minAngle = _minViewCast.angle;
        float maxAngle = _maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for (int i = 0; i < edgeResolveIterations; ++i)
        {
            float angle = (minAngle + maxAngle) / 2;
            ViewCastInfo newViewCast = RoundCast(angle);

            bool edgeDistanceThresholdExceeded = Mathf.Abs(_minViewCast.distance - newViewCast.distance) > roundEdgeDistanceThreshold;

            if (newViewCast.hit == _minViewCast.hit && !edgeDistanceThresholdExceeded)
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            }
            else
            {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }
        return new EdgeInfo(minPoint, maxPoint);
    }
}
