using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections.Generic;
partial class Window {
 static readonly Color Ink=Color.FromArgb(17,20,22), Surface=Color.FromArgb(26,30,32), Line=Color.FromArgb(61,67,68), Muted=Color.FromArgb(157,167,166), White=Color.FromArgb(242,245,240), Lime=Color.FromArgb(211,245,112);
 readonly Label selectedTitle=new Label(),selectedDetail=new Label();
 readonly TextBox search=new TextBox();
 readonly Label accountCount=new Label();
 readonly Label restart=new Label();
 AccountGrid grid;
 int hoveredIndex=-1;bool hoveredMenu;
 readonly ToolTip tips=new ToolTip{AutoPopDelay=7000,InitialDelay=400,ReshowDelay=150};
 List<Login> allAccounts=new List<Login>();
 string currentKey="";
 static GraphicsPath Round(Rectangle r,int radius) {var p=new GraphicsPath();int d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 static void TextAt(Graphics g,string text,Font f,Rectangle r,Color color,TextFormatFlags flags=TextFormatFlags.Left) {TextRenderer.DrawText(g,text,f,r,color,flags|TextFormatFlags.EndEllipsis|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine);}
 void BuildInterface() {
  Text="ChatGPT Codex Switch · v"+Application.ProductVersion+" · 账号管理"+(preview?" · 预览":"");Font=new Font("Microsoft YaHei UI",10);BackColor=Ink;ForeColor=White;
  ClientSize=new Size(1260,560);MinimumSize=new Size(900,560);StartPosition=FormStartPosition.CenterScreen;AutoScaleMode=AutoScaleMode.Dpi;
  layout.Dock=DockStyle.Fill;layout.Padding=new Padding(28,10,28,12);layout.ColumnCount=1;layout.RowCount=8;
  foreach(float height in new float[]{46,86,0,88,42,28,55,24})layout.RowStyles.Add(new RowStyle(height==0?SizeType.Percent:SizeType.Absolute,height==0?100:height));
  var brand=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty};
  brand.Paint+=(s,e)=>{using(var brush=new SolidBrush(Lime))using(var path=Round(new Rectangle(0,8,30,30),6))e.Graphics.FillPath(brush,path);using(var font=new Font("Segoe UI",15,FontStyle.Bold))TextAt(e.Graphics,"C",font,new Rectangle(3,7,26,31),Ink);using(var font=new Font("Segoe UI",14,FontStyle.Bold))TextAt(e.Graphics,"Codex Switch",font,new Rectangle(44,4,200,40),White);using(var pen=new Pen(Line))e.Graphics.DrawLine(pen,0,brand.Height-1,brand.Width,brand.Height-1);};
  var heading=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty};
  heading.Paint+=(s,e)=>{using(var font=new Font(Font.FontFamily,22,FontStyle.Bold))TextAt(e.Graphics,"工作区",font,new Rectangle(0,10,170,42),White);};
  accountCount.ForeColor=Muted;accountCount.SetBounds(0,54,190,22);heading.Controls.Add(accountCount);
  var searchBox=new Panel{BackColor=Surface,Height=40};
  searchBox.Paint+=(s,e)=>{e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var pen=new Pen(search.ContainsFocus?Lime:Line))using(var path=Round(new Rectangle(0,0,searchBox.Width-1,searchBox.Height-1),8))e.Graphics.DrawPath(pen,path);};search.Enter+=(s,e)=>searchBox.Invalidate();search.Leave+=(s,e)=>searchBox.Invalidate();
  search.BorderStyle=BorderStyle.None;search.BackColor=Surface;search.ForeColor=White;search.AccessibleName="搜索邮箱或备注";
  var hint=new Label{Name="searchHint",Text="搜索账号",ForeColor=Muted,BackColor=Surface,AutoSize=false,TextAlign=ContentAlignment.MiddleLeft};hint.Click+=(s,e)=>search.Focus();
  var clear=new DarkButton{Text="×",Width=32,Height=30,TabStop=false};clear.Click+=(s,e)=>{search.Clear();search.Focus();};
  searchBox.Controls.AddRange(new Control[]{search,hint,clear});
  hint.BringToFront();clear.BringToFront();
  searchBox.Resize+=(s,e)=>{search.SetBounds(92,9,searchBox.Width-134,24);hint.SetBounds(12,8,72,24);clear.Location=new Point(searchBox.Width-36,4);};
  search.HandleCreated+=(s,e)=>{try{SetSearchCue(search.Handle,0x1501,new IntPtr(1),"邮箱、备注或套餐");}catch{}};
  clear.Visible=false;clear.AccessibleName="清空搜索";search.TextChanged+=(s,e)=>{clear.Visible=search.TextLength>0;if(clear.Visible)clear.BringToFront();ApplyFilter();};search.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape){search.Clear();e.SuppressKeyPress=true;}};
  KeyPreview=true;KeyDown+=(s,e)=>{if(e.Control&&e.KeyCode==Keys.F){search.Focus();search.SelectAll();e.SuppressKeyPress=true;}};
  heading.Controls.Add(searchBox);
  SetButton(add,"＋  添加账号",140);SetButton(refresh,"↻  刷新额度",140);heading.Controls.Add(add);heading.Controls.Add(refresh);
  heading.Resize+=(s,e)=>{add.Location=new Point(heading.Width-314,20);refresh.Location=new Point(heading.Width-154,20);searchBox.SetBounds(205,20,Math.Max(220,heading.Width-540),40);};
  accounts.Dock=DockStyle.Fill;accounts.BorderStyle=BorderStyle.None;accounts.BackColor=Ink;accounts.ForeColor=White;accounts.DrawMode=DrawMode.OwnerDrawFixed;accounts.ItemHeight=94;accounts.IntegralHeight=false;accounts.AccessibleName="账号和工作区列表";accounts.DrawItem+=DrawAccount;
  var listHost=new Panel{Dock=DockStyle.Fill,BackColor=Ink,Margin=Padding.Empty};grid=new AccountGrid(accounts,DrawAccount){Dock=DockStyle.Fill};var rail=new ScrollRail(grid){Dock=DockStyle.Right};accounts.Visible=false;listHost.Controls.Add(accounts);listHost.Controls.Add(grid);listHost.Controls.Add(rail);
  accounts.SelectedIndexChanged+=(s,e)=>UpdateSelection();accounts.Resize+=(s,e)=>accounts.Invalidate();if(grid!=null)grid.Invalidate();
  var rowMenu=new ContextMenuStrip{BackColor=Surface,ForeColor=White,Renderer=new ToolStripProfessionalRenderer(new MenuColors())};rowMenu.Items.Add("修改账号备注",null,(s,e)=>note.PerformClick());
  StyleMenu(rowMenu);
  grid.MouseMove+=(s,e)=>{int i=grid.IndexFromPoint(e.Location);bool menuHover=i>=0&&grid.MenuBounds(i).Contains(e.Location);if(i!=hoveredIndex||menuHover!=hoveredMenu){hoveredIndex=i;hoveredMenu=menuHover;accounts.Invalidate();if(grid!=null)grid.Invalidate();}grid.Cursor=menuHover?Cursors.Hand:Cursors.Default;};grid.MouseLeave+=(s,e)=>{hoveredIndex=-1;hoveredMenu=false;accounts.Invalidate();if(grid!=null)grid.Invalidate();};
  grid.MouseUp+=(s,e)=>{int index=grid.IndexFromPoint(e.Location);if(index<0)return;if(e.Button!=MouseButtons.Right&&!grid.MenuBounds(index).Contains(e.Location))return;if(index>=0){accounts.SelectedIndex=index;rowMenu.Items[0].Enabled=note.Enabled;rowMenu.Show(grid,e.Location,ToolStripDropDownDirection.BelowLeft);}};
  grid.KeyDown+=(s,e)=>{if(accounts.SelectedIndex>=0&&(e.KeyCode==Keys.Apps||(e.Shift&&e.KeyCode==Keys.F10))){rowMenu.Items[0].Enabled=note.Enabled;rowMenu.Show(grid,new Point(grid.CardBounds(accounts.SelectedIndex).Right-20,grid.CardBounds(accounts.SelectedIndex).Bottom),ToolStripDropDownDirection.BelowLeft);e.SuppressKeyPress=true;}};
  var dock=new Panel{Dock=DockStyle.Fill,Padding=new Padding(22),Margin=new Padding(0,6,0,6)};
  dock.Paint+=(s,e)=>{e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var path=Round(new Rectangle(0,5,dock.Width-2,dock.Height-10),12))using(var brush=new SolidBrush(Surface))using(var pen=new Pen(Line)){e.Graphics.FillPath(brush,path);e.Graphics.DrawPath(pen,path);}};
  selectedTitle.Font=new Font(Font.FontFamily,14,FontStyle.Bold);selectedTitle.ForeColor=White;selectedTitle.AutoEllipsis=true;selectedTitle.BackColor=Surface;
  selectedDetail.ForeColor=Muted;selectedDetail.AutoEllipsis=true;selectedDetail.BackColor=Surface;
  SetButton(change,"⇄  切换至此账号",270);((DarkButton)change).Primary=true;
  restart.Text="切换将重新启动 Codex";restart.ForeColor=Muted;restart.BackColor=Surface;restart.TextAlign=ContentAlignment.MiddleCenter;
  dock.Controls.AddRange(new Control[]{selectedTitle,selectedDetail,change,restart});
  dock.Resize+=(s,e)=>{selectedTitle.SetBounds(20,12,Math.Max(100,dock.Width-350),26);selectedDetail.SetBounds(20,42,Math.Max(100,dock.Width-350),22);change.SetBounds(dock.Width-290,9,270,36);restart.SetBounds(dock.Width-290,48,270,19);};
  var recovery=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Margin=Padding.Empty,Padding=new Padding(0,3,0,3)};SetButton(confirm,"确认切换成功",155);SetButton(restore,"恢复切换前账号",185);confirm.Height=restore.Height=32;confirm.Margin=restore.Margin=new Padding(0,0,10,0);recovery.Controls.AddRange(new Control[]{confirm,restore});
  status.Dock=DockStyle.Fill;status.Margin=Padding.Empty;status.ForeColor=Muted;status.TextAlign=ContentAlignment.MiddleLeft;status.AutoEllipsis=true;
  log.Dock=DockStyle.Fill;log.Multiline=true;log.ReadOnly=true;log.ScrollBars=ScrollBars.Vertical;log.BackColor=Surface;log.ForeColor=Muted;log.BorderStyle=BorderStyle.None;log.AccessibleName="登录进度";
  var foot=new Label{Text="◈  凭据仅保存在本机",ForeColor=Muted,Dock=DockStyle.Fill,Margin=Padding.Empty,TextAlign=ContentAlignment.BottomLeft};
  layout.Controls.Add(brand,0,0);layout.Controls.Add(heading,0,1);layout.Controls.Add(listHost,0,2);layout.Controls.Add(dock,0,3);layout.Controls.Add(recovery,0,4);layout.Controls.Add(status,0,5);layout.Controls.Add(log,0,6);layout.Controls.Add(foot,0,7);Controls.Add(layout);
  tips.SetToolTip(add,"通过官方登录添加账号，当前账号保持登录。");tips.SetToolTip(refresh,"刷新所有已保存账号的额度，包括搜索结果以外的账号。");tips.SetToolTip(clear,"清空搜索（Esc）");tips.SetToolTip(search,"按邮箱、备注或套餐搜索（Ctrl+F）");tips.SetToolTip(confirm,"在 Codex 中核实账号后，清除本次恢复备份。");tips.SetToolTip(restore,"结束 Codex 并恢复到上次切换前的账号。");
  HandleCreated+=(s,e)=>{try{int dark=1;DwmSetWindowAttribute(Handle,20,ref dark,4);}catch{}};
  FormClosed+=(s,e)=>{tips.Dispose();rowMenu.Dispose();};
  UpdateSelection();
 }
 [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr hwnd,int attr,ref int value,int size);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SetSearchCue(IntPtr hwnd,int message,IntPtr showWhenFocused,string text);
 void CaptureAccounts(){allAccounts=accounts.Items.Cast<Login>().ToList();ApplyFilter();}
 void ApplyFilter(){
  var selected=accounts.SelectedItem as Login;string key=selected==null?null:selected.Key;
  accounts.BeginUpdate();try{accounts.Items.Clear();foreach(var login in allAccounts.Where(a=>a.MatchesQuery(search.Text)))accounts.Items.Add(login);int index=-1;for(int i=0;i<accounts.Items.Count;i++)if(((Login)accounts.Items[i]).Key==key)index=i;accounts.SelectedIndex=index>=0?index:accounts.Items.Count>0?0:-1;}finally{accounts.EndUpdate();}
  accountCount.Text=search.TextLength==0?allAccounts.Count+" 个工作区":accounts.Items.Count+" / "+allAccounts.Count+" 个匹配";UpdateSelection();
 }
 void UpdateSelection() {
  var login=accounts.SelectedItem as Login;
  bool live=login!=null&&!String.IsNullOrEmpty(currentKey)&&login.Key==currentKey;
  change.Enabled=!busy&&login!=null&&!live;
  change.Text=busy?"请稍候…":live?"正在使用":login==null?"请选择账号":"⇄  切换至此账号";
  restart.Text=busy?"当前操作完成后即可继续":live?"此账号正在使用，无需切换":"切换将重新启动 Codex";
  selectedTitle.Text=login==null?(search.TextLength>0?"没有匹配的账号":"选择一个账号"):(live?"正在使用： ":"将切换至： ")+login.Email;
  selectedDetail.Text=login==null?(search.TextLength>0?"试试其他邮箱、备注或套餐，按 Esc 清空搜索。":"添加账号后，在列表中选择工作区。"):WorkspaceName(login)+"  ·  "+login.PlanLabel;
  accounts.Invalidate();if(grid!=null)grid.Invalidate();
 }
 static string WorkspaceName(Login login) {if(!String.IsNullOrWhiteSpace(login.Note))return login.Note;bool personal=login.PlanLabel.StartsWith("个人");string id=login.AccountId??"";return personal?"个人工作区":"团队工作区 · "+id.Substring(0,Math.Min(8,id.Length));}
 static void SetButton(Button b,string text,int width) {b.Text=text;b.Width=width;b.Height=40;b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=0;b.Margin=new Padding(0,4,12,4);b.BackColor=Surface;b.ForeColor=White;b.Cursor=Cursors.Hand;}
 void DrawAccount(object sender,DrawItemEventArgs e){if(e.Index>=0)DrawCard(e,(Login)accounts.Items[e.Index]);}
 void DrawQuota(Graphics g,QuotaDisplay q,string raw,Rectangle r) {
  if(q==null){TextAt(g,"其他额度窗口",Font,new Rectangle(r.X,r.Y,r.Width,24),Muted);string text="服务未提供";TextAt(g,text,Font,new Rectangle(r.X,r.Y+35,r.Width,28),Muted);return;}
  TextAt(g,q.Label.Replace("额度","")+"剩余",Font,new Rectangle(r.X,r.Y,r.Width-49,24),Muted);
  using(var bold=new Font(Font,FontStyle.Bold))TextAt(g,q.Remaining.ToString("0.#")+"%",bold,new Rectangle(r.Right-52,r.Y,52,24),White,TextFormatFlags.Right);
  var bar=new Rectangle(r.X,r.Y+27,r.Width,6);using(var path=Round(bar,3))using(var brush=new SolidBrush(Color.FromArgb(67,73,74)))g.FillPath(brush,path);
  int width=(int)(bar.Width*q.Remaining/100);if(width>0)using(var brush=new SolidBrush(q.Remaining<15?Color.FromArgb(238,177,107):Lime))g.FillRectangle(brush,bar.X,bar.Y,width,bar.Height);
  TextAt(g,q.Reset,Font,new Rectangle(r.X,r.Y+39,r.Width,22),Muted);
 }
 class DarkButton:Button {
  public bool Primary;bool hover,pressed;
  public DarkButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
  protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
  protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
  protected override void OnMouseDown(MouseEventArgs e){pressed=true;Invalidate();base.OnMouseDown(e);}
  protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
  protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter){pressed=true;Invalidate();}base.OnKeyDown(e);}
  protected override void OnKeyUp(KeyEventArgs e){pressed=false;Invalidate();base.OnKeyUp(e);}
  protected override void OnEnabledChanged(EventArgs e){Cursor=Enabled?Cursors.Hand:Cursors.Default;pressed=false;base.OnEnabledChanged(e);Invalidate();}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Parent==null?Ink:Parent.BackColor);var rect=new Rectangle(1,1,Width-3,Height-3);Color bg=Primary?(Enabled?(pressed?Color.FromArgb(180,215,85):hover?Color.FromArgb(225,255,149):Lime):Surface):(pressed?Color.FromArgb(51,61,49):hover?Color.FromArgb(42,48,49):Surface);using(var path=Round(rect,8))using(var brush=new SolidBrush(bg))using(var pen=new Pen(Primary&&Enabled?bg:Line)){g.FillPath(brush,path);g.DrawPath(pen,path);}using(var font=new Font(Font,Primary?FontStyle.Bold:FontStyle.Regular))TextAt(g,Text,font,rect,Enabled?(Primary?Ink:White):Muted,TextFormatFlags.HorizontalCenter);if(Focused&&ShowFocusCues)using(var focus=new Pen(Primary&&Enabled?Ink:Lime))using(var ring=Round(Rectangle.Inflate(rect,-4,-4),5))g.DrawPath(focus,ring);}
 }
}
