namespace polly

open System
open System.Text
open System.Diagnostics
open System.Threading
open System.IO
open System.IO.Pipes
open NghiaBui.Common.Monads.Rop
open NghiaBui.Common.Misc

module Miner =

    let private make (bootstrap : Process) onLine =
        rop {
            let! pipe = try new NamedPipeServerStream ("Polly", PipeDirection.In) |> Ok
                        with _ -> (id, id) |> Error
            let close = wrap pipe.Dispose
            
            (Thread (ThreadStart (fun _ -> Thread.Sleep 500; bootstrap.Start () |> ignore))).Start ()

            do! try pipe.WaitForConnection () |> Ok
                with _ -> (id, close) |> Error
            
            let stream = new StreamReader (pipe, Encoding.UTF8)
            let close = (wrap stream.Dispose) >> close

            let! minerProcessId = try stream.ReadLine () |> Ok
                                    with _ -> (id, close) |> Error
            let miner = minerProcessId |> int |> Process.GetProcessById

            let stop = wrap miner.Kill >> wrap bootstrap.Kill >> wrap stream.Dispose >> wrap pipe.Dispose

            let rec wait () =
                let line = try stream.ReadLine () with _ -> null
                if isNull line then
                    stop ()
                else
                    onLine line
                    wait ()
            
            return! (wait, stop) |> Error }
        |> function
        | Error (w, s) -> WaitFun w, StopFun s
        | Ok _ -> failShouldNotGoHere ()

    let run noDevFee path args onLine =
        let bootstrap = new Process ()
        let baseDir = AppDomain.CurrentDomain.BaseDirectory
        bootstrap.StartInfo.WorkingDirectory <- baseDir
        bootstrap.StartInfo.UseShellExecute <- false
        bootstrap.StartInfo.FileName <- Path.Combine (baseDir, "bootstrap.exe")
        bootstrap.StartInfo.Arguments <- sprintf "%s %s %s" (if noDevFee then "yes" else "no") path args

        make bootstrap onLine
        