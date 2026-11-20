using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Web.Script.Serialization;
class Login {
 public string Email, AccountId, Key, Plan="unknown", Note="", Quota="额度尚未读取";
 public string PlanLabel {get {string p=Plan.ToLowerInvariant();if(p.Contains("enterprise"))return "企业版 / Enterprise";if(p.Contains("business")||p=="team")return "团队版 / Business";if(p=="edu")return "教育版 / Edu";if(p.Contains("plus"))return "个人版 / Plus";if(p=="pro"||p=="self_serve_pro")return "个人版 / Pro";if(p=="free")return "个人版 / Free";return "套餐待识别";}}
 public override string ToString() { return Email+" · "+PlanLabel; }
 public bool MatchesQuery(string query) {return (Email+" "+Note+" "+PlanLabel+" "+AccountId).IndexOf((query??"").Trim(),StringComparison.OrdinalIgnoreCase)>=0;}
 public static Dictionary<string,object> Claims(string jwt) {string p=jwt.Split('.')[1].Replace('-','+').Replace('_','/');return new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(Encoding.UTF8.GetString(Convert.FromBase64String(p.PadRight((p.Length+3)/4*4,'='))));}
 public static Login Parse(byte[] data) {
  if(data==null || data.Length>1024*1024) throw new Exception("登录文件大小异常。");
  try {
   var json=new JavaScriptSerializer();
   var doc=json.Deserialize<Dictionary<string,object>>(Encoding.UTF8.GetString(data));
   if(doc.ContainsKey("auth_mode") && (string)doc["auth_mode"]!="chatgpt") throw new Exception();
   var tokens=(Dictionary<string,object>)doc["tokens"];
   string id=(string)tokens["account_id"], access=(string)tokens["access_token"], refresh=(string)tokens["refresh_token"];
   if(String.IsNullOrWhiteSpace(id)||String.IsNullOrWhiteSpace(access)||String.IsNullOrWhiteSpace(refresh)) throw new Exception();
   string email="已登录账号";
   object jwt; if(!tokens.TryGetValue("id_token",out jwt)||!(jwt is string)||String.IsNullOrEmpty((string)jwt)) jwt=access;
   string payload=((string)jwt).Split('.')[1].Replace('-','+').Replace('_','/');
   payload=payload.PadRight((payload.Length+3)/4*4,'=');
   var claims=json.Deserialize<Dictionary<string,object>>(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
   object value; if(claims.TryGetValue("email",out value)&&value is string) email=(string)value;
   // JWT claims are display hints; only the official login flow authenticates an account.
   string user=""; object auth;
   if(claims.TryGetValue("https://api.openai.com/auth",out auth) && auth is Dictionary<string,object>) {
    var fields=(Dictionary<string,object>)auth;
    if(fields.TryGetValue("chatgpt_user_id",out value)) user=Convert.ToString(value);
   }
   string key; using(var sha=SHA256.Create()) key=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(id+"\n"+user+"\n"+email))).Replace("-","").ToLowerInvariant();
   string plan="unknown";
   try {var ac=Claims(access);var acAuth=(Dictionary<string,object>)ac["https://api.openai.com/auth"];object v;if(acAuth.TryGetValue("chatgpt_plan_type",out v))plan=Convert.ToString(v);}catch{}
   if(plan=="unknown" && auth is Dictionary<string,object>){object v;if(((Dictionary<string,object>)auth).TryGetValue("chatgpt_plan_type",out v))plan=Convert.ToString(v);}
   return new Login {Email=email, AccountId=id, Key=key,Plan=plan};
  } catch { throw new Exception("不是受支持的 ChatGPT 登录文件，或文件已损坏。API Key 和系统凭据库模式暂不支持。"); }
 }
}
class Vault {
 readonly string root;
 readonly byte[] entropy=Encoding.UTF8.GetBytes("CodexLocalSwitcher.v1");
 public Vault(string directory) {
  root=Path.GetFullPath(directory); Directory.CreateDirectory(root);
  if((File.GetAttributes(root)&FileAttributes.ReparsePoint)!=0) throw new Exception("账号目录不能是链接或联接点。");
  var acl=new DirectorySecurity(); acl.SetAccessRuleProtection(true,false);
  acl.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User,FileSystemRights.FullControl,InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit,PropagationFlags.None,AccessControlType.Allow));
  Directory.SetAccessControl(root,acl);
 }
 string AccountPath(string key) {
  if(key==null || !System.Text.RegularExpressions.Regex.IsMatch(key,"\\A[a-f0-9]{64}\\z")) throw new Exception("账号标识无效。");
  return Path.Combine(root,key+".bin");
 }
 static void RejectLink(string path) { if(File.Exists(path)&&(File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0) throw new Exception("登录文件不能是链接。"); }
 public static void Atomic(string path,byte[] data) {
  RejectLink(path); string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
  try {
   using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)) { stream.Write(data,0,data.Length); stream.Flush(true); }
   if(File.Exists(path)) { File.SetAccessControl(temp,File.GetAccessControl(path)); File.Replace(temp,path,null); }
   else File.Move(temp,path);
  } finally { if(File.Exists(temp)) File.Delete(temp); }
 }
 byte[] Encrypt(byte[] data) { return ProtectedData.Protect(data,entropy,DataProtectionScope.CurrentUser); }
 byte[] Decrypt(string path) { RejectLink(path); return ProtectedData.Unprotect(File.ReadAllBytes(path),entropy,DataProtectionScope.CurrentUser); }
 public string Save(byte[] data) {
  var login=Login.Parse(data); string path=AccountPath(login.Key);
  byte[] encrypted=Encrypt(data);
  if(!Equal(data,ProtectedData.Unprotect(encrypted,entropy,DataProtectionScope.CurrentUser))) throw new Exception("加密校验失败。");
  Atomic(path,encrypted); return login.Key;
 }
 static bool Equal(byte[] a,byte[] b) { if(a.Length!=b.Length)return false; for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false; return true; }
 public List<Login> List() {
  var accounts=new List<Login>();
  foreach(string path in Directory.GetFiles(root,"*.bin")) {
   if(Path.GetFileName(path)=="recovery.bin")continue;
   var login=Login.Parse(Load(Path.GetFileNameWithoutExtension(path)));
   string note=Path.Combine(root,login.Key+".note");if(File.Exists(note))login.Note=File.ReadAllText(note);
   string plan=Path.Combine(root,login.Key+".plan");if(File.Exists(plan))login.Plan=File.ReadAllText(plan);
   accounts.Add(login);
  }
  accounts.Sort((a,b)=>String.Compare(a.Email,b.Email,StringComparison.OrdinalIgnoreCase)); return accounts;
 }
 public byte[] Load(string key) { byte[] data=Decrypt(AccountPath(key)); if(Login.Parse(data).Key!=key)throw new Exception("账号备份身份不符。"); return data; }
 public void SetNote(string key,string note) {AccountPath(key);if(note.Length>80)throw new Exception("备注最多 80 个字。");Atomic(Path.Combine(root,key+".note"),Encoding.UTF8.GetBytes(note));}
 public void SetPlan(string key,string plan) {AccountPath(key);if(!System.Text.RegularExpressions.Regex.IsMatch(plan,"\\A[a-zA-Z0-9_-]{1,40}\\z"))return;Atomic(Path.Combine(root,key+".plan"),Encoding.UTF8.GetBytes(plan));}
 string Recovery { get { return Path.Combine(root,"recovery.bin"); } }
 public bool HasRecovery { get { return File.Exists(Recovery); } }
 public void Activate(byte[] data,string path) {
  Login.Parse(data);
  if(HasRecovery) throw new Exception("请先确认上次切换成功，或恢复切换前的账号。");
  if(!File.Exists(path)) throw new Exception("当前登录文件不存在。请先在 Codex 中正常登录。");
  RejectLink(path); byte[] old=File.ReadAllBytes(path); Save(old);
  var recovery=new Dictionary<string,string>(); recovery["path"]=Path.GetFullPath(path); recovery["auth"]=Convert.ToBase64String(old);
  Atomic(Recovery,Encrypt(Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(recovery))));
  try { Atomic(path,data); if(!Equal(data,File.ReadAllBytes(path)))throw new Exception("写入校验失败。"); }
  catch { Atomic(path,old); throw; }
 }
 public void Restore(string path) {
  var record=new JavaScriptSerializer().Deserialize<Dictionary<string,string>>(Encoding.UTF8.GetString(Decrypt(Recovery)));
  if(!String.Equals(record["path"],Path.GetFullPath(path),StringComparison.OrdinalIgnoreCase))throw new Exception("恢复记录属于另一个 Codex 目录。请使用原来的 CODEX_HOME。");
  byte[] old=Convert.FromBase64String(record["auth"]);Login.Parse(old);
  if(File.Exists(path)) {
   byte[] outgoing=File.ReadAllBytes(path);bool valid=true;try{Login.Parse(outgoing);}catch{valid=false;}
   if(valid)Save(outgoing);
  }
  Atomic(path,old);Commit();
 }
 public void Commit() { if(HasRecovery)File.Delete(Recovery); }
}
