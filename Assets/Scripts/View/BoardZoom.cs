using ArrowEscape.Core;
using UnityEngine;
using UnityEngine.InputSystem;
namespace ArrowEscape.View
{
    /// <summary>Optional path-board pinch and desktop navigation with explicit camera math.</summary>
    public sealed class BoardZoom : MonoBehaviour
    {
        private BoardView board;
        private PointerInputAdapter adapter;
        private GameController game;
        private Camera camera;
        private float fitSize;
        private Vector3 fitPosition;
        private bool pinching,rightDragging;
        private Vector2 lastCenter,lastMouse;
        private float lastDistance;
        /// <summary>Navigation is enabled only when the fitted cells are physically small (or when forced).</summary>
        public bool CanZoom { get; private set; }
        /// <summary>Daily puzzle: zoom and pan are always available on the big board.</summary>
        public bool Forced { get => forced; set { forced = value; if (camera != null) CanZoom = value || naturalZoom; } }
        private bool forced, naturalZoom, dragging, tracking;
        private Vector2 dragStart, lastDrag;
        /// <summary>True while the camera is zoomed in from the fit.</summary>
        public bool IsZoomed => camera != null && camera.orthographicSize < fitSize - 0.0001f;
        /// <summary>Captures the current fit, including its asymmetric HUD margins.</summary>
        public void ResetFit()
        {
            board=GetComponent<BoardView>();adapter=GetComponent<PointerInputAdapter>();game=GetComponent<GameController>();camera=Camera.main;
            pinching=rightDragging=false;if(camera==null)return;
            fitSize=camera.orthographicSize;fitPosition=camera.transform.position;
            float dpi=Screen.dpi>0?Screen.dpi:160;
            naturalZoom=board!=null&&board.UsesPaths&&Screen.height/(2*fitSize*dpi)<.30f;CanZoom=forced||naturalZoom;tracking=dragging=false;
        }
        /// <summary>Restores the fitted camera without changing the board.</summary>
        public void ResetZoom(){if(camera!=null){camera.orthographicSize=fitSize;camera.transform.position=fitPosition;}}
        /// <summary>Zooms about a screen point before applying board constraints.</summary>
        public void ZoomAt(Vector2 screen,float factor)
        {
            if(!CanZoom||camera==null||!(factor>0))return;
            Vector3 pivot=World(screen);float old=camera.orthographicSize;
            float size=ZoomMath.ClampSize(old*factor,fitSize,Screen.height,Screen.dpi);Vector3 pos=camera.transform.position;
            pos.x=ZoomMath.PivotCenter(pos.x,pivot.x,old,size);pos.y=ZoomMath.PivotCenter(pos.y,pivot.y,old,size);
            camera.orthographicSize=size;camera.transform.position=pos;Clamp();
        }
        private Vector3 World(Vector2 screen)=>camera.ScreenToWorldPoint(new Vector3(screen.x,screen.y,-camera.transform.position.z));
        private void Pan(Vector2 from,Vector2 to){camera.transform.position+=World(from)-World(to);Clamp();}
        private void Clamp()
        {
            if(Mathf.Abs(camera.orthographicSize-fitSize)<.00001f){camera.transform.position=fitPosition;return;}
            // Pan limits follow the drawn shape (the camera fit is centred on it, not on the grid centre).
            Vector3 p=camera.transform.position;Bounds b=board.ContentBounds.size.sqrMagnitude>0?board.ContentBounds:board.WorldBounds;b.Expand(new Vector3(1f,3f,0));
            p.x=b.center.x+ZoomMath.ClampCenter(p.x-b.center.x,b.extents.x,camera.orthographicSize*camera.aspect);
            p.y=b.center.y+ZoomMath.ClampCenter(p.y-b.center.y,b.extents.y,camera.orthographicSize);camera.transform.position=p;
        }
        private void Update()
        {
            if(!CanZoom||camera==null||!board.IsVisible||(game!=null&&game.IsPaused)){pinching=rightDragging=false;return;}
            var touches=Touchscreen.current;int first=-1,second=-1;
            if(touches!=null)for(int i=0;i<touches.touches.Count;i++)if(touches.touches[i].press.isPressed){if(first<0)first=i;else {second=i;break;}}
            if(second>=0)
            {
                Vector2 a=touches.touches[first].position.ReadValue(),b=touches.touches[second].position.ReadValue();Vector2 center=(a+b)*.5f;float distance=Vector2.Distance(a,b);
                if(!pinching && adapter!=null && (!adapter.ShouldAcceptPointerDown(a)||!adapter.ShouldAcceptPointerDown(b)))return;
                if(adapter!=null)adapter.CancelForNavigation();
                if(pinching&&distance>1&&lastDistance>1){ZoomAt(center,lastDistance/distance);Pan(lastCenter,center);}
                pinching=true;lastCenter=center;lastDistance=distance;return;
            }
            pinching=false;
            // One finger (or the left mouse button) drags the zoomed board; the input adapter cancels the tap once it moves.
            bool down=false;Vector2 point=default;
            if(first>=0){down=true;point=touches.touches[first].position.ReadValue();}
            else if(Mouse.current!=null&&Mouse.current.leftButton.isPressed){down=true;point=Mouse.current.position.ReadValue();}
            if(down&&IsZoomed)
            {
                if(!tracking){tracking=adapter==null||adapter.ShouldAcceptPointerDown(point);dragStart=lastDrag=point;dragging=false;}
                else{if(!dragging&&ZoomMath.CancelsTap(point.x-dragStart.x,point.y-dragStart.y))dragging=true;if(dragging){if(adapter!=null)adapter.CancelForNavigation();Pan(lastDrag,point);}lastDrag=point;}
            }
            else tracking=dragging=false;
            if(first>=0)return;
            Mouse mouse=Mouse.current;if(mouse==null)return;Vector2 position=mouse.position.ReadValue();
            if(adapter!=null&&!adapter.ShouldAcceptPointerDown(position)){rightDragging=false;return;}
            float scroll=mouse.scroll.ReadValue().y;if(scroll!=0){if(adapter!=null)adapter.CancelForNavigation();ZoomAt(position,Mathf.Pow(.9f,scroll/120));}
            if(mouse.rightButton.isPressed){if(adapter!=null)adapter.CancelForNavigation();if(rightDragging)Pan(lastMouse,position);rightDragging=true;lastMouse=position;}else rightDragging=false;
        }
    }
}
