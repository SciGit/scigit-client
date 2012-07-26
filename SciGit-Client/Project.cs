using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SciGit_Client
{
  class Project
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public int OwnerId { get; set; }
    public int CreatedTime { get; set; }
  }
}
