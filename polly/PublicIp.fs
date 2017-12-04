namespace polly

open System.IO
open System.Timers
open FSharp.Data
open NghiaBui.Common.Misc
open Out

module PublicIp =

    let private IP_FILE = Path.Combine (System.AppDomain.CurrentDomain.BaseDirectory, "ip.dat")

    let private get () =
        tryHard 3 1000 (fun _ -> Http.RequestString "http://api.ipify.org/")

    let private load () =
        try File.ReadAllText(IP_FILE) with _ -> ""

    let private save (ip : string) =
        try File.WriteAllText(IP_FILE, ip) with _ -> ()

    let private sendPublicIp senderInfo emails ip =
        try
            println "Send public IP ..."
            Email.sendPublicIp senderInfo emails ip
        with _ -> ()

    let private checkIp senderInfo emails =
        println "Get public IP ..."
        match get () with
        | Error msg ->
            println (sprintf "Could not get public IP: %s" msg)
        | Ok ip ->
            println (sprintf "Public IP = %s" ip)
            if ip <> load () then
                sendPublicIp senderInfo emails ip
                save ip

    let startCheck config =
        let senderInfo = Config.extractSenderInfo config
        checkIp senderInfo config.Subscribes

        let timer = new Timer(config.PublicIpCheckMinutes * 60000 |> float)
        timer.Elapsed.Add (fun _ -> checkIp senderInfo config.Subscribes)
        timer.Start ()
        timer
