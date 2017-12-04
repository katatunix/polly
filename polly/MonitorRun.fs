﻿namespace polly

open System
open System.IO
open System.Threading
open NghiaBui.Common.Text
open NghiaBui.Common.Time
open MonitorCommon
open Out

module MonitorRun =

    let private sendFireEmail senderInfo toAddresses (error : Error)  =
        try
            Email.sendFire senderInfo toAddresses error.Reason error.Log
        with _ -> ()

    let private sendExitEmail senderInfo toAddresses upTimeMs =
        try
            Email.sendExit senderInfo toAddresses upTimeMs
        with _ -> ()

    let private fire () =
        let path = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "fire.bat")
        System.Diagnostics.Process.Start path |> ignore

    type private SimpleChannel = AsyncReplyChannel<unit>

    type private Message =
        | Start of (unit -> unit) * (unit -> unit) * SimpleChannel
        | Stop of SimpleChannel

    let forever config =
        let senderInfo = Config.extractSenderInfo config

        let agentForever = MailboxProcessor.Start (fun mailbox ->
            let rec loop stopOp =
                async {
                    let! message = mailbox.Receive ()
                    match message with
                    | Start (start, stop, channel) ->
                        start ()
                        channel.Reply ()
                        return! loop (Some stop)
                    | Stop channel ->
                        stopOp |> Option.iter (fun stop -> stop ())
                        channel.Reply () }
            loop None)

        let fireWith error =
            sendFireEmail senderInfo config.Subscribes error
            fire ()

        let agentCheck = MonitorCheck.Agent (extractProfiles config, fireWith)

        let rec loop timeMs =
            let start, wait, stop =
                Process.create
                    (Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "winpty.exe"))
                    (sprintf "-Xallow-non-tty -Xcolor -Xplain \"%s\" %s" config.MinerPath config.MinerArgs)
                    (fun line ->
                        println line
                        line |> cleanAnsiEscapeCode |> agentCheck.Update)
            agentForever.PostAndReply (fun channel -> Start (start, stop, channel))
            wait ()

            let curTimeMs = currentUnixTimeMs ()
            let upTimeMs = curTimeMs - timeMs
            if upTimeMs < (int64 config.ExitToleranceMinutes) * 60000L then
                printSpecial "THE MINER HAS BEEN EXITED TOO QUICKLY, FIRE!"
                fireWith { Reason = "Exit too quickly"; Log = "<No log>" }
            else
                printSpecial "THE MINER HAS BEEN EXITED, NOW START IT AGAIN"
                sendExitEmail senderInfo config.Subscribes upTimeMs
                agentCheck.Reset ()
                loop curTimeMs

        let thread = Thread (ThreadStart (fun _ -> currentUnixTimeMs () |> loop))
        thread.Start ()

        let wait = fun () -> thread.Join ()
        let stop = fun () -> agentForever.PostAndReply Stop; agentCheck.Stop ()
        wait, stop