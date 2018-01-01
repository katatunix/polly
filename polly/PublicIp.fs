namespace polly

open System
open System.IO
open System.Net
open System.Timers

open Config

module PublicIp =

    let private IP_FILE = Path.Combine (System.AppDomain.CurrentDomain.BaseDirectory, "ip.dat")

    let private get () =
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

    let private sendPublicIp senderInfo emails ip =
        try
            Out.println "Send public IP ..."
            Email.sendPublicIp senderInfo emails ip
        with _ -> ()

    let private checkIp senderInfo emails =
        Out.println "Get public IP ..."
        match get () with
        | Error msg ->
            Out.println (sprintf "Could not get public IP: %s" msg)
        | Ok ip ->
            Out.println (sprintf "Public IP = %s" ip)
            if ip <> load () then
                sendPublicIp senderInfo emails ip
                save ip

    let startCheck (config : Config) =
        let check () = checkIp config.Sender config.Subscribes
        check ()

        let timer = new Timer (config.PublicIpCheck.Value |> float)
        timer.Elapsed.Add (fun _ -> check ())
        timer.Start ()
        timer
