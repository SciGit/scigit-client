using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SciGit_Filter
{
  public class SentenceFilter
  {
    public const string MergedNewlineDelim = "%%%%%%%MNL%%%%%%%";
    public const string MergedWindowsNewlineDelim = "%%%%%%%MWNL%%%%%%%";
    public const string SentenceDelim = "%%%%%%%SNL%%%%%%%";
    public static bool MergeSentences = false;

    public static string[] SplitLines(string str) {
      return str.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.None);
    }

    public static bool IsBinary(string str) {
      return str.Contains('\0');
    }

    public static bool IsWord(string str, bool allowEnd = false) {
      if (str == null) return false;
      string patt = String.Format(@"^[""']?\w+[""',:;{0}]?$", allowEnd ? @"\.!?" : "");
      return Regex.Match(str, patt).Success;
    }

    public static string Clean(string str) {
      // First, merge any sentences spanning multiple lines.
      // We'll just assume any letter/punctuation, followed by whitespace and a newline,
      // followed by whitespace and another letter satisfies this.
      String[] lines = Regex.Split(str, @"(?<=\n|\r\n)");
      List<String> merged = new List<String>();
      string currentLine = lines[0];
      string[] tokens = lines[0].Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
      string lastToken = tokens.LastOrDefault();
      for (int i = 1; i < lines.Length; i++) {
        tokens = lines[i].Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
        if (MergeSentences && IsWord(tokens.FirstOrDefault(), true) && IsWord(lastToken)) {
          currentLine = currentLine.Replace("\r\n", MergedWindowsNewlineDelim);
          currentLine = currentLine.Replace("\n", MergedNewlineDelim);
          currentLine += lines[i];
          lastToken = tokens.LastOrDefault();
        } else {
          merged.Add(currentLine);
          currentLine = lines[i];
          lastToken = tokens.LastOrDefault();
        }
      }
      merged.Add(currentLine);
      str = String.Join("", merged);

      // Split any sentences on the same line.
      // We'll define the end of a sentence to be a lowercase letter,
      // followed by [.!?] plus some whitespace, then a capital letter.
      str = Regex.Replace(str, @"([a-z][\.!?][ \t]+)([A-Z])", match => {
        return match.Groups[1] + "\n" + match.Groups[2];
      });

      return str;
    }

    public static string Smudge(string str) {
      str = str.Replace(SentenceDelim + "\n", "");
      str = str.Replace(MergedNewlineDelim, "\n");
      str = str.Replace(MergedWindowsNewlineDelim, "\r\n");
      return str;
    }
  }
}
