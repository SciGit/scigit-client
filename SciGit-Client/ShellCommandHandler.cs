using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace SciGit_Client
{
  class ShellCommandHandler
  {
    public delegate void CommandHandler(string s, string t);
    private NamedPipeServerStream pipeServer;
    private Thread pipeThread;

    public ShellCommandHandler(CommandHandler cmdHandler) {
      pipeServer = new NamedPipeServerStream("sciGitPipe", PipeDirection.In, 1, PipeTransmissionMode.Byte,
                                             PipeOptions.Asynchronous); 
      pipeThread = new Thread(() => {
        while (true) {
          try {
            var ar = pipeServer.BeginWaitForConnection(null, null);
            pipeServer.EndWaitForConnection(ar);
            var ss = new StreamString(pipeServer);
            string verb = ss.ReadString();
            string filename = ss.ReadString();
            cmdHandler(verb, filename);
            pipeServer.Disconnect();
          } catch (ObjectDisposedException) {
            break;
          } catch (IOException) {
            break;
          } catch (Exception e) {
            Logger.LogException(e);
          }
        }
      });
    }

    public void Start() {
      pipeThread.Start();
    }

    public void Stop() {
      pipeServer.Dispose();
      pipeThread.Join();
    }
  }
}
