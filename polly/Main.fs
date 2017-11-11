namespace polly

open System
open System.IO
open System.Text.RegularExpressions
open System.Diagnostics
open System.Timers
open FSharp.Data
open NghiaBui.Common

module Main =

    let getHtml port =
        let url = sprintf "http://localhost:%d" port
        try
            Http.RequestString url |> Some
        with _ ->
            None

    let cleanHtml (html : string) =
        let regex = Regex """<font color=\"#.{6}\">"""
        regex
            .Replace(html, "")
            .Replace("\r\n", "")
            .Replace("\n", "")
            .Replace("<br>", "\n")
            .Replace("""<html><body bgcolor="#000000" style="font-family: monospace;">""", "")
            .Replace("</font>", "")
            .Replace("</body></html>", "")

    let readErrorIndicators () =
        File.ReadAllLines "config.txt"
        |> List.ofArray
        |> List.filter (fun line ->
            let trimmed = line.Trim ()
            trimmed.Length > 0 && not (trimmed.StartsWith "#"))

    type ErrorType =
        | NoAnswer
        | RunningError of string * string // indicator * html

    let checkHtml (errorIndicators : string list) (htmlOp : string option) =
        match htmlOp with
        | None ->
            Some NoAnswer
        | Some html ->
            errorIndicators
            |> List.tryFind (fun indicator -> html.IndexOf indicator > -1)
            |> Option.map (fun indicator -> RunningError (indicator, html))

    let checkMiner port errorIndicators =
        getHtml port
        |> Option.map cleanHtml
        |> checkHtml errorIndicators

    let restart () =
        Process.Start ("shutdown", "/r /t 1") |> ignore

    let makeErrorMessage = function
        | NoAnswer ->
            "No answer from miner", None
        | RunningError (indicator, html) ->
            indicator, Some html

    let start port minutes (emails : string []) =
        use timer = new Timer (minutes * 60 * 1000 |> float)
        timer.Elapsed.Add (fun _ ->
            let errorIndicators = readErrorIndicators ()
            match checkMiner port errorIndicators with
            | None ->
                printfn "[%A] OK" DateTime.Now
            | Some error ->
                for email in emails do
                    Email.send email (makeErrorMessage error)
                restart ())
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
            printfn "Usage: polly.exe port minutes [email1,email2,...]"
            1
