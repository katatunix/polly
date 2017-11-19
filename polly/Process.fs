namespace polly

open System.Diagnostics
open System.Threading

module Process =

    let run filename args =
        use p = new Process ()
        p.StartInfo.FileName <- filename
        p.StartInfo.Arguments <- args
        p.StartInfo.UseShellExecute <- false
        p.StartInfo.CreateNoWindow <- true
        //p.StartInfo.WindowStyle <- ProcessWindowStyle.Hidden
        p.StartInfo.RedirectStandardError <- true


        use mre = new ManualResetEvent false
        p.ErrorDataReceived.Add (fun e ->   if isNull e.Data then mre.Set () |> ignore
                                            else printfn "%s" e.Data )
        p.Start () |> ignore
        p.BeginErrorReadLine ()

        p.WaitForExit ()
        mre.WaitOne () |> ignore
        p.ExitCode
