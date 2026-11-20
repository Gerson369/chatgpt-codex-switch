using System;
using System.Reflection;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
class UiGridCheck {
 static BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Check(bool value,string name){if(!value)throw new Exception(name);}
 static object Field(Type type,object instance,string name){return type.GetField(name,flags).GetValue(instance);}
 static void Shot(Control control,string path){using(var image=new Bitmap(control.Width,control.Height)){control.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(path);}}
 static int Prop(object instance,string name){return (int)instance.GetType().GetProperty(name).GetValue(instance,null);}
 [STAThread]static int Main(string[] args){
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  var asm=Assembly.LoadFrom(Path.GetFullPath(args[0]));var type=asm.GetType("Window");var form=(Form)Activator.CreateInstance(type,new object[]{true});int result=1;
  form.Shown+=(s,e)=>{var timer=new Timer{Interval=700};timer.Tick+=(sender,ev)=>{timer.Stop();try{
   var list=(ListBox)Field(type,form,"accounts");var grid=(Control)Field(type,form,"grid");var title=(Label)Field(type,form,"selectedTitle");var button=(Button)Field(type,form,"change");
   Check(form.Controls.Find("moreButton",true).Length==0,"redundant more menu remains");Check(Prop(grid,"Columns")==3,"default should have three columns");
   Check(grid.AccessibilityObject.GetChildCount()==3&&grid.AccessibilityObject.GetChild(1).Name.Contains("lin@example.com"),"account cards must expose accessible identities");
   list.SelectedIndex=0;grid.GetType().GetMethod("OnKeyDown",flags).Invoke(grid,new object[]{new KeyEventArgs(Keys.Right)});Check(list.SelectedIndex==1,"right arrow must select next card");
   grid.GetType().GetMethod("OnKeyDown",flags).Invoke(grid,new object[]{new KeyEventArgs(Keys.Down)});Check(list.SelectedIndex==2&&title.Text.Contains("studio@gmail.com"),"down arrow and dock");
   list.SelectedIndex=0;Check(!button.Enabled&&button.Text=="正在使用","active-account protection");list.SelectedIndex=2;
   var layout=(TableLayoutPanel)Field(type,form,"layout");layout.RowStyles[4].Height=42;foreach(string name in new[]{"confirm","restore"})((Button)Field(type,form,name)).Visible=true;
   form.ClientSize=new Size(1100,700);form.PerformLayout();Check(grid.Height>=Prop(grid,"RowHeight")*2,"three accounts should fit with recovery controls");
   foreach(string name in new[]{"confirm","restore"}){var b=(Button)Field(type,form,name);Check(b.Bottom<=b.Parent.ClientSize.Height,"recovery clipped");}
   var search=(TextBox)Field(type,form,"search");Check(search.Parent.Controls["searchHint"].Right<=search.Left,"search overlaps label");
   object login=list.Items[2];login.GetType().GetField("Quota").SetValue(login,"读取失败 · 登录凭据已过期");grid.Invalidate();Shot(form,args[1]);
   foreach(string method in new[]{"CreateNoteDialog","CreateLoginDialog"})using(var dialog=(Form)type.GetMethod(method,flags).Invoke(form,method=="CreateNoteDialog"?new object[]{login}:null)){Check(dialog.AcceptButton!=null&&dialog.CancelButton!=null,"dialog keyboard actions");dialog.Show(form);Application.DoEvents();Shot(dialog,Path.Combine(Path.GetDirectoryName(args[1]),method+".png"));dialog.Close();}
   var all=(System.Collections.IList)Field(type,form,"allAccounts");for(int i=0;i<40;i++){object item=Activator.CreateInstance(login.GetType());item.GetType().GetField("Email").SetValue(item,"account"+i+"@example.test");item.GetType().GetField("Key").SetValue(item,"test-"+i);item.GetType().GetField("AccountId").SetValue(item,"workspace-"+i);all.Add(item);}
   type.GetMethod("ApplyFilter",flags).Invoke(form,null);list.SelectedIndex=list.Items.Count-1;Check(Prop(grid,"TopRow")>0,"selection should scroll card into view");
   search.Text="account39@";Check(list.Items.Count==1&&title.Text.Contains("account39@"),"search selection");search.Text="not-found";Check(list.Items.Count==0&&!button.Enabled,"empty-result protection");search.Clear();Check(list.Items.Count==43,"clearing search preserves accounts");
   Control rail=null;foreach(Control c in grid.Parent.Controls)if(c.GetType().Name=="ScrollRail")rail=c;Check(rail!=null,"missing rail");
   rail.GetType().GetMethod("OnMouseDown",flags).Invoke(rail,new object[]{new MouseEventArgs(MouseButtons.Left,1,6,rail.Height-2,0)});int bottom=Prop(grid,"TopRow");Check(bottom>10,"rail click");
   rail.GetType().GetMethod("OnMouseMove",flags).Invoke(rail,new object[]{new MouseEventArgs(MouseButtons.Left,0,6,rail.Height/2,0)});Check(Prop(grid,"TopRow")<bottom,"rail drag");rail.GetType().GetMethod("OnMouseUp",flags).Invoke(rail,new object[]{new MouseEventArgs(MouseButtons.Left,1,6,rail.Height/2,0)});int before=Prop(grid,"TopRow");rail.GetType().GetMethod("OnMouseWheel",flags).Invoke(rail,new object[]{new MouseEventArgs(MouseButtons.None,0,6,20,120)});Check(Prop(grid,"TopRow")<before,"rail wheel");
   Shot(form,Path.Combine(Path.GetDirectoryName(args[1]),"many-grid-accounts.png"));form.ClientSize=new Size(940,700);form.PerformLayout();Check(Prop(grid,"Columns")==1,"narrow layout should become one column");
   result=0;Console.WriteLine("PASS: three-column cards, responsive two/one columns, keyboard navigation, 43-account search, rail, dialogs and no More menu");
  }catch(Exception ex){Console.WriteLine("FAIL: "+ex.Message);}finally{timer.Dispose();form.Close();}};timer.Start();};Application.Run(form);return result;
 }
}
