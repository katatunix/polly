namespace polly

open FSharp.Data
open System.Text.RegularExpressions

module Miner =

    type Error = {
        Reason : string
        Log : string option }

    let private fetchHtml port =
        let url = sprintf "http://localhost:%d" port
        try
            Http.RequestString url |> Some
        with _ ->
            None

    let private cleanHtml (html : string) =
        let regex = Regex """<font color=\"#.{6}\">"""
        regex
            .Replace(html, "")
            .Replace("\r\n", "")
            .Replace("\n", "")
            .Replace("<br>", "\n")
            .Replace("""<html><body bgcolor="#000000" style="font-family: monospace;">""", "")
            .Replace("</font>", "")
            .Replace("</body></html>", "")

    let private checkHtml (errorIndicators : string list) (htmlOp : string option) =
        match htmlOp with
        | None ->
            Some { Reason = "No answer from miner"; Log = None }
        | Some html ->
            errorIndicators
            |> List.tryFind (fun indicator -> html.IndexOf indicator > -1)
            |> Option.map (fun indicator -> { Reason = indicator; Log = Some html })

    let check port errorIndicators =
        fetchHtml port
        |> Option.map cleanHtml
        |> checkHtml errorIndicators
