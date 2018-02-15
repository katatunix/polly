namespace polly

open System
open System.IO
open System.Threading
open Config
open ErrorDetection
open Miner

module Monitor =

    let private sendFireEmail sender toAddresses (fi : FireInfo)  =
        try
            Email.sendFire sender toAddresses fi.Reason fi.UpTime.Value fi.Action fi.Log
        with _ -> ()

    let private execFile =
        Option.iter (fun file ->
            let path = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, file)
            System.Diagnostics.Process.Start path |> ignore)

    type private Message =
        | Start of (unit -> WaitFun * StopFun) * AsyncReplyChannel<WaitFun>
        | Stop of AsyncReplyChannel<unit>

    let private monitorBody (mailbox : MailboxProcessor<Message>) = 
        let rec loop stopOp =
            async {
                let! message = mailbox.Receive ()
                match message with
                | Start (run, channel) ->
                    let wait, stop = run ()
                    channel.Reply wait
                    return! loop (Some stop)
                | Stop channel ->
                    stopOp |> Option.iter (fun stop -> stop.Run ())
                    channel.Reply () }
        loop None

    let run (config : Config) =
        let fire info =
            sendFireEmail config.Sender config.Subscribes info
            execFile info.Action

        let detector = ErrorDetection.Agent (config.StuckProfile, config.Profiles, fire)
        let monitor = MailboxProcessor.Start monitorBody

        let rec loop () =
            let run () = Miner.run config.NoDevFee config.MinerPath config.MinerArgs detector.Update

            let wait = monitor.PostAndReply (fun channel -> Start (run, channel))
            let beginTime = TimeMs.Now
            wait.Run ()

            let duration = TimeMs.Now - beginTime
            let isQuick = duration < config.QuickExitProfile.Tolerance
            fire {  Reason = if isQuick then "Exit too quickly" else "Exit"
                    UpTime = duration
                    Action = if isQuick then Some config.QuickExitProfile.Action else None
                    Log = detector.GetLog () }
            
            detector.Reset ()
            loop ()

        let thread = Thread (ThreadStart loop)
        thread.Start ()

        let wait = fun () -> thread.Join ()
        let stop = fun () -> monitor.PostAndReply Stop; detector.Stop ()
        WaitFun wait, StopFun stop
