using System;
using System.Drawing;
using System.Windows.Forms;
class AccountGrid:Control {
 readonly ListBox source;readonly DrawItemEventHandler draw;int topRow;
 public event EventHandler ViewportChanged;
 public int RowHeight {get{return 176;}}
 public int Columns{get{return Width>=1120?3:Width>=880?2:1;}}
 public int VisibleRows{get{return Math.Max(1,Height/RowHeight);}}
 public int TotalRows{get{return (source.Items.Count+Columns-1)/Columns;}}
 public int MaximumTop{get{return Math.Max(0,TotalRows-VisibleRows);}}
 public int TopRow{get{return topRow;}set{topRow=Math.Max(0,Math.Min(MaximumTop,value));Invalidate();if(ViewportChanged!=null)ViewportChanged(this,EventArgs.Empty);}}
 public AccountGrid(ListBox list,DrawItemEventHandler render){source=list;draw=render;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Selectable,true);BackColor=Color.FromArgb(17,20,22);TabStop=true;AccessibleName="账号卡片列表";source.SelectedIndexChanged+=(s,e)=>{EnsureSelection();AccessibilityNotifyClients(AccessibleEvents.Selection,source.SelectedIndex);};}
 public Rectangle CardBounds(int index){int width=ClientSize.Width/Columns;return new Rectangle((index%Columns)*width,(index/Columns-TopRow)*RowHeight,width,RowHeight);}
 public Rectangle MenuBounds(int index){var r=CardBounds(index);return new Rectangle(r.Right-45,r.Top+17,26,26);}
 public int IndexFromPoint(Point p){if(p.X<0||p.Y<0||p.X>=Width||p.Y>=Height)return -1;int index=(TopRow+p.Y/RowHeight)*Columns+Math.Min(Columns-1,p.X/Math.Max(1,Width/Columns));if(index>=source.Items.Count)return -1;var r=CardBounds(index);return new Rectangle(r.X+1,r.Y+3,r.Width-12,r.Height-12).Contains(p)?index:-1;}
 void EnsureSelection(){int index=source.SelectedIndex;if(index<0){if(source.Items.Count==0)TopRow=0;return;}int row=index/Columns;if(row<TopRow)TopRow=row;else if(row>=TopRow+VisibleRows)TopRow=row-VisibleRows+1;Invalidate();}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(topRow>MaximumTop)topRow=MaximumTop;for(int i=TopRow*Columns;i<Math.Min(source.Items.Count,(TopRow+VisibleRows+1)*Columns);i++){var state=i==source.SelectedIndex?DrawItemState.Selected:DrawItemState.None;if(Focused&&i==source.SelectedIndex)state|=DrawItemState.Focus;draw(this,new DrawItemEventArgs(e.Graphics,Font,CardBounds(i),i,state,ForeColor,BackColor));}}
 protected override void OnMouseDown(MouseEventArgs e){Focus();int index=IndexFromPoint(e.Location);if(index>=0)source.SelectedIndex=index;base.OnMouseDown(e);}
 protected override void OnMouseWheel(MouseEventArgs e){TopRow-=Math.Sign(e.Delta);base.OnMouseWheel(e);}
 protected override bool IsInputKey(Keys key){return key==Keys.Left||key==Keys.Right||key==Keys.Up||key==Keys.Down||key==Keys.Home||key==Keys.End||key==Keys.PageUp||key==Keys.PageDown||base.IsInputKey(key);}
 protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.Handled||source.Items.Count==0)return;int index=Math.Max(0,source.SelectedIndex);switch(e.KeyCode){case Keys.Left:index--;break;case Keys.Right:index++;break;case Keys.Up:index-=Columns;break;case Keys.Down:index+=Columns;break;case Keys.Home:index=0;break;case Keys.End:index=source.Items.Count-1;break;case Keys.PageUp:index-=VisibleRows*Columns;break;case Keys.PageDown:index+=VisibleRows*Columns;break;default:return;}source.SelectedIndex=Math.Max(0,Math.Min(source.Items.Count-1,index));e.SuppressKeyPress=true;}
 protected override void OnResize(EventArgs e){base.OnResize(e);TopRow=topRow;EnsureSelection();}
 protected override AccessibleObject CreateAccessibilityInstance(){return new GridAccess(this);}
 class GridAccess:ControlAccessibleObject {
  readonly AccountGrid grid;public GridAccess(AccountGrid g):base(g){grid=g;}
  public override AccessibleRole Role{get{return AccessibleRole.List;}}
  public override int GetChildCount(){return grid.source.Items.Count;}
  public override AccessibleObject GetChild(int index){return index>=0&&index<GetChildCount()?new CardAccess(grid,index):null;}
  public override AccessibleObject GetSelected(){return GetChild(grid.source.SelectedIndex);}
  public override AccessibleObject GetFocused(){return grid.Focused?GetSelected():null;}
 }
 class CardAccess:AccessibleObject {
  readonly AccountGrid grid;readonly int index;public CardAccess(AccountGrid g,int i){grid=g;index=i;}
  public override string Name{get{return index<grid.source.Items.Count?((Login)grid.source.Items[index]).ToString()+" "+((Login)grid.source.Items[index]).Note:"";}set{}}
  public override AccessibleRole Role{get{return AccessibleRole.ListItem;}}
  public override AccessibleObject Parent{get{return grid.AccessibilityObject;}}
  public override Rectangle Bounds{get{return grid.RectangleToScreen(grid.CardBounds(index));}}
  public override AccessibleStates State{get{return AccessibleStates.Selectable|AccessibleStates.Focusable|(grid.source.SelectedIndex==index?AccessibleStates.Selected:AccessibleStates.None)|(!grid.ClientRectangle.IntersectsWith(grid.CardBounds(index))?AccessibleStates.Offscreen:AccessibleStates.None);}}
  public override string DefaultAction{get{return "选择账号";}}
  public override void DoDefaultAction(){if(index<grid.source.Items.Count){grid.source.SelectedIndex=index;grid.Focus();}}
 }
}
