namespace polly

open System
open System.IO
open System.Diagnostics
open System.Timers
open NghiaBui.Common.ActivePatterns

module Main =

    let loadErrorIndicators () =
        File.ReadAllLines "config.txt"
        |> List.ofArray
        |> List.filter (fun line ->
            let trimmed = line.Trim ()
            trimmed.Length > 0 && not (trimmed.StartsWith "#"))

    let sendPublicIpToEmails ip emails =
        for email in emails do
            printf "Send public IP address to %s ... " email
            Email.sendPublicIp email ip
            printfn "[OK]"

    let sendResetToEmails (error : Miner.Error) emails =
        for email in emails do
             Email.sendReset email error.Reason error.Log

    let resetComputer () =
        Process.Start ("shutdown", "/r /t 1") |> ignore

    let start port minutes (emails : string []) =
        let ip = PublicIp.get ()
        printfn "Public IP = %s" ip
        if ip <> PublicIp.load () then
            try sendPublicIpToEmails ip emails with _ -> ()
            PublicIp.save ip

        use timer = new Timer (minutes * 60 * 1000 |> float)
        timer.Elapsed.Add (fun _ ->
            let errorIndicators = loadErrorIndicators ()
            match Miner.check port errorIndicators with
            | None ->
                printfn "[%A] OK" DateTime.Now
            | Some error ->
                try sendResetToEmails error emails with _ -> ()
                resetComputer ())

        timer.Start ()

        let rec loop () =
            let key = Console.ReadKey(true).Key
            if key <> ConsoleKey.Escape then loop ()
        loop ()

        timer.Stop ()

    [<EntryPoint>]
    let main argv =
        match argv with
        | [| Int port; Int minutes |] ->
            start port minutes [||]
            0
        | [| Int port; Int minutes; emails |] ->
            start port minutes (emails.Split ',')
            0
        | _ ->
            printfn "Usage: polly.exe <port> <minutes> [<email1>,<email2>,...]"
            1
