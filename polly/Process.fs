namespace polly

open System
open System.Diagnostics
open System.Threading
open System.Text
open System.IO
open System.IO.Pipes

module Process =

    let private makeResult (p : Process) handle =
        let thread = Thread (ThreadStart handle)
        let start   = fun () -> thread.Start ()
        let wait    = fun () -> thread.Join ()
        let stop    = fun () -> try p.Kill () with _ -> ()
        start, wait, stop

    let create filename args onLine =
        let p = new Process ()
        p.StartInfo.FileName <- filename
        p.StartInfo.Arguments <- args
        p.StartInfo.UseShellExecute <- false
        p.StartInfo.RedirectStandardOutput <- true
        p.StartInfo.RedirectStandardError <- true
        p.StartInfo.StandardOutputEncoding <- Encoding.UTF8

        let handle () =
            p.Start () |> ignore

            use mreOut = new ManualResetEvent false
            use mreErr = new ManualResetEvent false

            p.OutputDataReceived.Add    (fun e ->   if isNull e.Data then mreOut.Set () |> ignore
                                                    else onLine e.Data)
            p.ErrorDataReceived.Add     (fun e ->   if isNull e.Data then mreErr.Set () |> ignore
                                                    else onLine e.Data)

            p.BeginOutputReadLine ()
            p.BeginErrorReadLine ()

            p.WaitForExit ()

            mreOut.WaitOne () |> ignore
            mreErr.WaitOne () |> ignore

        makeResult p handle

    let bootstrap noDevFee filename args onLine =
        let bootstrap = new Process ()
        let baseDir = AppDomain.CurrentDomain.BaseDirectory
        bootstrap.StartInfo.WorkingDirectory <- baseDir
        bootstrap.StartInfo.UseShellExecute <- false
        bootstrap.StartInfo.FileName <- Path.Combine (baseDir, "bootstrap.exe")
        bootstrap.StartInfo.Arguments <- sprintf "%s %s %s" (if noDevFee then "yes" else "no") filename args

        let handle () =
            use pipe = new NamedPipeServerStream ("Polly", PipeDirection.In)
            async {
                do! Async.Sleep 500
                bootstrap.Start () |> ignore }
            |> Async.Start

            pipe.WaitForConnection ()

            use stream = new StreamReader (pipe, Encoding.UTF8)
            let rec loop () =
                let line = stream.ReadLine ()
                if not (isNull line) then
                    onLine line
                    loop ()
            loop ()

        makeResult bootstrap handle
