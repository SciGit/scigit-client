using System.IO;

namespace SciGit_Client
{
  class Util
  {
    public static string PathCombine(params string[] args) {
      if (args.Length == 0) return "";
      if (args.Length == 1) return args[0];
      string s = Path.Combine(args[0], args[1]);
      for (int i = 2; i < args.Length; i++) {
        s = Path.Combine(s, args[i]);
      }
      return s;
    }
  }
}
