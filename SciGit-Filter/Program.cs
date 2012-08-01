/*
 * Filters LaTeX files for storage in SciGit (see gitattributes: filter)
 * In particular, it attempts to put each sentence on its own line,
 * allowing git to merge/diff at a sentence level.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SciGit_Filter
{
  class Program
  {
    static void PrintUsage() {
      Console.WriteLine("Usage: scigit-filter (--clean|--smudge|--help)");
    }

    static void Main(string[] args) {
      if (args.Count() != 1) {
        Console.WriteLine("Invalid arguments.");
        PrintUsage();
        Environment.Exit(1);
      }

      switch (args[0]) {
        case "--clean":
          Console.Write(SentenceFilter.Clean(Console.In.ReadToEnd()));
          break;
        case "--smudge":
          Console.Write(SentenceFilter.Smudge(Console.In.ReadToEnd()));
          break;
        case "--help":
          PrintUsage();
          break;
        default:
          Console.WriteLine("Unrecognized option.");
          PrintUsage();
          break;
      }
    }
  }
}
