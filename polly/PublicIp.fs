namespace polly

open System.IO
open System.Timers
open FSharp.Data
open NghiaBui.Common.Misc

module PublicIp =

    let private IP_FILE = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "ip.dat")

    let private get () =
        tryHard 3 1000 (fun _ -> Http.RequestString "http://api.ipify.org/")

    let private load () =
        try File.ReadAllText(IP_FILE) with _ -> ""

    let private save (ip : string) =
        try File.WriteAllText(IP_FILE, ip) with _ -> ()

    let private sendPublicIp out senderInfo emails ip =
        try
            for email in emails do
                out (sprintf "Send public IP to %s ..." email)
                Email.sendPublicIp senderInfo email ip
        with _ -> ()

    let private checkIp out senderInfo subscribedEmails =
        out "Get public IP ..."
        match get () with
        | Error msg ->
            out (sprintf "Could not get public IP: %s" msg)
        | Ok ip ->
            out (sprintf "Public IP = %s" ip)
            if ip <> load () then
                sendPublicIp out senderInfo subscribedEmails ip
                save ip

    let startCheck out config =
        let senderInfo = Config.extractSenderInfo config
        let subscribedEmails = config.SubscribedEmails
        checkIp out senderInfo subscribedEmails

        let timer = new Timer(10 * 60 * 1000 |> float)
        timer.Elapsed.Add (fun _ -> checkIp out senderInfo subscribedEmails)
        timer.Start ()
        timer
