module polly.Monitor

open System
open System.IO
open System.Threading
open Config
open Detector

let private sendFireEmail sender recipients info =
    let log = info.Log |> List.rev
    Email.sendFire sender recipients info.Reason info.UpTime.Value info.Action log

let private executeAction action =
    action
    |> Option.iter (fun action ->
        let path = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, action)
        Diagnostics.Process.Start path |> ignore
    )

let private fire config info =
    try
        sendFireEmail config.Sender config.Subscribes info
    with ex ->
        printfn "Could not send email: %s" ex.Message
    try
        executeAction info.Action
    with ex ->
        printfn "Could not execute action: %s" ex.Message

type private Message =
    | Start
    | Exit of TimeMs
    | Stop of AsyncReplyChannel<unit>

type Agent (config) =

    let detector = Detector.Agent (config.StuckProfile, config.Profiles, config.MaxLogLines, fire config)
    let waitHandle = new AutoResetEvent false

    let mailbox = MailboxProcessor.Start (fun mailbox ->
        let rec loop currentStop = async {
            let! message = mailbox.Receive ()
            match message with
            | Start ->
                let stop = Miner.start  "bootstrap.exe"
                                        config.MinerPath
                                        config.MinerArgs
                                        config.NoDevFee detector.Update
                                        (Exit >> mailbox.Post)
                return! loop (Some stop)
            | Exit duration ->
                let isQuick = duration < config.QuickExitProfile.Tolerance
                fire config {   Reason = if isQuick then "Exit too quickly" else "Exit"
                                UpTime = duration
                                Action = if isQuick then config.QuickExitProfile.Action else None
                                Log = detector.GetLog () }
                detector.Reset ()
                mailbox.Post Start
                return! loop None
            | Stop channel ->
                currentStop |> Option.iter (fun stop -> stop.Execute ())
                channel.Reply ()
                waitHandle.Set () |> ignore
        }
        loop None
    )

    do mailbox.Post Start

    member this.Join () = waitHandle.WaitOne () |> ignore

    interface IDisposable with
        member this.Dispose () =
            mailbox.PostAndReply Stop
