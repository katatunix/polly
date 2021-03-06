﻿module polly.Detector

open NghiaBui.Common.Misc
open Config

type FireInfo =
    { Reason: string
      UpTime: TimeMs
      Action: string option
      Log: string list }

let private insert2Log maxLines line log =
    let len = log |> List.length
    if len >= maxLines then
        line :: (log |> List.take (len - 1))
    else
        line :: log

type private ProfileState =
    | Idle
    | Active of (FireInfo * TimeMs)
    | Over of FireInfo

type private Data =
    { ProfileStates: ProfileState []
      Log: string list }

let private matched (line: string) (indicator: Indicator) =
    indicator.Contain.Length > 0 &&
    indicator.Contain |> line.Contains &&
    indicator.NotContains |> Array.forall (line.Contains >> not)

let private updateProfileState line beginTime curTime log profile = function
    | Idle ->
        match profile.Bad |> Array.tryFind (matched line) with
        | None ->
            Idle
        | Some indicator ->
            let fireInfo = { Reason = indicator.RawText
                             UpTime = curTime - beginTime
                             Action = profile.Action
                             Log = log }
            match profile.Tolerance with
            | None ->
                Over fireInfo
            | _ ->
                Active (fireInfo, curTime)

    | Active (fireInfo, oldTime) ->
        let { Duration = toleranceDuration; Good = goodIndicators } = profile.Tolerance.Value
        if curTime - oldTime > toleranceDuration then
            Over { fireInfo with UpTime = curTime - beginTime }
        elif goodIndicators |> Array.exists (matched line) then
            Idle
        else
            let fireInfo' = { fireInfo with Log = line :: fireInfo.Log }
            Active (fireInfo', oldTime)

    | Over _ ->
        failShouldNotGoHere ()

type private Message =
    | Line of string
    | Reset
    | GetLog of AsyncReplyChannel<string list>
    | Stop of AsyncReplyChannel<unit>

type Agent (stuckProfile: SpecialProfile, profiles, maxLines, onFire) =

    let updateData line beginTime data =
        let log = data.Log |> insert2Log maxLines line
        let profileStates = Array.map2 (updateProfileState line beginTime TimeMs.Now log) profiles data.ProfileStates

        profileStates
        |> Array.tryPick (function | Over fireInfo -> Some fireInfo | _ -> None)
        |> Option.iter onFire

        let profileStates =
            profileStates
            |> Array.map (function | Over _ -> Idle | _ as state -> state)

        { ProfileStates = profileStates
          Log = log }

    let fireBecauseStuck beginTime log =
        onFire { Reason = "Stuck"
                 UpTime = TimeMs.Now - beginTime
                 Action = stuckProfile.Action
                 Log = log }

    let initialData =
        { ProfileStates = Array.replicate (profiles |> Array.length) Idle
          Log = [] }

    let mailbox = MailboxProcessor.Start <| fun mailbox ->
        let rec loop beginTime data = async {
            let! msg = mailbox.TryReceive (int stuckProfile.Tolerance.Value)

            match msg with
            | None ->
                fireBecauseStuck beginTime data.Log
                return! loop beginTime data

            | Some (Line line) ->
                let data = data |> updateData line beginTime
                return! loop beginTime data

            | Some Reset ->
                return! loop TimeMs.Now initialData

            | Some (GetLog channel) ->
                channel.Reply data.Log
                return! loop beginTime data

            | Some (Stop channel) ->
                channel.Reply ()
        }
        loop TimeMs.Now initialData

    member this.Update line = mailbox.Post (Line line)
    member this.Reset () = mailbox.Post Reset
    member this.GetLog () = mailbox.PostAndReply GetLog
    member this.Stop () = mailbox.PostAndReply Stop
