namespace polly

open System
open System.IO
open System.Threading

open NghiaBui.Common.Text

open Config
open ErrorDetection

module Monitor =

    let private sendFireEmail sender toAddresses (fi : FireInfo)  =
        try
            Email.sendFire sender toAddresses fi.Reason fi.UpTime.Value fi.Action fi.Log
        with _ -> ()

    let private execFile =
        Option.iter (fun file ->
            let path = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, file)
            System.Diagnostics.Process.Start path |> ignore)

    type private SimpleChannel = AsyncReplyChannel<unit>

    type private Message =
        | Start of (unit -> unit) * (unit -> unit) * SimpleChannel
        | Stop of SimpleChannel

    let private monitorBody (mailbox : MailboxProcessor<Message>) = 
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

    let run (config : Config) =
        let fire info =
            Out.printSpecial (sprintf "FIRE! REASON: %s! ACTION: %s"
                                info.Reason (info.Action |> Option.defaultValue "<None>"))
            sendFireEmail config.Sender config.Subscribes info
            execFile info.Action
        let detector = ErrorDetection.Agent (config.StuckProfile, config.Profiles, fire)
        let monitor = MailboxProcessor.Start monitorBody

        let rec loop beginTime =
            let start, wait, stop =
                Process.create
                    (Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "winpty.exe"))
                    (sprintf "-Xallow-non-tty -Xcolor -Xplain \"%s\" %s" config.MinerPath config.MinerArgs)
                    (fun line ->
                        Out.println line
                        line |> cleanAnsiEscapeCode |> detector.Update)
            monitor.PostAndReply (fun channel -> Start (start, stop, channel))
            wait ()

            let now = TimeMs.Now
            let duration = now - beginTime
            if duration < config.QuickExitProfile.Tolerance then
                fire {  Reason = "Exit too quickly"
                        UpTime = duration
                        Action = Some config.QuickExitProfile.Action
                        Log = None }
            else
                fire {  Reason = "Exit"
                        UpTime = duration
                        Action = None
                        Log = None }
                detector.Reset ()
                loop now

        let thread = Thread (ThreadStart (fun _ -> loop TimeMs.Now))
        thread.Start ()

        let wait = fun () -> thread.Join ()
        let stop = fun () -> monitor.PostAndReply Stop; detector.Stop ()
        wait, stop
