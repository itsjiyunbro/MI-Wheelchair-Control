using UnityEditor;
using UnityEngine;
using System.Reflection;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static partial class ConvergenceHallFirstFloor
    {
        const float StoreyRise=4.50f;

        static void ConnectCoreStairs(GameObject basement,Vector3 firstOrigin)
        {
            ConnectStair(basement,firstOrigin,true);
            ConnectStair(basement,firstOrigin,false);
        }

        static void ConnectStair(GameObject basement,Vector3 firstOrigin,bool east)
        {
            var lower=basement.transform.Find(east?"CoreHalls/B112Stairwell":"CoreHalls/B109Stairwell");
            var upper=details.Find(east?"NorthStairwell":"SouthStairwell");
            var originalLanding=lower.Find("Landing");
            var connected=P.Group(details,east?"EastConnectedStair":"WestConnectedStair");
            connected.localPosition=lower.position-firstOrigin;
            connected.localRotation=lower.rotation;

            var oldFlight=lower.Find("SwitchbackStairs").gameObject;
            oldFlight.SetActive(false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(oldFlight);
            var storage=lower.Find("UnderStairStorage");
            if(!east&&storage)
            {
                var firstStorage=storage.localPosition;
                firstStorage.x=(originalLanding.localScale.x-.10f)/2;
                storage.localPosition=firstStorage;
                PrefabUtility.RecordPrefabInstancePropertyModifications(storage);
            }

            const int count=13;
            const float start=.98f,landingDepth=.94f,mid=StoreyRise/2;
            float width=originalLanding.localScale.x,depth=originalLanding.localScale.z;
            float turn=depth-landingDepth;
            float tread=(turn-start)/count,rise=mid/count;
            float laneWidth=(width-.10f)/2;
            float lowerX=(east?1:-1)*(laneWidth+.10f)/2;
            float upperX=-lowerX;
            var stairTile=AssetDatabase.LoadAssetAtPath<Material>(Mats+"StairTile.mat");

            void Surface(string name,Vector3 position,Vector3 size)
            {
                var surface=Box(connected,name,position,size,stairTile);
                surface.gameObject.AddComponent<BoxCollider>();
            }

            for(int i=0;i<count;i++)
            {
                float lowerHeight=(i+1)*rise;
                float lowerZ=start+(i+.5f)*tread;
                Surface("LowerTread"+i,new Vector3(lowerX,lowerHeight/2,lowerZ),new Vector3(laneWidth,lowerHeight,tread));
                float upperHeight=mid+(i+1)*rise;
                float upperZ=turn-(i+.5f)*tread;
                Surface("ReturnTread"+i,new Vector3(upperX,upperHeight-rise/2,upperZ),new Vector3(laneWidth,rise,tread));
                Box(connected,"LowerNosing",new Vector3(lowerX,lowerHeight+.003f,lowerZ-tread/2+.012f),new Vector3(laneWidth,.006f,.023f),silver);
                Box(connected,"ReturnNosing",new Vector3(upperX,upperHeight+.003f,upperZ+tread/2-.012f),new Vector3(laneWidth,.006f,.023f),silver);
            }
            Surface("MidLanding",new Vector3(0,mid-.075f,turn+landingDepth/2),new Vector3(width,.15f,landingDepth));
            var slabBottom=new Vector3(upperX,mid-.16f,turn);
            var slabTop=new Vector3(upperX,StoreyRise-.16f,start);
            var slab=Box(connected,"ReturnSupport",(slabBottom+slabTop)/2,new Vector3(laneWidth,.15f,Vector3.Distance(slabBottom,slabTop)),stairTile);
            slab.localRotation=Quaternion.LookRotation(slabTop-slabBottom);

            var entrance=east?upper.Find("StairDoor"):upper;
            var topEntry=connected.InverseTransformPoint(entrance.position);
            const float deckEdge=.46f;
            float entryWidth=east?4f:3.528f;
            float deckFront=topEntry.z-.018f;
            Require(deckFront<deckEdge,"Upper stair approach lands beyond the stair");
            Surface("UpperApproach",new Vector3(topEntry.x,StoreyRise-.09f,(deckFront+deckEdge)/2),
                new Vector3(entryWidth,.18f,deckEdge-deckFront));
            Surface("UpperLanding",new Vector3(upperX,StoreyRise-.09f,(deckEdge+start)/2),
                new Vector3(laneWidth,.18f,start-deckEdge));
            // The stair remains a walking route. Keep the wheelchair clear of the open flight.
            var guard=Box(connected,"WheelchairLandingGuard",new Vector3(topEntry.x,StoreyRise+.55f,deckEdge-.035f),
                new Vector3(entryWidth,1.10f,.07f),white,true);
            guard.GetComponent<Renderer>().enabled=false;

            foreach(int side in new[]{-1,1})
            {
                float x=lowerX+side*(laneWidth/2-.055f);
                AddStairRail(connected,new Vector3(x,.03f,start),new Vector3(x,mid,turn));
                x=upperX+side*(laneWidth/2-.055f);
                AddStairRail(connected,new Vector3(x,mid,turn),new Vector3(x,StoreyRise,start));
                AddStairRail(connected,new Vector3(x,StoreyRise,start),new Vector3(x,StoreyRise,deckEdge));
            }
            AddStairRail(connected,new Vector3(-width/2+.05f,mid,depth-.065f),new Vector3(width/2-.05f,mid,depth-.065f));
            float laneLeft=upperX-laneWidth/2,laneRight=upperX+laneWidth/2;
            float deckLeft=topEntry.x-entryWidth/2,deckRight=topEntry.x+entryWidth/2;
            if(laneLeft-deckLeft>.08f)
                AddStairRail(connected,new Vector3(deckLeft+.025f,StoreyRise,deckEdge),new Vector3(laneLeft,StoreyRise,deckEdge));
            if(deckRight-laneRight>.08f)
                AddStairRail(connected,new Vector3(laneRight,StoreyRise,deckEdge),new Vector3(deckRight-.025f,StoreyRise,deckEdge));

            // Close the shaft perimeter between the basement walls and the upper hall walls.
            float seamHeight=StoreyRise-H;
            foreach(float x in new[]{-width/2-.058f,width/2+.058f})
                Box(connected,"ShaftSideWall",new Vector3(x,H+seamHeight/2,depth/2),new Vector3(.116f,seamHeight,depth),white,true);
            Box(connected,"ShaftRearWall",new Vector3(0,H+seamHeight/2,depth+.058f),new Vector3(width+.232f,seamHeight,.116f),white,true);
            if(!east)
                Box(connected,"LowerFlightCeiling",new Vector3(lowerX,StoreyRise-.10f,depth/2),
                    new Vector3(laneWidth+.10f,.15f,depth+.20f),white);

            float lowerEntryX=east?-.43f:0;
            var path=new[]
            {
                new Vector3(lowerEntryX,0,-.72f),new Vector3(lowerEntryX,0,.43f),
                new Vector3(lowerX,0,.63f),new Vector3(lowerX,0,start),
                new Vector3(lowerX,mid,turn),new Vector3(lowerX,mid,turn+.46f),
                new Vector3(upperX,mid,turn+.46f),new Vector3(upperX,mid,turn),
                new Vector3(upperX,StoreyRise,start),new Vector3(upperX,StoreyRise,.15f),
                new Vector3(topEntry.x,StoreyRise,Mathf.Lerp(.15f,topEntry.z,.64f)),
                new Vector3(topEntry.x,StoreyRise,topEntry.z-.72f)
            };
            connected.gameObject.AddComponent<StairWalkRoute>().Configure(path,entrance.GetComponent<InteractiveDoor>());
            Debug.Log($"CONNECTED_STAIR: {connected.name}, doorway offset {topEntry.z:F2}m, 26 treads, rise {StoreyRise:F2}m.");
        }

        static void AddStairRail(Transform parent,Vector3 start,Vector3 end)
        {
            var height=Vector3.up*.92f;
            P.Tube(parent,"Handrail",start+height,end+height,.037f,black);
            int posts=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(start,end)/1.40f));
            for(int i=0;i<=posts;i++)
            {
                var foot=Vector3.Lerp(start,end,i/(float)posts);
                P.Tube(parent,"RailPost",foot,foot+height,.029f,black);
            }
        }

        static void ValidateConnectedStairs(GameObject floor)
        {
            foreach(string name in new[]{"EastConnectedStair","WestConnectedStair"})
            {
                var stair=floor.transform.Find("Details/"+name);
                var route=stair.GetComponent<StairWalkRoute>();
                Require(Mathf.Abs(route.PositionAt(route.Length).y-route.PositionAt(0).y-StoreyRise)<.004f,name+" storey mismatch");
                for(int i=0;i<26;i++)
                {
                    var tread=stair.Find((i<13?"LowerTread":"ReturnTread")+(i%13));
                    Require(tread&&tread.GetComponent<BoxCollider>().enabled,name+" missing physical tread "+i);
                }
                for(float d=2.2f;d<route.Length-.92f;d+=.08f)
                {
                    var ground=route.PositionAt(d);
                    Require(Physics.Raycast(ground+Vector3.up*.23f,Vector3.down,.44f,~LayerMask.GetMask("WheelchairObstacle"),QueryTriggerInteraction.Ignore),
                        name+" has unsupported stair route at "+stair.InverseTransformPoint(ground));
                }
            }
            Debug.Log("CONNECTED_STAIRS_OK: both stair runs have continuous floor support from B1 to 1F.");
        }

        static void ValidateStairWalk(GameObject floor)
        {
            var free=Camera.main.GetComponent<DemoFreeCamera>();
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var toggle=typeof(DemoFreeCamera).GetMethod("ToggleStairWalk",flags);
            var walk=typeof(DemoFreeCamera).GetMethod("WalkStairs",flags);
            var active=typeof(DemoFreeCamera).GetField("stairRoute",flags);
            var eastDoor=floor.transform.Find("Details/NorthStairwell/StairDoor").GetComponent<InteractiveDoor>();
            var eye=Vector3.up*StairWalkRoute.EyeHeight;
            free.EnterFreeView();
            try
            {
                foreach(var route in floor.GetComponentsInChildren<StairWalkRoute>())
                foreach(bool ascending in new[]{true,false})
                {
                    var from=route.PositionAt(ascending?0:route.Length)+eye;
                    var to=route.PositionAt(ascending?route.Length:0)+eye;
                    free.transform.position=from;
                    toggle.Invoke(free,null);
                    Require((StairWalkRoute)active.GetValue(free)==route,"Could not enter stair walking view");
                    eastDoor.SetProgressInstant(1);
                    int maxFrames=Mathf.CeilToInt(route.Length/1.65f*60)+10;
                    for(int frame=0;frame<maxFrames&&(StairWalkRoute)active.GetValue(free);frame++)
                        walk.Invoke(free,new object[]{ascending?1f:-1f,1f/60f});
                    Require(Vector3.Distance(free.transform.position,to)<.005f&&active.GetValue(free)==null,
                        "Stair walking did not reach the destination corridor");
                }
            }
            finally
            {
                eastDoor.ResetDoor();
                free.ExitFreeView();
            }
            Debug.Log("STAIR_WALK_OK: both staircase routes enter, ascend, descend and return to free movement.");
        }
    }
}
