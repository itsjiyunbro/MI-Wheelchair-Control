using System;
using System.Linq;
using UnityEngine;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static partial class ConvergenceHallFirstFloor
    {
        static void TeachingRoom(string code,R bounds,string entrance,string title="",bool glazed=false,
            bool doubleDoor=false,bool sideEntry=false,bool computers=false)
        {
            Room(code,bounds,entrance,title,glazed,doubleDoor);
            bool frontAlongV=sideEntry||entrance[0]=='v';
            float uSize=bounds.u1-bounds.u0,vSize=bounds.v1-bounds.v0;
            float width=frontAlongV?uSize:vSize,depth=frontAlongV?vSize:uSize;
            float yaw=sideEntry||entrance=="v0"?-90:entrance=="v1"?90:entrance=="u0"?180:0;
            var shell=architecture.Find("Room"+code);
            var interior=Face(shell,"ClassroomInterior",At((bounds.u0+bounds.u1)/2,(bounds.v0+bounds.v1)/2),yaw);

            const float aisleHalfWidth=.75f;
            float deskWidth=1.5f*P.FurnitureScale;
            float columnPitch=deskWidth+.17f;
            int columnsPerSide=Mathf.FloorToInt((width/2-aisleHalfWidth-.36f+.17f)/columnPitch);
            if(computers)columnsPerSide=Mathf.Min(3,columnsPerSide);
            bool useSideAisle=sideEntry||columnsPerSide==0;
            float aisleX=useSideAisle?width/2-.78f:0;
            int columns=useSideAisle?Mathf.Max(1,Mathf.FloorToInt((width-2.0f)/columnPitch)):columnsPerSide*2;
            float firstDesk=depth/2-2.02f;
            float lastDesk=-depth/2+(sideEntry?1.10f:useSideAisle?2.52f:1.58f);
            float rowPitch=computers?1.75f:depth<=6f?1.38f:1.55f;
            int rows=0;
            for(float z=firstDesk;z>=lastDesk-.001f;z-=rowPitch)
            {
                for(int column=0;column<columns;column++)
                {
                    float x;
                    if(useSideAisle)x=-width/2+.42f+deskWidth/2+column*columnPitch;
                    else
                    {
                        int side=column<columnsPerSide?-1:1;
                        x=side*(aisleHalfWidth+deskWidth/2+(column%columnsPerSide)*columnPitch);
                    }
                    var desk=P.Instance(interior,"TwoSeatFoldingDesk",$"Desk_{rows}_{column}",new Vector3(x,0,z));
                    foreach(int seat in new[]{-1,1})
                    {
                        P.Instance(interior,"BlueMeshChair",$"Chair_{rows}_{column}_{seat}",new Vector3(x+seat*.36f,0,z-.64f));
                        if(computers)AddComputer(desk,seat);
                    }
                }
                rows++;
            }

            float boardWidth=Mathf.Min(5.6f,width-.65f);
            var board=P.Group(interior,"FrontWhiteboard");
            board.localPosition=new Vector3(0,1.57f,depth/2-.18f);
            Box(board,"AluminiumFrame",Vector3.zero,new Vector3(boardWidth,1.15f,.043f),silver);
            Box(board,"WritingSurface",new Vector3(0,0,-.023f),new Vector3(boardWidth-.038f,1.11f,.010f),white);
            Box(board,"MarkerTray",new Vector3(0,-.586f,-.048f),new Vector3(boardWidth,.027f,.092f),silver);
            Box(board,"Eraser",new Vector3(boardWidth*.35f,-.558f,-.066f),new Vector3(.10f,.029f,.042f),black);
            Box(board,"Marker",new Vector3(boardWidth*.35f-.15f,-.568f,-.063f),new Vector3(.093f,.012f,.012f),navy);

            float lecternX=useSideAisle?-width/2+.85f:-Mathf.Min(width*.25f,2.1f);
            var lectern=P.Instance(interior,"ElectronicLectern","Lectern",new Vector3(lecternX,0,depth/2-.82f));
            lectern.localRotation=Quaternion.Euler(0,180,0);
            if(depth>6f)
            {
                var projector=P.Group(interior,"CeilingProjector");
                projector.localPosition=new Vector3(0,H-.26f,depth/2-3.35f);
                Box(projector,"Mount",new Vector3(0,.14f,0),new Vector3(.055f,.24f,.055f),silver);
                Box(projector,"Housing",Vector3.zero,new Vector3(.38f,.12f,.28f),white);
                P.Cylinder(projector,"Lens",new Vector3(.085f,0,.153f),new Vector3(.082f,.016f,.082f),black,Quaternion.Euler(90,0,0));
                Box(interior,"ScreenCassette",new Vector3(0,2.42f,depth/2-.23f),new Vector3(Mathf.Min(3.1f,boardWidth),.07f,.085f),white);
            }

            int lightColumns=Mathf.Max(1,Mathf.RoundToInt(width/3f));
            int lightRows=Mathf.Max(2,Mathf.RoundToInt(depth/2.7f));
            for(int row=0;row<lightRows;row++)
                for(int column=0;column<lightColumns;column++)
                {
                    var at=new Vector3(width*((column+.5f)/lightColumns-.5f),H-.039f,depth*((row+.5f)/lightRows-.5f));
                    Box(interior,"CeilingPanelFrame",at,new Vector3(1.20f,.032f,.31f),silver);
                    Box(interior,"CeilingPanelDiffuser",at+Vector3.down*.019f,new Vector3(1.15f,.011f,.266f),lamp);
                }

            // Save the clear approach and turning area for testing the generated prefab.
            var aisleEntry=P.Group(interior,"AisleEntry");
            aisleEntry.localPosition=new Vector3(aisleX,0,sideEntry?0:-depth/2+1.07f);
            P.Group(interior,"AisleFront").localPosition=new Vector3(aisleX,0,depth/2-.88f);
            Debug.Log($"FIRST_FLOOR_CLASSROOM: {code}, {rows*columns} desks, {rows*columns*2} seats{(computers?", PC lab":"")}");
        }

        static void AddComputer(Transform desk,int seat)
        {
            var pc=P.Group(desk,seat<0?"ComputerLeft":"ComputerRight");
            pc.localPosition=new Vector3(seat*.379f,.756f,.025f);
            Box(pc,"DisplayStand",new Vector3(0,.008f,.102f),new Vector3(.20f,.016f,.13f),black);
            Box(pc,"DisplayPost",new Vector3(0,.071f,.114f),new Vector3(.036f,.133f,.038f),silver);
            Box(pc,"DisplayBezel",new Vector3(0,.231f,.113f),new Vector3(.45f,.27f,.029f),black);
            Box(pc,"DisplaySurface",new Vector3(0,.232f,.097f),new Vector3(.423f,.241f,.004f),navy);
            Box(pc,"Keyboard",new Vector3(-.025f,.012f,-.14f),new Vector3(.31f,.024f,.096f),black);
            Box(pc,"Mouse",new Vector3(.182f,.014f,-.139f),new Vector3(.048f,.029f,.073f),black);
        }

        static void ValidateClassroomAccess(GameObject floor,WheelchairCollisionGuard guard)
        {
            int rooms=0;
            foreach(Transform shell in floor.transform.Find("RoomsAndCores"))
            {
                var room=shell.Find("ClassroomInterior");
                if(!room)continue;
                var door=shell.GetComponentInChildren<InteractiveDoor>();
                door.SetProgressInstant(1);
                Physics.SyncTransforms();
                var entry=room.Find("AisleEntry").position;
                var front=room.Find("AisleFront").position;
                var threshold=door.transform.position;
                // Enter perpendicular to the doorway, then follow the reserved aisle.
                var inRoom=threshold+Vector3.Project(entry-threshold,door.transform.forward);
                var points=new[]{threshold-door.transform.forward*.65f,inRoom,entry,front};
                for(int leg=1;leg<points.Length;leg++)
                {
                    var from=points[leg-1];
                    var delta=points[leg]-from;
                    if(delta.sqrMagnitude<.0001f)continue;
                    var rotation=Quaternion.LookRotation(delta);
                    int steps=Mathf.CeilToInt(delta.magnitude/.1f);
                    for(int step=0;step<=steps;step++)
                    {
                        var ground=from+delta*(step/(float)steps);
                        Require(guard.CanOccupy(ground+Vector3.up*.5f,rotation),
                            $"{shell.name} aisle obstructed at {room.InverseTransformPoint(ground)}");
                        Require(Physics.RaycastAll(ground+Vector3.up*.1f,Vector3.down,.2f,~LayerMask.GetMask("WheelchairObstacle"),QueryTriggerInteraction.Ignore)
                            .Any(hit=>hit.transform.IsChildOf(floor.transform.Find("Floors"))&&hit.normal.y>.9f),
                            $"{shell.name} aisle lacks floor at {room.InverseTransformPoint(ground)}");
                    }
                }
                foreach(var turn in new[]{inRoom,entry,front})
                    for(int angle=0;angle<360;angle+=15)
                        Require(guard.CanOccupy(turn+Vector3.up*.5f,Quaternion.Euler(0,angle,0)),
                            $"{shell.name} turning area obstructed at {room.InverseTransformPoint(turn)}");
                door.ResetDoor();
                rooms++;
            }
            Require(rooms==12,"First-floor classroom count differs");
            Debug.Log($"FIRST_FLOOR_CLASSROOM_ACCESS_OK: {rooms} furnished classrooms, open door approaches, continuous aisle floors and wheelchair turning space.");
        }
    }
}
