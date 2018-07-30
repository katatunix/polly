namespace polly

open System
open System.IO
open System.Net
open System.Timers

open Config

module PublicIp =

    let private IP_FILE = Path.Combine (System.AppDomain.CurrentDomain.BaseDirectory, "ip.dat")

    let private fetch () =
        use wc = new WebClient ()
        try
            wc.DownloadString (Uri "http://api.ipify.org/")
            |> Ok
        with ex ->
            Error ex.Message

    let private load () =
        try File.ReadAllText(IP_FILE) with _ -> ""

    let private save (ip : string) =
        try File.WriteAllText(IP_FILE, ip) with _ -> ()

    let private check sender recipients =
        match fetch () with
        | Error _ ->
            ()
        | Ok ip ->
            if ip <> load () then
                try Email.sendPublicIp sender recipients ip with _ -> ()
                save ip

    let start (config : Config) =
        let check () = check config.Sender config.Subscribes
        check ()

        let interval = config.PublicIpCheck.Value |> float
        let timer = new Timer (interval)
        timer.Elapsed.Add (fun _ -> check ())
        timer.Start ()

        { new IDisposable with
            member this.Dispose () = timer.Stop () }
