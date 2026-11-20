using System;
using System.IO;
using System.Text;
class Tests {
 static int count;
 static void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; }
 static byte[] Auth(string id, string email,string plan="unknown") {
  string jwt = "e30." + Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"email\":\""+email+"\",\"https://api.openai.com/auth\":{\"chatgpt_plan_type\":\""+plan+"\"}}")).TrimEnd('=').Replace('+','-').Replace('/','_') + ".signature";
  return Encoding.UTF8.GetBytes("{\"auth_mode\":\"chatgpt\",\"tokens\":{\"account_id\":\""+id+"\",\"access_token\":\""+jwt+"\",\"id_token\":\""+jwt+"\",\"refresh_token\":\"test-refresh-secret\"}}");
 }
 static void Reject(Action action, string name) { bool rejected=false; try { action(); } catch { rejected=true; } Check(rejected,name); }
 public static int Main(string[] args) {
  if(args.Length>0&&args[0]=="--utf8-child"){byte[] fixture=Encoding.UTF8.GetBytes("{\"planType\":\"business\",\"name\":\"中文团队\"}\n");using(var stdout=Console.OpenStandardOutput())stdout.Write(fixture,0,fixture.Length);return 0;}
  string root=Path.Combine(Path.GetTempPath(),"codex-switch-test-"+Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(root);
  try {
   byte[] a=Auth("workspace-a","a@example.test"), b=Auth("workspace-b","b@example.test");
   var originalEncoding=Console.OutputEncoding;string decoded;
   try{Console.OutputEncoding=Encoding.GetEncoding(936);var psi=QuotaReader.ProcessInfo(System.Reflection.Assembly.GetExecutingAssembly().Location,root);psi.Arguments="--utf8-child";using(var child=System.Diagnostics.Process.Start(psi)){decoded=child.StandardOutput.ReadToEnd();child.WaitForExit();}}finally{Console.OutputEncoding=originalEncoding;}
   Check(new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<System.Collections.Generic.Dictionary<string,object>>(decoded)["name"].ToString()=="中文团队","UTF8 Business response survives Chinese Windows encoding");
   Check(Login.Parse(a).Email=="a@example.test","JWT identity");
   var searchable=Login.Parse(a);searchable.Note="星河团队";
   Check(searchable.MatchesQuery(" A@EXAMPLE ")&&searchable.MatchesQuery("星河"),"search matches email and note ignoring case");
   Check(!searchable.MatchesQuery("not-present")&&searchable.MatchesQuery(""),"search no match and reset");
   Check(Login.Parse(Auth("work","a@example.test","business")).PlanLabel=="团队版 / Business","business plan");
   Check(Login.Parse(Auth("work","a@example.test","self_serve_business_prolite")).PlanLabel=="团队版 / Business","server business variant");
   Check(Login.Parse(Auth("personal","a@example.test","plus")).PlanLabel=="个人版 / Plus","personal plan");
   Check(Login.Parse(Auth("work","a@example.test","enterprise")).PlanLabel=="企业版 / Enterprise","enterprise plan");
   Check(Login.Parse(Auth("work","a@example.test")).Key!=Login.Parse(Auth("personal","a@example.test")).Key,"same email separate workspace");
   var json=new System.Web.Script.Serialization.JavaScriptSerializer();
   var limits=json.Deserialize<System.Collections.Generic.Dictionary<string,object>>("{\"rateLimits\":{\"primary\":{\"usedPercent\":25,\"windowDurationMins\":300},\"secondary\":{\"usedPercent\":10,\"windowDurationMins\":10080}}}");
   Check(QuotaReader.Format(limits)=="5 小时剩余 75%    |    每周剩余 90%","quota windows remaining");
   Check(QuotaReader.Format(json.Deserialize<System.Collections.Generic.Dictionary<string,object>>("{}"))=="服务未返回额度","missing quota is unknown");
   var display=QuotaDisplay.Parse("5 小时剩余 75% · 今天 20:00 重置 | 每周剩余 0%");
   Check(display.Count==2&&display[0].Remaining==75&&display[1].Remaining==0&&display[0].Reset=="重置 今天 20:00","quota bars retain percent and reset");
   Check(QuotaDisplay.Parse("未知 · 登录凭据已过期").Count==0,"unknown quota has no fake progress");
   Check(QuotaDisplay.Parse("每周剩余 99%").Count==1,"weekly only never duplicates five hour quota");
   Check(QuotaDisplay.Parse("5 小时剩余 125%").Count==0,"invalid percent rejected");
   var processes=new System.Collections.Generic.List<OwnedProcess>{new OwnedProcess{Id=1,Path=@"C:\Program Files\WindowsApps\OpenAI.Codex_1_x64\app\ChatGPT.exe"},new OwnedProcess{Id=2,Parent=1,Path=@"C:\native\codex.exe"},new OwnedProcess{Id=3,Parent=1,Path=@"C:\Browser\msedge.exe"},new OwnedProcess{Id=4,Path=@"C:\Other\ChatGPT.exe"},new OwnedProcess{Id=5,Path=@"C:\native-evil\codex.exe"}};
   var owned=OwnedProcess.Select(processes,@"C:\native",@"C:\home");
   Check(owned.Count==2&&owned.Exists(p=>p.Id==1)&&owned.Exists(p=>p.Id==2),"only verified Codex processes selected, browsers excluded");
   Reject(()=>Login.Parse(Encoding.UTF8.GetBytes("{\"OPENAI_API_KEY\":\"secret\"}")),"API key rejected");
   Reject(()=>Login.Parse(Encoding.UTF8.GetBytes("{}")),"incomplete auth rejected");
   var store=new Vault(Path.Combine(root,"vault"));
   string key=store.Save(a); Check(store.List().Count==1,"save account");
   store.SetNote(key,"个人工作区");Check(store.List()[0].Note=="个人工作区","notes persisted without replacing login");
   store.SetPlan(key,"business");Check(store.List()[0].Plan=="business","server plan persists for older tokens");
   Check(Encoding.UTF8.GetString(store.Load(key)).Contains("test-refresh-secret"),"encrypted round trip");
   Check(!Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(root,"vault",key+".bin"))).Contains("test-refresh-secret"),"no plaintext secret on disk");
   store.Save(a); Check(store.List().Count==1,"deduplicate");
   string auth=Path.Combine(root,"auth.json"); File.WriteAllBytes(auth,a);
   string sessions=Path.Combine(root,"sessions.txt"); File.WriteAllText(sessions,"unchanged");
   store.Activate(b,auth); Check(Login.Parse(File.ReadAllBytes(auth)).AccountId=="workspace-b","switch");
   Check(File.ReadAllText(sessions)=="unchanged","session untouched");
   Reject(()=>store.Restore(Path.Combine(root,"other-auth.json")),"recovery bound to original home");
   store.Restore(auth); Check(Login.Parse(File.ReadAllBytes(auth)).AccountId=="workspace-a","rollback");
   Reject(()=>store.Activate(Encoding.UTF8.GetBytes("{}"),auth),"invalid target blocked");
   Check(Login.Parse(File.ReadAllBytes(auth)).AccountId=="workspace-a","bad target keeps current");
   store.Activate(b,auth); Reject(()=>store.Activate(a,auth),"pending recovery not overwritten");
   store.Commit(); Check(!store.HasRecovery,"commit clears recovery");
   byte[] damaged=File.ReadAllBytes(Path.Combine(root,"vault",key+".bin")); damaged[damaged.Length-1]^=1;
   File.WriteAllBytes(Path.Combine(root,"vault",key+".bin"),damaged);
   Reject(()=>store.Load(key),"tamper rejected");
   Reject(()=>store.Load("../auth"),"path traversal rejected");
   Console.WriteLine("PASS: "+count+" checks, synthetic credentials only"); return 0;
  } catch(Exception e) { Console.WriteLine("FAIL: "+e.Message); return 1; }
  finally { Directory.Delete(root,true); }
 }
}
