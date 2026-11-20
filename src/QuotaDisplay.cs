using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
class QuotaDisplay {
 public string Label,Reset;public double Remaining;
 public static List<QuotaDisplay> Parse(string text) {
  var result=new List<QuotaDisplay>();
  foreach(string part in (text??"").Split('|')) {
   var m=Regex.Match(part,@"(?<name>\d+\s*小时|每周|\d+\s*分钟|额度)剩余\s*(?<n>\d+(?:\.\d+)?)%");double n;
   if(!m.Success||!Double.TryParse(m.Groups["n"].Value,NumberStyles.Float,CultureInfo.InvariantCulture,out n)||n<0||n>100)continue;
   var reset=Regex.Match(part,@"·\s*(?<time>[^·|]+?)\s*重置");
   result.Add(new QuotaDisplay{Label=m.Groups["name"].Value+"额度",Remaining=n,Reset=reset.Success?"重置 "+reset.Groups["time"].Value.Trim():"重置时间未返回"});
  }
  return result;
 }
}
