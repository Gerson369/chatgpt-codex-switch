using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Xml;

partial class Window : Form {
 readonly bool preview;
 readonly string home,auth,dataRoot;
 Vault vault;
 readonly CompactList accounts=new CompactList();
 readonly Label active=new Label(),status=new Label();
 readonly Button add=new DarkButton(),change=new DarkButton(),confirm=new DarkButton(),restore=new DarkButton(),refresh=new DarkButton(),note=new DarkButton();
 readonly TextBox log=new TextBox();
 readonly TableLayoutPanel layout=new TableLayoutPanel();
 bool busy;
 public Window(bool demo) {
  preview=demo;
  home=Environment.GetEnvironmentVariable("CODEX_HOME");
  if(String.IsNullOrWhiteSpace(home))home=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".codex");
  home=Path.GetFullPath(home); auth=Path.Combine(home,"auth.json");
  dataRoot=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CodexLocalSwitcher");
  BuildInterface();
  change.Click+=async (s,e)=>await Run(SwitchSelected);
  refresh.Click+=async(s,e)=>await Run(RefreshQuota);
  note.Click+=async(s,e)=>await Run(EditNote);
  add.Click+=async(s,e)=>await Run(AddAccount);
  confirm.Click+=async(s,e)=>await Run(()=>{
   if(MessageBox.Show(this,"已在 Codex 中确认显示目标账号，并且能够正常使用？","确认账号",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes) {
    vault.Commit(); Reload(); status.Text="已确认切换完成。";
   } return Task.FromResult(0);
  });
  restore.Click+=async(s,e)=>await Run(async()=>{
   var app=await FindDesktop(); await ExitCodex(); vault.Restore(auth); await Launch(app); Reload(); status.Text="已恢复切换前账号并重新打开 Codex。";
  });
  FormClosing+=(s,e)=>{if(busy){e.Cancel=true;MessageBox.Show(this,"请等待当前操作结束。登录窗口尚未完成时，可在登录窗口按 Ctrl+C 取消。");}};
  Shown+=async(s,e)=>{
   try {
    if(preview) {
     active.Text="当前账号：personal@example.com\r\n聊天和项目目录：原 Codex 目录";
     accounts.Items.Add(new Login{Key="demo-team",Email="lin@example.com",Plan="business",AccountId="workspace-team",Note="星河工作室",Quota="5 小时剩余 72% · 今天 20:00 重置 | 每周剩余 46% · 周一 00:00 重置"});accounts.Items.Add(new Login{Key="demo-personal",Email="lin@example.com",Plan="plus",AccountId="workspace-personal",Note="个人工作区",Quota="5 小时剩余 100% · 今天 20:00 重置 | 每周剩余 98% · 周一 00:00 重置"});accounts.Items.Add(new Login{Key="demo-enterprise",Email="studio@gmail.com",Plan="enterprise",AccountId="workspace-enterprise",Note="设计团队",Quota="5 小时剩余 86% · 今天 20:00 重置 | 每周剩余 64% · 周一 00:00 重置"}); accounts.SelectedIndex=1;
     status.Text="选择账号后切换；运行中的任务需要先结束。";
     confirm.Visible=restore.Visible=false;layout.RowStyles[4].Height=0;log.Visible=false;layout.RowStyles[6].Height=0;
     currentKey="demo-team";UpdateSelection();
     CaptureAccounts();
    } else {vault=new Vault(Path.Combine(dataRoot,"accounts"));try{vault.Save(ReadActive());}catch{}Reload();if(accounts.Items.Count>0)await Run(RefreshQuota);}
   }catch(Exception ex){ShowError(ex);}
  };
 }
 async Task RefreshQuota() {
  refresh.Text="正在读取…";
  string cli=FindCli();var entries=allAccounts.ToList();
  foreach(var login in entries) {
   status.Text="正在读取 "+login.Email+" · "+login.PlanLabel;login.Quota="读取中…";accounts.Invalidate();
   try {byte[] bytes=vault.Load(login.Key);try{byte[] current=ReadActive();if(Login.Parse(current).Key==login.Key)bytes=current;}catch{}login.Quota=await QuotaReader.Read(cli,dataRoot,home,bytes,plan=>{login.Plan=plan;vault.SetPlan(login.Key,plan);});}
   catch(Exception ex){login.Quota="读取失败 · "+(ex.Message.Contains("过期")?"登录凭据已过期，请重新添加该账号":ex.Message.Contains("超时")?"查询超时，请稍后重试":"请刷新额度重试");}
   UpdateSelection();
  }
  status.Text="额度读取结束；显示的是本次查询结果。登录过期的账号请重新添加。";
 }
 void ShowError(Exception ex) {status.ForeColor=Color.FromArgb(242,170,128);status.Text=ex.Message;MessageBox.Show(this,ex.Message,"操作未完成",MessageBoxButtons.OK,MessageBoxIcon.Information);}
 async Task Run(Func<Task> action) {
  if(busy||preview||vault==null)return;
  busy=true; foreach(var b in new[]{add,change,confirm,restore,refresh,note})b.Enabled=false;
  UpdateSelection();
  status.ForeColor=Muted;
  try {await action();} catch(Exception ex){ShowError(ex);} finally {busy=false;refresh.Text="↻  刷新额度";add.Text="＋  添加账号";foreach(var b in new[]{add,change,refresh,note})b.Enabled=true;confirm.Enabled=restore.Enabled=vault.HasRecovery;UpdateSelection();}
 }
 void CheckStore() {
  string cfg=Path.Combine(home,"config.toml");
  if(File.Exists(cfg) && Regex.IsMatch(File.ReadAllText(cfg),"(?m)^\\s*cli_auth_credentials_store\\s*=\\s*[\"'](?:keyring|auto|ephemeral)[\"']"))
   throw new Exception("当前配置使用系统凭据库或临时登录。本程序只支持 auth.json 文件登录，不会修改你的认证配置。");
 }
 byte[] ReadActive() {CheckStore(); byte[] bytes=File.ReadAllBytes(auth);Login.Parse(bytes);return bytes;}
 void Reload() {
  currentKey=""; accounts.Items.Clear(); foreach(var login in vault.List())accounts.Items.Add(login);
  string current="未读取到文件登录";
  try {
   var live=Login.Parse(ReadActive()); currentKey=live.Key; current=live.Email+" · "+live.PlanLabel;
   for(int i=0;i<accounts.Items.Count;i++)if(((Login)accounts.Items[i]).Key==live.Key)accounts.SelectedIndex=i;
  } catch { }
  active.Text="当前账号："+current;
  if(accounts.SelectedIndex<0&&accounts.Items.Count>0)accounts.SelectedIndex=0;
  CaptureAccounts();
  confirm.Enabled=restore.Enabled=vault.HasRecovery;
  confirm.Visible=restore.Visible=vault.HasRecovery;
  layout.RowStyles[4].Height=vault.HasRecovery?42:0;
  log.Visible=!String.IsNullOrEmpty(log.Text);layout.RowStyles[6].Height=log.Visible?55:0;
  status.Text=vault.HasRecovery?"上次切换待确认。请在 Codex 中核实账号，或恢复切换前的登录。":allAccounts.Count==0?"点击添加账号，完成官方登录。":"选择账号卡片，点击底部按钮切换。";
 }
 async Task SwitchSelected() {
  var target=accounts.SelectedItem as Login;if(target==null)throw new Exception("请先保存或添加一个账号。");
  if(vault.HasRecovery)throw new Exception("请先确认上次切换成功，或恢复切换前的账号。");
  CheckStore(); byte[] bytes=vault.Load(target.Key);
  if(Login.Parse(ReadActive()).Key==target.Key)throw new Exception("当前已经是这个账号，无需重启。");
  if(MessageBox.Show(this,"即将切换到："+target+"\r\n\r\n程序将自动结束当前 Codex 及其后台进程，然后重新打开。运行中的任务会中断，请先保存工作。","切换并重启",MessageBoxButtons.OKCancel,MessageBoxIcon.Warning)!=DialogResult.OK)return;
  var app=await FindDesktop(); await ExitCodex();
  vault.Activate(bytes,auth);
  try {await Launch(app);}catch {Reload();throw new Exception("账号已写入，但未能确认 Codex 启动。可手动打开 Codex，或点击恢复切换前账号。");}
  Reload(); status.Text="已重新打开 Codex。请核实实际账号，再点击“确认切换成功”。";
 }
 class Desktop {public string Exe,AppId;}
 static string ProcessPath(Process p) {
  try{return p.MainModule.FileName;}catch{try{if(p.HasExited)return "";}catch{}throw new Exception("无法确认一个 Codex/ChatGPT 进程的身份。请退出 Codex 和命令行后重试。");}
 }
 static bool IsDesktop(string path) {return Regex.IsMatch(path,@"\\WindowsApps\\OpenAI\.Codex_[^\\]+\\app\\ChatGPT\.exe$",RegexOptions.IgnoreCase);}
 static Desktop DescribeDesktop(string path) {
  if(!File.Exists(path)||!IsDesktop(path))throw new Exception("未识别到受支持的 Microsoft Store Codex。请先打开已安装的 Codex 再重试。");
  string manifest=Path.Combine(Directory.GetParent(Path.GetDirectoryName(path)).FullName,"AppxManifest.xml");
  var doc=new XmlDocument();doc.XmlResolver=null;doc.Load(manifest);
  var app=doc.SelectSingleNode("//*[local-name()='Application' and contains(@Executable,'ChatGPT.exe')]");
  if(app==null)throw new Exception("无法识别 Codex 的应用启动入口。");
  return new Desktop {Exe=path,AppId="OpenAI.Codex_2p2nqsd0c76g0!"+app.Attributes["Id"].Value};
 }
 static async Task<Desktop> FindDesktop() {
  return await Task.Run(()=>{
   foreach(var p in Process.GetProcessesByName("ChatGPT"))using(p){string path=ProcessPath(p);if(IsDesktop(path))return DescribeDesktop(path);}
   var psi=new ProcessStartInfo("powershell.exe","-NoProfile -NonInteractive -Command \"(Get-AppxPackage -Name OpenAI.Codex).InstallLocation\"") {UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
   using(var p=Process.Start(psi)){
    string path=p.StandardOutput.ReadToEnd().Trim();p.StandardError.ReadToEnd();
    if(!p.WaitForExit(15000)){p.Kill();throw new Exception("查找 Codex 安装位置超时。");}
    if(String.IsNullOrEmpty(path))throw new Exception("未找到 Microsoft Store Codex，请先安装并打开它。");
    return DescribeDesktop(Path.Combine(path,"app","ChatGPT.exe"));
   }
  });
 }
 async Task ExitCodex() {
  status.Text="正在请求 Codex 正常退出…";
  foreach(var p in Process.GetProcessesByName("ChatGPT"))using(p){if(IsDesktop(ProcessPath(p)))p.CloseMainWindow();}
  for(int i=0;i<6;i++) {
   bool running=false;
   foreach(var p in Process.GetProcessesByName("ChatGPT"))using(p){if(IsDesktop(ProcessPath(p)))running=true;}
   if(!running)break;await Task.Delay(500);
  }
  status.Text="正在结束已确认属于 Codex 的进程…";
  await Task.Run(()=>{
   var snapshot=new System.Collections.Generic.List<OwnedProcess>();
   using(var query=new ManagementObjectSearcher("SELECT ProcessId, ParentProcessId, ExecutablePath FROM Win32_Process"))using(var rows=query.Get())foreach(ManagementObject row in rows)using(row){snapshot.Add(new OwnedProcess{Id=Convert.ToInt32(row["ProcessId"]),Parent=Convert.ToInt32(row["ParentProcessId"]),Path=Convert.ToString(row["ExecutablePath"])});}
   string native=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"OpenAI","Codex");
   foreach(var item in OwnedProcess.Select(snapshot,native,home)) {
    if(item.Id==Process.GetCurrentProcess().Id)continue;
    try{using(var process=Process.GetProcessById(item.Id)){if(!String.Equals(ProcessPath(process),item.Path,StringComparison.OrdinalIgnoreCase))throw new Exception("进程身份改变，已停止切换。");process.Kill();if(!process.WaitForExit(5000))throw new Exception("结束 Codex 进程超时。");}}
    catch(ArgumentException){} // Process exited between the snapshot and opening its handle.
   }
  });
  foreach(var p in Process.GetProcessesByName("ChatGPT"))using(p){if(IsDesktop(ProcessPath(p)))throw new Exception("仍有 Codex 进程运行，未修改登录状态。请稍后重试。");}
  // Independent CLI sessions and shared daemons may still own/refresh auth.json.
  for(int i=0;i<10;i++) {
   var processes=Process.GetProcessesByName("codex");bool running=processes.Length>0;foreach(var p in processes)p.Dispose();
   if(!running)return;await Task.Delay(500);
  }
  throw new Exception("仍有 Codex 命令行或后台服务运行。请正常退出它们后再切换；登录状态未改动。");
 }
 async Task Launch(Desktop app) {
  Process.Start(new ProcessStartInfo("explorer.exe","shell:AppsFolder\\"+app.AppId){UseShellExecute=true});
  for(int i=0;i<30;i++) {
   foreach(var p in Process.GetProcessesByName("ChatGPT"))using(p){if(IsDesktop(ProcessPath(p)))return;}
   await Task.Delay(500);
  }
  throw new Exception("未确认 Codex 已启动。");
 }
 static string FindCli() {
  string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"OpenAI","Codex","bin");
  if(Directory.Exists(root)) {
   var dirs=Directory.GetDirectories(root).OrderByDescending(d=>Directory.GetLastWriteTimeUtc(d));
   foreach(var dir in dirs){string exe=Path.Combine(dir,"codex.exe");if(File.Exists(exe))return exe;}
  }
  throw new Exception("未找到 Codex 自带的登录程序。请先打开并更新 Codex。");
 }
 async Task AddAccount() {
  string cli=FindCli();
  DialogResult method=ChooseLoginMethod();
  if(method==DialogResult.Cancel)return;
  add.Text="等待登录…";
  string loginHome=Path.Combine(dataRoot,"login-"+Guid.NewGuid().ToString("N"));
  // Use the same owner-only directory protection as the vault for temporary credentials.
  new Vault(loginHome);
  string config="cli_auth_credentials_store = \"file\"\r\n";
  string cfg=Path.Combine(home,"config.toml");
  if(File.Exists(cfg))foreach(string line in File.ReadAllLines(cfg))if(Regex.IsMatch(line,@"^\s*forced_(login_method|chatgpt_workspace_id)\s*="))config+=line+"\r\n";
  File.WriteAllText(Path.Combine(loginHome,"config.toml"),config);
  log.Text="即将打开官方登录窗口。请在该窗口完成登录；取消时按 Ctrl+C。";
  log.Visible=true;layout.RowStyles[6].Height=55;
  status.Text="等待官方登录完成。请核实浏览器中选中的账号。";
  try {
   var psi=new ProcessStartInfo(cli,"login"+(method==DialogResult.No?" --device-auth":"")){UseShellExecute=false,CreateNoWindow=false,WorkingDirectory=loginHome};
   psi.EnvironmentVariables["CODEX_HOME"]=loginHome;
   using(var p=Process.Start(psi)) {
    await Task.Run(()=>p.WaitForExit());
    if(p.ExitCode!=0)throw new Exception("官方登录未完成或已取消，当前账号未改动。");
   }
   string path=Path.Combine(loginHome,"auth.json");
   if(!File.Exists(path))throw new Exception("登录未生成文件凭据，未保存账号。请检查组织登录策略。");
   byte[] bytes=File.ReadAllBytes(path); var login=Login.Parse(bytes);vault.Save(bytes);Reload();
   status.Text="已添加 "+login.Email+"；当前 Codex 登录未切换。";
   log.Text="添加完成。需要使用该账号时，在列表选中后点击切换。";
  } finally {
   // Delete only this uniquely created temporary login directory; never CODEX_HOME.
   string expected=Path.GetFullPath(dataRoot)+Path.DirectorySeparatorChar+"login-";
   if(!Path.GetFullPath(loginHome).StartsWith(expected,StringComparison.OrdinalIgnoreCase))throw new Exception("临时登录目录校验失败，未删除文件。");
   if(Directory.Exists(loginHome)&&(File.GetAttributes(loginHome)&FileAttributes.ReparsePoint)==0)Directory.Delete(loginHome,true);
  }
 }
}
static class Program {
 [STAThread] static int Main(string[] args) {
  bool owned;using(var mutex=new Mutex(true,"Local\\CodexLocalSwitcher-v1"+(args.Contains("--preview")?"-preview":""),out owned)){
   if(!owned){MessageBox.Show("账号切换程序已经打开。请回到现有窗口。");return 1;}
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   var window=new Window(args.Contains("--preview"));
   if(args.Length==3&&args[0]=="--preview"&&args[1]=="--snapshot") {
    window.Shown+=(s,e)=>{var timer=new System.Windows.Forms.Timer {Interval=800};timer.Tick+=(sender,ev)=>{timer.Stop();using(var bitmap=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));bitmap.Save(Path.GetFullPath(args[2]));}timer.Dispose();window.Close();};timer.Start();};
   }
   Application.Run(window);return 0;
  }
 }
}
