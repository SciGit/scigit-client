using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SciGit_Client
{
  class Tuple<T1, T2>
  {
    public T1 Item1 { get; set; }
    public T2 Item2 { get; set; }

    public Tuple(T1 first, T2 second) {
      Item1 = first;
      Item2 = second;
    }
  }

  class Tuple
  {
    public static Tuple<T1, T2> Create<T1, T2>(T1 first, T2 second) {
      return new Tuple<T1, T2>(first, second);
    }
  }
}
