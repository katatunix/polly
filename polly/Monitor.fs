namespace polly

open System
open System.IO
open System.Threading
open Config
open ErrorDetection

module Monitor =

    let private sendFireEmail sender recipients info  =
        Email.sendFire sender recipients info.Reason info.UpTime.Value info.Action info.Log

    let private executeAction action =
        action
        |> Option.iter (fun action ->
            let path = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, action)
            System.Diagnostics.Process.Start path |> ignore)

    let private fire config info =
        try
            sendFireEmail config.Sender config.Subscribes info
            executeAction info.Action
        with _ -> ()

    type private Message =
        | Start
        | Exit of TimeMs
        | Stop of AsyncReplyChannel<unit>

    type Agent (config) =

        let detectionAgent = ErrorDetection.Agent (config.StuckProfile, config.Profiles, fire config)
        let waitHandle = new AutoResetEvent false

        let mailbox = MailboxProcessor.Start (fun mailbox ->
            let rec loop currentStop = async {
                let! message = mailbox.Receive ()
                match message with
                | Start ->
                    let stop = MinerExecution.start "bootstrap.exe" config.MinerPath config.MinerArgs
                                                        config.NoDevFee detectionAgent.Update
                                                        (Exit >> mailbox.Post)
                    return! loop (Some stop)
                | Exit duration ->
                    let isQuick = duration < config.QuickExitProfile.Tolerance
                    fire config {   Reason = if isQuick then "Exit too quickly" else "Exit"
                                    UpTime = duration
                                    Action = if isQuick then Some config.QuickExitProfile.Action else None
                                    Log = detectionAgent.GetLog () }
                    detectionAgent.Reset ()
                    mailbox.Post Start
                    return! loop None
                | Stop channel ->
                    currentStop |> Option.iter (fun stop -> stop.Execute ())
                    channel.Reply ()
                    waitHandle.Set () |> ignore }
            loop None)

        do mailbox.Post Start

        member this.Join () = waitHandle.WaitOne () |> ignore

        interface IDisposable with
            member this.Dispose () =
                mailbox.PostAndReply Stop
