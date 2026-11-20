using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
class QuotaReader {
 public static ProcessStartInfo ProcessInfo(string cli,string directory){return new ProcessStartInfo(cli,"app-server --listen stdio://"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=System.Text.Encoding.UTF8,StandardErrorEncoding=System.Text.Encoding.UTF8,WorkingDirectory=directory};}
 public static string Format(Dictionary<string,object> result) {
  object raw;if(!result.TryGetValue("rateLimits",out raw)||!(raw is Dictionary<string,object>))return "服务未返回额度";
  var bucket=(Dictionary<string,object>)raw;var lines=new List<string>();
  foreach(string key in new[]{"primary","secondary"}) {
   object w;if(!bucket.TryGetValue(key,out w)||!(w is Dictionary<string,object>))continue;
   var window=(Dictionary<string,object>)w;object used,mins;
   if(!window.TryGetValue("usedPercent",out used)||used==null)continue;
   double percent=Convert.ToDouble(used);if(Double.IsNaN(percent)||percent<0||percent>100)continue;
   string label="额度";
   if(window.TryGetValue("windowDurationMins",out mins)&&mins!=null){int n=Convert.ToInt32(mins);label=n==10080?"每周":n%60==0?(n/60)+" 小时":n+" 分钟";}
   string reset="";object at;
   if(window.TryGetValue("resetsAt",out at)&&at!=null)try{reset=" · "+DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(at)).ToLocalTime().ToString("MM-dd HH:mm")+" 重置";}catch{}
   lines.Add(label+"剩余 "+(100-percent).ToString("0.#")+"%"+reset);
  }
  return lines.Count==0?"服务未返回额度窗口":String.Join("    |    ",lines);
 }
 static async Task<Dictionary<string,object>> Call(Process p,int id,string method,object parameters) {
  var json=new JavaScriptSerializer();p.StandardInput.WriteLine(json.Serialize(new {id=id,method=method,@params=parameters}));p.StandardInput.Flush();
  var deadline=DateTime.UtcNow.AddSeconds(20);
  while(DateTime.UtcNow<deadline) {
   Task<string> read=p.StandardOutput.ReadLineAsync();
   if(await Task.WhenAny(read,Task.Delay(Math.Max(1,(int)(deadline-DateTime.UtcNow).TotalMilliseconds)))!=read)break;
   string line=await read;if(line==null)throw new Exception("额度服务已退出");
   Dictionary<string,object> msg;try{msg=json.Deserialize<Dictionary<string,object>>(line);}catch{throw new Exception("额度返回解析失败，请更新程序后重试。");}object key;
   if(msg.TryGetValue("method",out key)&&Convert.ToString(key)=="account/chatgptAuthTokens/refresh")throw new Exception("登录凭据已过期，请重新添加该账号");
   if(!msg.TryGetValue("id",out key)||Convert.ToString(key)!=id.ToString())continue;
   if(msg.ContainsKey("error"))throw new Exception("暂时无法读取，请稍后重试或重新添加账号");
   return (Dictionary<string,object>)msg["result"];
  }
  throw new Exception("读取超时，请稍后刷新");
 }
 public static async Task<string> Read(string cli,string root,string originalHome,byte[] bytes,Action<string> onPlan=null) {
  string temp=Path.Combine(root,"quota-"+Guid.NewGuid().ToString("N"));new Vault(temp);
  Process p=null;
  try {
   string config="cli_auth_credentials_store = \"ephemeral\"\r\n";
   string source=Path.Combine(originalHome,"config.toml");
   if(File.Exists(source))foreach(string line in File.ReadAllLines(source))if(System.Text.RegularExpressions.Regex.IsMatch(line,@"^\s*forced_(login_method|chatgpt_workspace_id)\s*="))config+=line+"\r\n";
   File.WriteAllText(Path.Combine(temp,"config.toml"),config);
   var psi=ProcessInfo(cli,temp);
   psi.EnvironmentVariables["CODEX_HOME"]=temp;psi.EnvironmentVariables.Remove("OPENAI_API_KEY");psi.EnvironmentVariables.Remove("CODEX_ACCESS_TOKEN");
   p=Process.Start(psi);p.ErrorDataReceived+=(s,e)=>{};p.BeginErrorReadLine();
   await Call(p,1,"initialize",new {clientInfo=new{name="codex_local_switcher",version="2.0"},capabilities=new{experimentalApi=true}});
   p.StandardInput.WriteLine("{\"method\":\"initialized\",\"params\":{}}");p.StandardInput.Flush();
   var doc=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(System.Text.Encoding.UTF8.GetString(bytes));var tokens=(Dictionary<string,object>)doc["tokens"];
   var login=Login.Parse(bytes);var args=new Dictionary<string,object>{{"type","chatgptAuthTokens"},{"accessToken",tokens["access_token"]},{"chatgptAccountId",login.AccountId}};
   if(login.Plan!="unknown")args["chatgptPlanType"]=login.Plan;
   await Call(p,2,"account/login/start",args);
   var info=await Call(p,4,"account/read",new{refreshToken=false});object account;
   if(info.TryGetValue("account",out account)&&account is Dictionary<string,object>){var a=(Dictionary<string,object>)account;object email,plan;if(a.TryGetValue("email",out email)&&email!=null&&login.Email!="已登录账号"&&!String.Equals(Convert.ToString(email),login.Email,StringComparison.OrdinalIgnoreCase))throw new Exception("账号身份不匹配，未显示额度");if(a.TryGetValue("planType",out plan)&&plan!=null&&onPlan!=null)onPlan(Convert.ToString(plan));}
   return Format(await Call(p,3,"account/rateLimits/read",new{}))+"  ·  "+DateTime.Now.ToString("HH:mm")+" 更新";
  } finally {
   if(p!=null){try{p.StandardInput.Close();if(!p.WaitForExit(1000)){p.Kill();p.WaitForExit(3000);}}finally{p.Dispose();}}
   string resolved=Path.GetFullPath(temp);
   if(resolved.StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar+"quota-",StringComparison.OrdinalIgnoreCase)&&Directory.Exists(resolved)&&(File.GetAttributes(resolved)&FileAttributes.ReparsePoint)==0) {
    // Ephemeral query homes contain no saved auth; background cache downloads can briefly hold files.
    for(int i=0;i<8;i++)try{Directory.Delete(resolved,true);break;}catch(IOException){System.Threading.Thread.Sleep(250);}catch(UnauthorizedAccessException){break;}
   }
  }
 }
}
