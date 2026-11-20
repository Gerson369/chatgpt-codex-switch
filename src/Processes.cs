using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
class OwnedProcess {
 public int Id,Parent;public string Path;
 public static List<OwnedProcess> Select(List<OwnedProcess> all,string native,string home) {
  Func<string,string,bool> under=(p,root)=>!String.IsNullOrEmpty(p)&&p.StartsWith(root.TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase);
  Func<string,bool> desktop=p=>!String.IsNullOrEmpty(p)&&System.Text.RegularExpressions.Regex.IsMatch(p,@"\\WindowsApps\\OpenAI\.Codex_[^\\]+\\app\\ChatGPT\.exe$",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
  var selected=new HashSet<int>(all.Where(p=>desktop(p.Path)||(under(p.Path,native)&&String.Equals(System.IO.Path.GetFileName(p.Path),"codex.exe",StringComparison.OrdinalIgnoreCase))).Select(p=>p.Id));
  bool changed=true;while(changed){changed=false;foreach(var p in all)if(!selected.Contains(p.Id)&&selected.Contains(p.Parent)&&(under(p.Path,native)||under(p.Path,home)||desktop(p.Path))){selected.Add(p.Id);changed=true;}}
  return all.Where(p=>selected.Contains(p.Id)).Reverse().ToList();
 }
}
