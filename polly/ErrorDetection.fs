﻿namespace polly

open System
open NghiaBui.Common.Misc

open Config

module ErrorDetection =

    type FireInfo = {
        Reason : string
        UpTime : TimeMs
        Action : string option
        Log : string option }

    type private State =
        | Idle
        | Active of (FireInfo * TimeMs)
        | Over of FireInfo

    let private updateState beginTime curTime (line : string) profile state =
        match state with
        | Idle ->
            match profile.Bad |> Array.tryFind line.Contains with
            | None ->
                state
            | Some reason ->
                let fireInfo = {    Reason = reason; UpTime = curTime - beginTime;
                                    Action = profile.Action; Log = Some line }
                match profile.Tolerance with
                | None ->
                    Over fireInfo
                | _ ->
                    Active (fireInfo, curTime)

        | Active (fireInfo, oldTime) ->
            let { Duration = duration; Good = good } = profile.Tolerance.Value
            if curTime - oldTime > duration then
                Over { fireInfo with UpTime = curTime - beginTime }
            elif good |> Array.exists line.Contains then
                Idle
            else
                state

        | Over _ ->
            failShouldNotGoHere ()

    type private Message =
        | Line of string
        | Reset
        | Stop of AsyncReplyChannel<unit>

    type Agent (stuckProfile : SpecialProfile, profiles, fire) =

        let updateWithNewLine beginTime line states =
            let states' = Array.map2 (updateState beginTime TimeMs.Now line) profiles states
            states' |> Array.tryPick (function | Over fireInfo -> Some fireInfo | _ -> None)
                    |> Option.iter fire
            states' |> Array.map (function | Over _ -> Idle | _ as state -> state)

        let fireBecauseStuck beginTime =
            fire {  Reason = "Stuck"
                    UpTime = TimeMs.Now - beginTime
                    Action = Some stuckProfile.Action
                    Log = None }

        let initialStates = Array.replicate (profiles |> Array.length) Idle

        let mailbox = MailboxProcessor.Start (fun mailbox ->
            let rec loop beginTime states =
                async {
                    let! msg = mailbox.TryReceive (int stuckProfile.Tolerance.Value)
                    match msg with
                    | None ->
                        fireBecauseStuck beginTime
                        return! loop beginTime states

                    | Some (Line line) ->
                        let states' = states |> updateWithNewLine beginTime line
                        return! loop beginTime states'

                    | Some Reset ->
                        return! loop TimeMs.Now initialStates

                    | Some (Stop channel) ->
                        channel.Reply () }

            loop TimeMs.Now initialStates)

        member this.Update line = mailbox.Post (Line line)
        member this.Reset () = mailbox.Post Reset
        member this.Stop () = mailbox.PostAndReply Stop