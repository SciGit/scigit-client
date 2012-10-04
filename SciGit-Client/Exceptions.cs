using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SciGit_Client
{
  class InvalidRepositoryException : Exception
  {
    public Project Project;
    public InvalidRepositoryException(Project p) {
      Project = p;
    }
  }
}
