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
    public const string NewlineDelim = "%%%%%%%NL%%%%%%%";
    public const string WindowsNewlineDelim = "%%%%%%%WNL%%%%%%%";
    public const string ConflictStart = "%%%%%%%CS%%%%%%%";
    public const string ConflictDelim = "%%%%%%%CD%%%%%%%";
    public const string ConflictEnd = "%%%%%%%CE%%%%%%%";

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
        if (IsWord(tokens.FirstOrDefault(), true) && IsWord(lastToken)) {
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

      // Replace all existing newlines with our own newline indicator.
      // This lets us remember where the newlines originally were (when we add newlines for sentences)
      str = "";
      foreach (var line in merged) {
        string cleanLine;
        if (line.EndsWith("\r\n")) {
          cleanLine = line.Replace("\r\n", "\n" + WindowsNewlineDelim + "\n");
        } else {
          cleanLine = line.Replace("\n", "\n" + NewlineDelim + "\n");
        }
        // Don't want any empty lines
        if (cleanLine.StartsWith("\n")) {
          cleanLine = cleanLine.Substring(1);
        }
        str += cleanLine;
      }

      // Finally, split any sentences on the same line.
      // We'll define the end of a sentence to be a lowercase letter,
      // followed by [.!?] plus some whitespace, then a capital letter.
      str = Regex.Replace(str, @"([a-z][\.!?][ \t]+)([A-Z])", match => {
        return match.Groups[1] + "\n" + match.Groups[2];
      });

      return str;
    }

    public static string Smudge(string str) {
      str = str.Replace("\r\n", "\n");
      string[] lines = str.Split('\n');
      for (int i = 0; i < lines.Length; i++) {
        if (lines[i].StartsWith("<<<<<<<")) {
          lines[i] = ConflictStart;
        } else if (lines[i].StartsWith("=======")) {
          lines[i] = ConflictDelim;
        } else if (lines[i].StartsWith(">>>>>>>")) {
          lines[i] = ConflictEnd;
        }
      }
      str = String.Join("\n", lines);

      str = str.Replace("\n", "");
      str = str.Replace(MergedNewlineDelim, "\n");
      str = str.Replace(MergedWindowsNewlineDelim, "\r\n");
      str = str.Replace(NewlineDelim, "\n");
      str = str.Replace(WindowsNewlineDelim, "\r\n");
      return str;
    }
  }
}
