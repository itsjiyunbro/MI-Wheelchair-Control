using UnityEngine;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static partial class ConvergenceHallFirstFloor
    {
        static void OpenWestHallFront(Transform hall,Material finish)
        {
            foreach(string name in new[]{"FrontLeftPier","FrontRightPier","Lintel"})
            {
                hall.Find(name).gameObject.SetActive(false);
                hall.Find(name+"Collision").gameObject.SetActive(false);
            }

            // The wall turns the corner at the lift face, leaving the hall open to ceiling height.
            var sideWall=hall.Find("CoreLeftWall");
            var insetWall=hall.Find("LeftWallElevator/RecessedWallSurround/LeftWall");
            float left=sideWall.localPosition.x-sideWall.localScale.x/2;
            var insetEdge=hall.InverseTransformPoint(insetWall.TransformPoint(new Vector3(-.5f,0,-.5f)));
            float right=insetEdge.x;
            float depth=Mathf.Max(.12f,insetEdge.z+.012f);
            Require(right>left&&right<0,"Invalid west lift side recess bounds");
            Box(hall,"WestLiftCornerWall",new Vector3((left+right)/2,H/2,depth/2),
                new Vector3(right-left,H,depth),finish,true);
            Box(hall,"CornerSkirting",new Vector3((left+right)/2,.0425f,-.006f),
                new Vector3(right-left,.085f,.012f),black);
        }

        static void BuildRoom106Exit()
        {
            const float corridorEnd=15.8f,midpoint=58.2f,vestibuleEnd=19.5f;
            var vestibule=new R(corridorEnd,57,vestibuleEnd,59.4f);
            var slab=Rect(root.Find("Floors"),"Room106VestibuleFloor",vestibule,-.09f,.18f,tile);
            slab.gameObject.AddComponent<BoxCollider>();
            Rect(roofs,"Room106VestibuleCeiling",vestibule,H+.05f,.10f,white);
            Wall(architecture,"Room106VestibuleLeft",At(17.65f,57),90,3.7f);
            Wall(architecture,"Room106VestibuleRight",At(17.65f,59.4f),-90,3.7f);
            Wall(architecture,"Room106VestibuleBack",At(vestibuleEnd,midpoint),0,2.4f);

            var entrance=Face(architecture,"Room106OutsideEntrance",At(corridorEnd,midpoint),180);
            AddVestibuleGlazing(entrance,true);
            var outer=Face(architecture,"Room106OuterGlazing",At(18.5f,midpoint),180);
            AddVestibuleGlazing(outer,false);
            AddExitPictogram(entrance);
            Downlight(16.95f,midpoint);

            // Brick returns seen through the second glazed frame.
            var mortar=Surface("1F_ExitBrickMortar",new Color(.69f,.62f,.54f));
            var brick=Surface("1F_ExitBrick",new Color(.58f,.31f,.23f));
            var brickShade=Surface("1F_ExitBrickShade",new Color(.51f,.27f,.21f));
            foreach(float x in new[]{-.92f,.92f})
            {
                Box(outer,"BrickPierBacking",new Vector3(x,H/2,.64f),new Vector3(.49f,H,.42f),mortar,true);
                for(int row=0;row<38;row++)
                {
                    float left=x-.24f,right=x+.24f;
                    for(float start=left-(row%2)*.12f;start<right;start+=.24f)
                    {
                        float a=Mathf.Max(left,start),b=Mathf.Min(right,start+.23f);
                        if(b-a<.015f)continue;
                        Box(outer,"Brick",new Vector3((a+b)/2,.038f+row*.074f,.425f),
                            new Vector3(b-a,.064f,.023f),(row+Mathf.RoundToInt(start*10))%4==0?brickShade:brick);
                    }
                }
            }
        }

        static void AddVestibuleGlazing(Transform frame,bool operable)
        {
            const float split=-.12f,innerEdge=1.14f,leafTop=2.44f;
            const float leafWidth=innerEdge-split-.054f;
            float leafCentre=(innerEdge+split+.054f)/2;
            foreach(float x in new[]{-1.17f,1.17f})
                Box(frame,"OuterJamb",new Vector3(x,H/2,0),new Vector3(.06f,H,.09f),black,true);
            Box(frame,"Head",new Vector3(0,H-.032f,0),new Vector3(2.4f,.064f,.09f),black);
            Box(frame,"TransomBar",new Vector3(0,leafTop+.022f,0),new Vector3(2.4f,.066f,.09f),black);
            Box(frame,"TransomGlass",new Vector3(0,2.645f,0),new Vector3(2.28f,.34f,.019f),glass);
            Box(frame,"SidelightMullion",new Vector3(split,leafTop/2,0),new Vector3(.064f,leafTop,.09f),black,true);
            float paneWidth=split-.032f+innerEdge;
            float paneCentre=-innerEdge+paneWidth/2;
            Box(frame,"FixedSidelight",new Vector3(paneCentre,leafTop/2,0),new Vector3(paneWidth,leafTop-.04f,.022f),glass,true);
            Box(frame,"SidelightSill",new Vector3(paneCentre,.027f,0),new Vector3(paneWidth,.054f,.075f),black);

            var leaf=P.Group(frame,"GlassDoorLeaf");
            leaf.localPosition=new Vector3(leafCentre,0,0);
            Box(leaf,"ClearPane",new Vector3(0,leafTop/2,0),new Vector3(leafWidth-.088f,leafTop-.133f,.022f),glass);
            foreach(float x in new[]{-leafWidth/2+.032f,leafWidth/2-.032f})
                Box(leaf,"DoorStile",new Vector3(x,leafTop/2,0),new Vector3(.064f,leafTop-.023f,.068f),black);
            foreach(float y in new[]{.042f,leafTop-.041f})
                Box(leaf,"DoorRail",new Vector3(0,y,0),new Vector3(leafWidth,.063f,.068f),black);
            float handle=-leafWidth/2+.033f;
            foreach(float side in new[]{-1f,1f})
            {
                Box(leaf,"HandlePlate",new Vector3(handle,.94f,side*.037f),new Vector3(.040f,.24f,.014f),silver);
                P.Tube(leaf,"LeverHandle",new Vector3(handle,.97f,side*.064f),new Vector3(handle+.11f,.97f,side*.064f),.020f,silver);
            }
            var collision=leaf.gameObject.AddComponent<BoxCollider>();
            collision.center=new Vector3(0,leafTop/2,0);
            collision.size=new Vector3(leafWidth,leafTop,.068f);
            leaf.gameObject.layer=obstacle;
            if(!operable)return;
            frame.gameObject.AddComponent<InteractiveDoor>().Configure("106호 앞 외부 출입문",
                new[]{new InteractiveDoor.Motion{target=leaf,closedPosition=leaf.localPosition,
                    hinge=new Vector3(innerEdge,0,0),closedRotation=Quaternion.identity,angle=95}},
                new[]{collision},new Vector3(leafCentre,1.1f,-.12f));
        }

        static void AddExitPictogram(Transform entrance)
        {
            var sign=P.Group(entrance,"ExitPictogram");
            sign.localPosition=new Vector3(0,2.66f,-.071f);
            var green=Surface("1F_ExitGreen",new Color(.025f,.38f,.23f));
            Box(sign,"Housing",Vector3.zero,new Vector3(.32f,.25f,.056f),white);
            Box(sign,"GreenPanel",new Vector3(0,0,-.031f),new Vector3(.287f,.221f,.009f),green);
            Box(sign,"Doorway",new Vector3(.074f,0,-.037f),new Vector3(.065f,.158f,.005f),white);
            Vector3 Point(float x,float y)=>new Vector3(x,y,-.043f);
            P.Cylinder(sign,"Head",Point(-.042f,.066f),new Vector3(.035f,.003f,.035f),white,Quaternion.Euler(90,0,0));
            var shoulder=Point(-.025f,.039f);
            var hip=Point(-.045f,-.015f);
            P.Tube(sign,"Torso",shoulder,hip,.028f,white);
            P.Tube(sign,"ForwardArm",shoulder,Point(.016f,.006f),.017f,white);
            P.Tube(sign,"RearArm",shoulder,Point(-.078f,.006f),.016f,white);
            P.Tube(sign,"RearLeg",hip,Point(-.092f,-.079f),.019f,white);
            P.Tube(sign,"ForwardThigh",hip,Point(.013f,-.022f),.021f,white);
            P.Tube(sign,"ForwardShin",Point(.013f,-.022f),Point(.026f,-.068f),.019f,white);
        }

        static void ValidateRoom106Exit(GameObject floor,WheelchairCollisionGuard guard)
        {
            var hall=floor.transform.Find("RoomsAndCores/SouthElevatorHall");
            var gapWall=hall.Find("WestLiftCornerWall").GetComponent<BoxCollider>();
            foreach(float x in new[]{-.44f,0,.44f})
            foreach(float y in new[]{-.44f,0,.44f})
            {
                var ray=new Ray(gapWall.transform.TransformPoint(new Vector3(x,y,-1)),hall.forward);
                Require(gapWall.Raycast(ray,out _,1),"West lift side recess wall is incomplete");
            }
            foreach(float x in new[]{.6f,1.65f})
            foreach(float y in new[]{1.3f,H-.12f})
                Require(!Physics.Raycast(hall.TransformPoint(new Vector3(x,y,-.35f)),hall.forward,.65f,LayerMask.GetMask("WheelchairObstacle")),
                    "West hall still has an entrance pier or lintel");
            for(float z=-.8f;z<5.1f;z+=.12f)
                Require(guard.CanOccupy(hall.TransformPoint(new Vector3(.6f,.5f,z)),hall.rotation),"West hall entrance or approach blocked");

            var entrance=floor.transform.Find("RoomsAndCores/Room106OutsideEntrance");
            var door=entrance.GetComponent<InteractiveDoor>();
            var leafCentre=door.Motions[0].closedPosition.x;
            door.ResetDoor();
            Require(!guard.CanOccupy(entrance.TransformPoint(new Vector3(leafCentre,.5f,0)),entrance.rotation),
                "Closed 106 exterior door can be crossed");
            door.SetProgressInstant(1);
            for(float z=-.7f;z<1.5f;z+=.1f)
            {
                var ground=entrance.TransformPoint(new Vector3(leafCentre,0,z));
                Require(guard.CanOccupy(ground+Vector3.up*.5f,entrance.rotation),"Open 106 exterior door blocks the route");
                Require(Physics.Raycast(ground+Vector3.up*.1f,Vector3.down,.17f,~LayerMask.GetMask("WheelchairObstacle")),
                    "106 exterior route is missing vestibule floor");
            }
            door.ResetDoor();
            Debug.Log("ROOM_106_EXIT_OK: side recess wall sealed, west elevator hall accessible, and exterior door crosses continuous vestibule floor.");
        }
    }
}
