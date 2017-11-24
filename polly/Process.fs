namespace polly

open System.Diagnostics
open System.Threading
open System.Text

module Process =

    let run filename args onLine =
        let handle () =
            use p = new Process ()
            p.StartInfo.FileName <- filename
            p.StartInfo.Arguments <- args
            p.StartInfo.UseShellExecute <- false
            p.StartInfo.RedirectStandardOutput <- true
            p.StartInfo.RedirectStandardError <- true
            p.StartInfo.StandardOutputEncoding <- Encoding.UTF8

            p.Start () |> ignore

            use mreOut = new ManualResetEvent false
            use mreErr = new ManualResetEvent false

            p.OutputDataReceived.Add (fun e ->  if isNull e.Data then mreOut.Set () |> ignore
                                                else onLine e.Data)
            p.ErrorDataReceived.Add (fun e ->   if isNull e.Data then mreErr.Set () |> ignore
                                                else onLine e.Data)

            p.BeginOutputReadLine ()
            p.BeginErrorReadLine ()

            p.WaitForExit ()
            mreOut.WaitOne () |> ignore
            mreErr.WaitOne () |> ignore

        let thread = Thread (ThreadStart handle)
        thread.Start ()
        thread.Join ()
