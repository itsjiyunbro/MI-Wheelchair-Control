using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallEntranceBoards
    {
        static Font font;
        static readonly Color Ink=new Color(.07f,.08f,.09f);
        static Text Label(Transform parent,string name,string content,Vector3 at,Vector2 size,int pixels,Color color,TextAnchor anchor=TextAnchor.MiddleCenter)
        {
            if(!font)font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/ConvergenceHall/NotoSansCJKkr-Regular.otf");
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(Text));go.transform.SetParent(parent,false);go.transform.localPosition=at;go.transform.localScale=Vector3.one*.001f;
            go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;((RectTransform)go.transform).sizeDelta=size*1000;
            var text=go.GetComponent<Text>();text.font=font;text.text=content;text.fontSize=pixels;text.resizeTextForBestFit=true;text.resizeTextMinSize=Mathf.Min(5,pixels);text.resizeTextMaxSize=pixels;text.color=color;text.alignment=anchor;text.raycastTarget=false;return text;
        }
        static void Crest(Transform parent)
        {
            var g=P.Group(parent,"ModeledUniversityEmblem");g.localPosition=new Vector3(-1.39f,2.30f,-.045f);
            var gray=P.Mat("DonorEmblemGray",new Color(.34f,.37f,.42f),.30f);var white=P.Mat("DonorEmblemInset",new Color(.69f,.71f,.70f));
            P.Cylinder(g,"OuterMetalDisc",Vector3.zero,new Vector3(.27f,.006f,.27f),gray,Quaternion.Euler(90,0,0));
            P.Cylinder(g,"InsetDisc",new Vector3(0,0,-.008f),new Vector3(.213f,.002f,.213f),white,Quaternion.Euler(90,0,0));
            P.Box(g,"Shield",new Vector3(0,-.003f,-.014f),new Vector3(.133f,.128f,.004f),gray);
            P.Box(g,"ShieldHorizontal",new Vector3(0,-.016f,-.018f),new Vector3(.125f,.007f,.002f),white);
            P.Cylinder(g,"RoundSymbol",new Vector3(0,.035f,-.019f),new Vector3(.039f,.002f,.039f),white,Quaternion.Euler(90,0,0));
            P.Cylinder(g,"RoundInset",new Vector3(0,.035f,-.022f),new Vector3(.025f,.002f,.025f),gray,Quaternion.Euler(90,0,0));
            var a=P.Box(g,"DiagonalA",new Vector3(0,-.035f,-.020f),new Vector3(.112f,.014f,.002f),white);a.localRotation=Quaternion.Euler(0,0,26);
            var b=P.Box(g,"DiagonalB",new Vector3(0,-.035f,-.022f),new Vector3(.112f,.014f,.002f),white);b.localRotation=Quaternion.Euler(0,0,-26);
            string name="YONSEI UNIVERSITY";
            for(int i=0;i<name.Length;i++){float angle=Mathf.Lerp(-110,110,i/(float)(name.Length-1));float rad=angle*Mathf.Deg2Rad;var label=Label(g,"EmblemLetter"+i,name[i].ToString(),new Vector3(Mathf.Sin(rad)*.115f,Mathf.Cos(rad)*.115f,-.010f),new Vector2(.022f,.030f),16,new Color(.8f,.82f,.83f));label.transform.localRotation=Quaternion.Euler(0,0,-angle);}
            Label(g,"Established","1885",new Vector3(0,-.092f,-.012f),new Vector2(.065f,.025f),13,gray.color);
        }
        public static void DonorWall(Transform parent)
        {
            var ink=P.Mat("DonorRaisedLetterMetal",new Color(.28f,.30f,.33f),.30f);
            var gold=P.Mat("NativeDonorGold",new Color(.66f,.52f,.27f),.48f);gold.SetFloat("_Metallic",.48f);EditorUtility.SetDirty(gold);
            Crest(parent);
            Label(parent,"RaisedHeading","연세대학교 미래캠퍼스 컨버전스홀 기부자 명예의 전당",new Vector3(.16f,2.32f,-.038f),new Vector2(2.69f,.13f),62,ink.color);
            Label(parent,"KoreanRecognition","컨버전스홀 건축을 후원해주신 분들의 값진 사랑과 마음을 여기에 기립니다",new Vector3(.15f,2.13f,-.040f),new Vector2(2.75f,.062f),25,Ink);
            Label(parent,"EnglishRecognition","In grateful recognition of those who contributed to the construction of the Convergence Hall",new Vector3(.15f,2.04f,-.040f),new Vector2(2.75f,.055f),19,Ink);
            string[][] names={
                new[]{"이혜영"},new[]{"김한성","김현수","서홍철"},
                new[]{"황재훈","강승일","김상범","남은우","백소연","윤상균","윤영철","이경종","이규식"},
                new[]{"이상수","임걸","임대운","전혜선","정형선","팽기정",""},
                new[]{"최윤석","한영훈","권명중","권운오","김종현","백혜림","유일","이인재","허상희"},
                new[]{"주선미","김창수","나성룡","박용석","신혜원","오영교","양현종","정찬문"},
                new[]{"김재영","김연기","심에린","이충희","정해욱","최우영","코리아여행사","김남숙","류종대","문병채","박영철"},
                new[]{"이상미","이승훈","","곽기홍","권지예","김만수","김미향","김범식","김수경","김은희","김이영"},
                new[]{"김준일","김택중","김판석","김현정","김형순","김형택","노선표","박민호","박준수","박창원","서광덕"},
                new[]{"성재호","신동훈","신장우","오병근","윤동욱","윤영로","윤현석","이규도","이상용","이상우","이영석"},
                new[]{"임병혁","장기영","장희숙","정용현","주용규","진은영","최영남","최정원","최종원","하홍호","화공과69동기회"},
                new[]{"홍승필","황대연","황지영"}};
            const float pitch=.245f;float start=-1.38f;
            string[] headings={"3천만원 이상","2천만원 이상","1천만원 이상","5백만원 이상","3백만원 이상","1백만원 이상"};int[] columns={0,1,2,4,5,6};int[] spans={1,1,2,1,1,6};
            for(int i=0;i<headings.Length;i++)
            {
                float width=spans[i]*pitch-.035f,x=start+columns[i]*pitch+(spans[i]-1)*pitch/2;
                Label(parent,"DonationBand"+i,headings[i],new Vector3(x,1.91f,-.043f),new Vector2(width,.068f),21,Ink);
                P.Box(parent,"DonationBandRule"+i,new Vector3(x,1.866f,-.040f),new Vector3(width,.006f,.005f),ink);
            }
            for(int col=0;col<names.Length;col++)for(int row=0;row<names[col].Length;row++)
            {
                float x=start+col*pitch,y=1.775f-row*.110f;
                var plaque=P.Group(parent,"DonorPlate_"+col+"_"+row);plaque.localPosition=new Vector3(x,y,-.040f);
                P.Box(plaque,"RaisedGoldPlate",Vector3.zero,new Vector3(.205f,.083f,.009f),gold);
                if(names[col][row]!="")Label(plaque,"DonorName",names[col][row],new Vector3(0,0,-.006f),new Vector2(.185f,.067f),28,Ink);
            }
        }
        static string Row(string number,string korean,string english)=>number+"|"+korean+"|"+english;
        public static void Directory(Transform parent)
        {
            var paper=P.Mat("NativeDirectoryWhite",new Color(.93f,.94f,.92f),.12f);var dark=P.Mat("NativeDirectoryBlack",new Color(.015f,.02f,.025f),.10f);var gray=P.Mat("NativeDirectoryBand",new Color(.56f,.59f,.56f));
            P.Box(parent,"DirectoryWhiteBody",new Vector3(0,0,-.020f),new Vector3(2.56f,1.22f,.008f),paper);
            P.Box(parent,"DirectoryBlackHeader",new Vector3(0,.535f,-.027f),new Vector3(2.56f,.15f,.003f),dark);
            Label(parent,"BuildingKorean","컨버전스홀",new Vector3(-1.07f,.56f,-.030f),new Vector2(.34f,.065f),39,Color.white,TextAnchor.MiddleLeft);
            Label(parent,"BuildingEnglish","Convergence Hall",new Vector3(-1.00f,.51f,-.030f),new Vector2(.49f,.040f),17,Color.white,TextAnchor.MiddleLeft);
            P.Box(parent,"FloorBand",new Vector3(0,.422f,-.027f),new Vector3(2.56f,.075f,.003f),gray);
            string[][][] floors={
                new[]{new[]{Row("B101","SLI 첨단강의실","SLI Class Room"),Row("B102","SLI 첨단강의실","SLI Class Room"),Row("B103","SLI 첨단강의실","SLI Class Room"),Row("B104","RC프로그램활동실","RC Program Activity Room"),Row("B105","SLI 첨단강의실","SLI Class Room"),Row("B106","SLI 첨단강의실","SLI Class Room"),Row("B107","SLI 첨단강의실","SLI Class Room"),Row("B108","SLI 첨단강의실","SLI Class Room"),Row("B109","SLI 첨단강의실","SLI Class Room"),Row("B110","SLI 첨단강의실","SLI Class Room"),Row("B111","학생휴게실","Student Lounge"),Row("B112","SU 첨단강의실","SU Class Room"),Row("B113","SU 첨단강의실","SU Class Room"),Row("B114","SU 첨단강의실","SU Class Room")},new[]{Row("B115","SU 첨단강의실","SU Class Room"),Row("B124","상담실라운지","Consulting Lounge"),Row("B124-1","심리상담센터/인권센터","Counseling Center/Human Rights Center"),Row("B124-2","인권센터장실","Director of Human Rights Center"),Row("B124-3","심리상담센터장실","Director of Counseling Center")}},
                new[]{new[]{Row("101","BK21 융합강의실","BK21 Lecture Room"),Row("102","강의실","Lecture Room"),Row("103","강의실","Lecture Room"),Row("104","SLI 첨단강의실","SLI Class Room"),Row("105","SLI 첨단강의실","SLI Class Room"),Row("106","학사지도상담실","Academic Advisors Counseling Room"),Row("107","MOOC스튜디오","MOOC Studio"),Row("107-1","셀프스튜디오","Self Studio"),Row("108","융합교육연구실","Convergence Education Research Laboratory"),Row("109","융합교육연구실","Convergence Education Research Laboratory"),Row("110","학생(등대)활동실","Lighthouse Beacon"),Row("111","AI융합과학원","Institute of AI Convergence Science"),Row("112","BK21 Creative","Thinking Room"),Row("113","AI융합과학원","Institute of AI Convergence Science")},Enumerable.Range(114,10).Select(n=>Row(n.ToString(),"학습공동체활동실","Learning Community Room")).ToArray()},
                new[]{new[]{Row("201","학생창업보육실","SW Startups Incubator"),Row("202","학생창업보육실","SW Startups Incubator"),Row("203","피지컬AI프로젝트실","Physical AI Project Room"),Row("203-1","의료AI프로젝트실","Healthcare AI Project Room"),Row("204","세미나실","Seminar Room"),Row("205","문서보존실","Graduate School Document Storage Room"),Row("206","미래혁신지원센터","MIRAE Innovation Support Center"),Row("207","대학원 부원장실","Graduate School Associate Dean's Office"),Row("208","대학원 행정팀","Administrative Team, Graduate School"),Row("209","커리어개발룸","Career Planning Room"),Row("210","관리실","Maintenance Room"),Row("211","BK21 미래혁신LAB","BK21 Entrepreneurship LAB")},new[]{Row("212","학사지도상담실1","Academic Advising & Counseling Office"),Row("213","학사지도상담실2","Academic Advising & Counseling Office"),Row("214","RC학사지도실","Academic Advising Office"),Row("215","RC라운지","RC Lounge"),Row("216","서버실","Server Room"),Row("217","RC융합대학 행정팀","Administrative Team, College of RC Convergence"),Row("219","RC융합대학 교학장실","Dean's Office, College of RC Convergence"),Row("295","전기실","Electric Room"),Row("296","방재실","Emergency Room"),Row("296-1","MDF실","Main Distributed Frame Room"),Row("297","기계실","Machine Room")}},
                new[]{new[]{Row("301","창고","Storage"),Row("302","첨단강의실","SW Lecture Room"),Row("303","SW Help Desk","SW Help Desk"),Row("304","워크샵실","SW Workshop Room")}.Concat(Enumerable.Range(305,8).Select(n=>Row(n.ToString(),"프로젝트실","SW Project Room"))).Concat(new[]{Row("313","창고","Storage"),Row("314","PC실습실","Computer Lab")}).ToArray(),new[]{Row("315","PC실습실","Computer Lab"),Row("316","PC실습실","Computer Lab"),Row("318-1","첨단강의실","SW Lecture Room"),Row("318-2","첨단강의실","SW Lecture Room"),Row("319","첨단강의실","SW Lecture Room"),Row("320","첨단강의실","SW Lecture Room"),Row("321","첨단강의실","SW Lecture Room"),Row("322","첨단강의실","SW Lecture Room"),Row("323","첨단강의실","SW Lecture Room")}}
            };
            string[] floorNames={"B1","1","2","3"};const float floorWidth=.64f,cellWidth=.312f,rowH=.069f;
            for(int f=0;f<4;f++)
            {
                float floorX=-.96f+f*floorWidth;Label(parent,"FloorHeading"+f,floorNames[f],new Vector3(floorX,.423f,-.031f),new Vector2(.50f,.064f),29,Ink);
                for(int side=0;side<2;side++)for(int row=0;row<floors[f][side].Length;row++)
                {
                    string[] value=floors[f][side][row].Split('|');float x=floorX+(side==0?-.159f:.159f),y=.345f-row*rowH;
                    var cell=P.Group(parent,"RoomCell_"+value[0]);cell.localPosition=new Vector3(x,y,-.028f);
                    P.Box(cell,"CellBorder",Vector3.zero,new Vector3(cellWidth,rowH-.002f,.002f),gray);P.Box(cell,"CellWhiteFace",new Vector3(0,0,-.002f),new Vector3(cellWidth-.003f,rowH-.006f,.001f),paper);
                    Label(cell,"RoomCode",value[0],new Vector3(-.117f,.008f,-.004f),new Vector2(.070f,.043f),18,Ink,TextAnchor.MiddleLeft);
                    Label(cell,"KoreanRoomName",value[1],new Vector3(.035f,.013f,-.004f),new Vector2(.230f,.030f),20,Ink,TextAnchor.MiddleLeft);
                    Label(cell,"EnglishRoomName",value[2],new Vector3(.035f,-.017f,-.004f),new Vector2(.230f,.022f),10,Ink,TextAnchor.MiddleLeft);
                }
                if(f<3)P.Box(parent,"FloorSeparator"+f,new Vector3(floorX+.32f,-.105f,-.029f),new Vector3(.003f,1.00f,.003f),gray);
            }
        }
    }
}

