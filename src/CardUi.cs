using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
partial class Window {
 void DrawCard(DrawItemEventArgs e,Login login){
  var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
  bool selected=(e.State&DrawItemState.Selected)!=0,live=login.Key==currentKey&&!String.IsNullOrEmpty(currentKey);
  var card=new Rectangle(e.Bounds.Left+1,e.Bounds.Top+3,e.Bounds.Width-12,e.Bounds.Height-12);
  using(var path=Round(card,11))using(var fill=new SolidBrush(selected?Color.FromArgb(35,40,31):e.Index==hoveredIndex?Color.FromArgb(32,37,38):Surface))using(var pen=new Pen(selected?Lime:Line,selected?1.5f:1)){g.FillPath(fill,path);g.DrawPath(pen,path);}
  int x=card.Left+18,y=card.Top+13;
  using(var pen=new Pen(live||selected?Lime:Muted,2))g.DrawEllipse(pen,x,y+5,14,14);
  if(live||selected)using(var fill=new SolidBrush(Lime))g.FillEllipse(fill,x+4,y+9,6,6);
  using(var bold=new Font(Font.FontFamily,12,FontStyle.Bold))TextAt(g,login.Email,bold,new Rectangle(x+25,y,card.Width-155,26),White);
  if(live||selected)TextAt(g,live?"当前使用":"已选择",Font,new Rectangle(card.Right-110,y,72,26),Lime,TextFormatFlags.Right);
  var menuBox=new Rectangle(card.Right-34,y+1,26,26);
  if(e.Index==hoveredIndex&&hoveredMenu)using(var path=Round(menuBox,5))using(var fill=new SolidBrush(Color.FromArgb(55,63,55)))g.FillPath(fill,path);
  TextAt(g,"···",Font,menuBox,Muted,TextFormatFlags.HorizontalCenter);
  TextAt(g,WorkspaceName(login)+"  ·  "+login.PlanLabel,Font,new Rectangle(x,y+31,card.Width-36,24),Muted);
  using(var pen=new Pen(Line))g.DrawLine(pen,x,card.Top+75,card.Right-18,card.Top+75);
  var windows=QuotaDisplay.Parse(login.Quota);int width=(card.Width-50)/2;
  if(windows.Count==0){string text=login.Quota.Contains("过期")?"登录已过期":login.Quota=="读取中…"?"正在读取额度…":login.Quota.StartsWith("读取失败")?"额度读取失败":"此工作区未返回额度";string help=login.Quota.Contains("过期")?"请重新添加该账号":login.Quota=="读取中…"?"请稍候":"可点击“刷新额度”重试";TextAt(g,text,Font,new Rectangle(x,card.Top+88,card.Width-36,25),Muted);TextAt(g,help,Font,new Rectangle(x,card.Top+120,card.Width-36,24),Muted);}
  else for(int i=0;i<2;i++)DrawQuota(g,windows.Count>i?windows[i]:null,login.Quota,new Rectangle(x+i*(width+14),card.Top+85,width,70));
  if((e.State&DrawItemState.Focus)!=0)using(var pen=new Pen(Color.FromArgb(125,151,91)))using(var path=Round(Rectangle.Inflate(card,-4,-4),8))g.DrawPath(pen,path);
 }
}
