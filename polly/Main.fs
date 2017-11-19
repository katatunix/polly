namespace polly

open System
open System.IO
open System.Text.RegularExpressions
open System.Diagnostics
open System.Timers
open FSharp.Data
open NghiaBui.Common.ActivePatterns

module Main =

    let fetchHtml port =
        let url = sprintf "http://localhost:%d" port
        try
            Http.RequestString url |> Some
        with _ ->
            None

    let clean (html : string) =
        let regex = Regex """<font color=\"#.{6}\">"""
        regex
            .Replace(html, "")
            .Replace("\r\n", "")
            .Replace("\n", "")
            .Replace("<br>", "\n")
            .Replace("""<html><body bgcolor="#000000" style="font-family: monospace;">""", "")
            .Replace("</font>", "")
            .Replace("</body></html>", "")

    let loadErrorIndicators () =
        File.ReadAllLines "config.txt"
        |> List.ofArray
        |> List.filter (fun line ->
            let trimmed = line.Trim ()
            trimmed.Length > 0 && not (trimmed.StartsWith "#"))

    type Error = {
        Reason : string
        Log : string option }

    let checkHtml (errorIndicators : string list) (htmlOp : string option) =
        match htmlOp with
        | None ->
            Some { Reason = "No answer from miner"; Log = None }
        | Some html ->
            errorIndicators
            |> List.tryFind (fun indicator -> html.IndexOf indicator > -1)
            |> Option.map (fun indicator -> { Reason = indicator; Log = Some html })

    let checkMiner port errorIndicators =
        fetchHtml port
        |> Option.map clean
        |> checkHtml errorIndicators

    let sendPublicIpToEmails ip emails =
        for email in emails do
            printf "Send public IP address to %s ..." email
            Email.sendPublicIp email ip
            printfn "[OK]"

    let sendResetToEmails error emails =
        for email in emails do
             Email.sendReset email error.Reason error.Log

    let restart () = Process.Start ("shutdown", "/r /t 1") |> ignore

    let start port minutes (emails : string []) =
        let ip = PublicIp.get ()
        printfn "Public IP = %s" ip
        if ip <> PublicIp.load () then
            try sendPublicIpToEmails ip emails with _ -> ()
            PublicIp.save ip

        use timer = new Timer (minutes * 60 * 1000 |> float)
        timer.Elapsed.Add (fun _ ->
            let errorIndicators = loadErrorIndicators ()
            match checkMiner port errorIndicators with
            | None ->
                printfn "[%A] OK" DateTime.Now
            | Some error ->
                try sendResetToEmails error emails with _ -> ()
                restart ())

        timer.Start ()

        let rec loop () =
            let key = Console.ReadKey(true).Key
            if key <> ConsoleKey.Escape then loop ()
        loop ()

        timer.Stop ()

    [<EntryPoint>]
    let main argv =
        //Process.run argv.[0] argv.[1]
        match argv with
        | [| Int port; Int minutes |] ->
            start port minutes [||]
            0
        | [| Int port; Int minutes; emails |] ->
            start port minutes (emails.Split ',')
            0
        | _ ->
            printfn "Usage: polly.exe port minutes [email1,email2,...]"
            1
