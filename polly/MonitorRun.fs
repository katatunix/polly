namespace polly

open System
open System.IO
open System.Threading
open NghiaBui.Common.Text
open MonitorCommon
open Out

module MonitorRun =

    let private sendFireEmail senderInfo toAddresses (error : FireInfo)  =
        let (TimeMs upTimeMs) = error.UpTime
        try
            Email.sendFire senderInfo toAddresses error.Reason upTimeMs error.Action error.Log
        with _ -> ()

    let private execFile =
        Option.iter (fun file ->
            let path = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, file)
            System.Diagnostics.Process.Start path |> ignore)

    type private SimpleChannel = AsyncReplyChannel<unit>

    type private Message =
        | Start of (unit -> unit) * (unit -> unit) * SimpleChannel
        | Stop of SimpleChannel

    let private agentForeverBody (mailbox : MailboxProcessor<Message>) = 
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
        loop None

    let forever config =
        let agentForever = MailboxProcessor.Start agentForeverBody

        let senderInfo = Config.extractSenderInfo config

        let fire error =
            printSpecial (sprintf "FIRE! REASON: %s! ACTION: %s"
                                    error.Reason (error.Action |> Option.defaultValue "<None>"))
            sendFireEmail senderInfo config.Subscribes error
            execFile error.Action
        
        let agentCheck = MonitorCheck.Agent (extractStuckProfile config, extractProfiles config, fire)

        let quickExitProfile = extractQuickExitProfile config

        let rec loop beginTime =
            let start, wait, stop =
                Process.create
                    (Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "winpty.exe"))
                    (sprintf "-Xallow-non-tty -Xcolor -Xplain \"%s\" %s" config.MinerPath config.MinerArgs)
                    (fun line ->
                        println line
                        line |> cleanAnsiEscapeCode |> agentCheck.Update)
            agentForever.PostAndReply (fun channel -> Start (start, stop, channel))
            wait ()

            let now = curTime ()
            let duration = now - beginTime
            if duration < quickExitProfile.Tolerance then
                fire {  Reason = "Exit too quickly"
                        UpTime = duration
                        Action = Some quickExitProfile.Action
                        Log = None }
            else
                fire {  Reason = "Exit"
                        UpTime = duration
                        Action = None
                        Log = None }
                agentCheck.Reset ()
                loop now

        let thread = Thread (ThreadStart (fun _ -> curTime () |> loop))
        thread.Start ()

        let wait = fun () -> thread.Join ()
        let stop = fun () -> agentForever.PostAndReply Stop; agentCheck.Stop ()
        wait, stop
