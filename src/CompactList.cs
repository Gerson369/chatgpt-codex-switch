using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
class CompactList:ListBox {
 public event EventHandler ViewportChanged;
 [DllImport("user32.dll")]static extern bool ShowScrollBar(IntPtr hwnd,int bar,bool show);
 bool hiding;
 protected override CreateParams CreateParams {get{var p=base.CreateParams;p.Style&=~0x00200000;return p;}}
 protected override void WndProc(ref Message m) {
  base.WndProc(ref m);
  if(!hiding&&IsHandleCreated&&(m.Msg==0xF||m.Msg==5||m.Msg==0x115||m.Msg==0x20A||m.Msg==0x100||m.Msg==0x197||m.Msg==0x180||m.Msg==0x184)){
   hiding=true;try{ShowScrollBar(Handle,1,false);}finally{hiding=false;}
   if(ViewportChanged!=null)ViewportChanged(this,EventArgs.Empty);
  }
 }
}
class ScrollRail:Control {
 readonly AccountGrid list;bool dragging;int grab;
 public ScrollRail(AccountGrid target){list=target;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=Color.FromArgb(17,20,22);Width=14;Cursor=Cursors.Hand;AccessibleName="账号列表滚动条";list.ViewportChanged+=(s,e)=>Invalidate();list.Resize+=(s,e)=>Invalidate();}
 int Rows{get{return list.VisibleRows;}}
 int Maximum{get{return list.MaximumTop;}}
 Rectangle Thumb {get{int height=Math.Min(Height,Math.Max(30,Height*Rows/Math.Max(1,list.TotalRows)));int top=Maximum==0?0:(int)((Height-height)*(double)list.TopRow/Maximum);return new Rectangle(4,top,6,height);}}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Maximum==0)return;var r=Thumb;using(var brush=new SolidBrush(dragging?Color.FromArgb(157,177,139):Color.FromArgb(76,86,83)))e.Graphics.FillRectangle(brush,r);}
 void SetTop(int y){var thumb=Thumb;int range=Height-thumb.Height;list.TopRow=range<=0?0:Math.Max(0,Math.Min(Maximum,(int)Math.Round((y-grab)*(double)Maximum/range)));Invalidate();}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(Maximum==0)return;var thumb=Thumb;grab=thumb.Contains(e.Location)?e.Y-thumb.Top:thumb.Height/2;dragging=true;Capture=true;SetTop(e.Y);}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging)SetTop(e.Y);}
 protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;Invalidate();}
 protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture){dragging=false;Invalidate();}}
 protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);list.TopRow=Math.Max(0,Math.Min(Maximum,list.TopRow-Math.Sign(e.Delta)*3));Invalidate();}
}
