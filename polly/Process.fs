namespace polly

open System.Diagnostics
open System.Threading

module Process =

    let run filename args =
        use p = new Process ()
        p.StartInfo.FileName <- filename
        p.StartInfo.Arguments <- args
        p.StartInfo.UseShellExecute <- false
        p.StartInfo.CreateNoWindow <- false
        p.StartInfo.WindowStyle <- ProcessWindowStyle.Normal
        p.StartInfo.RedirectStandardOutput <- true


        use mreOut = new ManualResetEvent false
        p.OutputDataReceived.Add (fun e ->  if isNull e.Data then mreOut.Set () |> ignore
                                            else printfn "output: %s" e.Data )
        p.Start () |> ignore
        p.BeginOutputReadLine ()

        p.WaitForExit ()
        mreOut.WaitOne () |> ignore
