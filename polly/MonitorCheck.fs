namespace polly

open System
open MonitorCommon

module MonitorCheck =

    type private State =
        | Idle
        | Active of (FireInfo * TimeMs)
        | Over of FireInfo

    let private updateState beginTime curTime (line : string) profile state =
        match state with
        | Idle ->
            match profile.Bad |> Array.tryFind line.Contains with
            | None -> state
            | Some reason ->
                let error = { Reason = reason; UpTime = curTime - beginTime; Action = profile.Action; Log = Some line }
                match profile.Tolerance with
                | None ->
                    Over error
                | _ ->
                    Active (error, curTime)

        | Active (error, oldTime) ->
            let { Duration = duration; Good = good } = profile.Tolerance.Value
            if curTime - oldTime > duration then
                Over { error with UpTime = curTime - beginTime }
            elif good |> Array.exists line.Contains then
                Idle
            else
                state

        | _ ->
            state

    type private Message =
        | Line of string
        | Reset
        | Stop of AsyncReplyChannel<unit>

    type Agent (stuckProfile : SpecialProfile, profiles, fire) =

        let updateWithNewLine beginTime line states =
            let states' = Array.map2 (updateState beginTime (curTime ()) line) profiles states
            states'
            |> Array.tryPick (function | Over error -> Some error | _ -> None)
            |> Option.iter fire
            states'

        let fireBecauseStuck beginTime =
            fire {  Reason = "Stuck"
                    UpTime = curTime () - beginTime
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
                        return! loop (curTime ()) initialStates

                    | Some (Stop channel) ->
                        channel.Reply ()
                        return () }

            loop (curTime ()) initialStates)

        member this.Update line = mailbox.Post (Line line)
        member this.Reset () = mailbox.Post Reset
        member this.Stop () = mailbox.PostAndReply Stop
