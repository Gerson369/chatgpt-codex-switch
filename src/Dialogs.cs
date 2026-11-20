using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
partial class Window {
 void StyleMenu(ContextMenuStrip menu){menu.Font=new Font(Font.FontFamily,10);menu.BackColor=Surface;menu.ForeColor=White;menu.Renderer=new ToolStripProfessionalRenderer(new MenuColors());menu.ShowCheckMargin=false;menu.ShowImageMargin=false;menu.Padding=new Padding(5);foreach(ToolStripItem item in menu.Items){item.AutoSize=false;item.Size=new Size(220,34);item.Padding=new Padding(8,0,8,0);}}
 Form CreateNoteDialog(Login login) {
  var dialog=NewDialog("修改账号备注",new Size(500,250));
  var label=new Label{Text=login.Email+"  ·  "+login.PlanLabel,ForeColor=White,Left=24,Top=22,Width=450,Height=30,AutoEllipsis=true};
  var input=new TextBox{Name="noteInput",Text=login.Note,MaxLength=80,BackColor=Surface,ForeColor=White,BorderStyle=BorderStyle.FixedSingle,Left=24,Top=75,Width=450};
  var help=new Label{Text="备注只用于本机识别，例如“公司团队”。留空可恢复默认名称。",ForeColor=Muted,Left=24,Top=118,Width=450,Height=40};
  var ok=new DarkButton{Text="保存备注",Primary=true,Left=344,Top=188,Width=130,Height=38,DialogResult=DialogResult.OK};
  var cancel=new DarkButton{Text="取消",Left=230,Top=188,Width=100,Height=38,DialogResult=DialogResult.Cancel};
  dialog.Controls.AddRange(new Control[]{label,input,help,cancel,ok});dialog.AcceptButton=ok;dialog.CancelButton=cancel;
  dialog.Shown+=(s,e)=>{input.Focus();input.SelectAll();};return dialog;
 }
 Task EditNote() {
  var login=accounts.SelectedItem as Login;if(login==null)return Task.FromResult(0);
  using(var dialog=CreateNoteDialog(login))if(dialog.ShowDialog(this)==DialogResult.OK){string value=((TextBox)dialog.Controls["noteInput"]).Text.Trim();vault.SetNote(login.Key,value);login.Note=value;ApplyFilter();status.Text="账号备注已保存。";}
  return Task.FromResult(0);
 }
 Form CreateLoginDialog() {
  var dialog=NewDialog("添加账号",new Size(500,270));
  var title=new Label{Text="选择官方登录方式",Font=new Font(Font.FontFamily,16,System.Drawing.FontStyle.Bold),ForeColor=White,Left=24,Top=24,Width=450,Height=36};
  var help=new Label{Text="推荐使用浏览器登录。\r\n设备码登录需要账号或组织允许该功能。",ForeColor=Muted,Left=24,Top=76,Width=450,Height=58};
  var browser=new DarkButton{Text="浏览器登录",Primary=true,Left=24,Top=156,Width=215,Height=44,DialogResult=DialogResult.Yes};
  var device=new DarkButton{Text="设备码登录",Left=254,Top=156,Width=220,Height=44,DialogResult=DialogResult.No};
  var cancel=new DarkButton{Text="取消",Left=374,Top=218,Width=100,Height=32,DialogResult=DialogResult.Cancel};
  dialog.Controls.AddRange(new Control[]{title,help,browser,device,cancel});dialog.AcceptButton=browser;dialog.CancelButton=cancel;return dialog;
 }
 DialogResult ChooseLoginMethod(){using(var dialog=CreateLoginDialog())return dialog.ShowDialog(this);}
 Form NewDialog(string title,Size size){var dialog=new Form{Text=title,Font=Font,BackColor=Ink,ForeColor=White,ClientSize=size,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,ShowInTaskbar=false};dialog.HandleCreated+=(s,e)=>{try{int dark=1;DwmSetWindowAttribute(dialog.Handle,20,ref dark,4);}catch{}};return dialog;}
 class MenuColors:ProfessionalColorTable {
  public override Color ToolStripDropDownBackground{get{return Surface;}}
  public override Color ImageMarginGradientBegin{get{return Surface;}}
  public override Color ImageMarginGradientMiddle{get{return Surface;}}
  public override Color ImageMarginGradientEnd{get{return Surface;}}
  public override Color MenuBorder{get{return Line;}}
  public override Color MenuItemSelected{get{return Color.FromArgb(49,59,47);}}
  public override Color MenuItemBorder{get{return Line;}}
 }
}
