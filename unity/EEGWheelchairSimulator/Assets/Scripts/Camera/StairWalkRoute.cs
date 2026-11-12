using UnityEngine;

namespace EEGWheelchairSimulator
{
    public sealed class StairWalkRoute : MonoBehaviour
    {
        [SerializeField] private Vector3[] floorPoints;
        [SerializeField] private InteractiveDoor upperDoor;
        public const float EyeHeight=1.60f;

        public float Length
        {
            get
            {
                float length=0;
                for(int i=1;i<floorPoints.Length;i++)
                    length+=Vector3.Distance(floorPoints[i-1],floorPoints[i]);
                return length;
            }
        }

        public void Configure(Vector3[] path,InteractiveDoor door)
        {
            floorPoints=path;
            upperDoor=door;
        }

        public Vector3 PositionAt(float distance)
        {
            for(int i=1;i<floorPoints.Length;i++)
            {
                var a=floorPoints[i-1];
                var b=floorPoints[i];
                float length=Vector3.Distance(a,b);
                if(distance<=length)
                    return transform.TransformPoint(Vector3.Lerp(a,b,length>0?distance/length:0));
                distance-=length;
            }
            return transform.TransformPoint(floorPoints[floorPoints.Length-1]);
        }

        public void OpenUpperDoor()
        {
            if(upperDoor)upperDoor.SetOpen(true);
        }
    }
}
