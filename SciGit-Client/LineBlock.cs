using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SciGit_Filter;

namespace SciGit_Client
{
  public enum BlockType
  {
    Normal,
    Blank,
    ChangeAdd,
    ChangeDelete,
    Conflict,
    Edited
  }

  public class Block
  {
    public string text;
    public BlockType type;

    public Block(string text, BlockType type = BlockType.Normal) {
      this.text = text;
      this.type = type;
    }

    public override string ToString() {
      return text;
    }
  }

  public class Line
  {
    public List<Block> blocks = new List<Block>();

    public Line(string strLine) {
      strLine = SentenceFilter.Clean(strLine);
      string[] strBlocks = strLine.Split('\n');
      foreach (var str in strBlocks) {
        blocks.Add(new Block(str));
      }
    }

    public override string ToString() {
      string ret = "";
      foreach (var block in blocks) {
        string text = block.text;
        if (text.EndsWith(SentenceFilter.SentenceDelim)) {
          text = text.Substring(0, text.Length - SentenceFilter.SentenceDelim.Length);
        }
        ret += text;
      }
      return ret;
    }
  }

  public class LineBlock
  {
    public List<Line> lines = new List<Line>();
    public BlockType type;
    public LineBlock() { }

    public LineBlock(string[] strLines, BlockType type = BlockType.Normal) {
      this.type = type;
      AddLines(strLines);
    }

    public void AddLines(string[] strLines) {
      foreach (string line in strLines) {
        lines.Add(new Line(line));
      }
    }

    public void Append(LineBlock lb) {
      foreach (Line line in lb.lines) {
        lines.Add(line);
      }
    }

    public override string ToString() {
      return String.Join("\n", lines);
    }
  }
}
