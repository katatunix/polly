namespace polly

open System
open NghiaBui.Common.Time
open MonitorCommon

module MonitorCheck =

    type private State =
        | Idle
        | Active of (Error * TimeMs)
        | Over of Error

    let private updateState (TimeMs beginTime) (TimeMs curTime) (line : string option) profile state =
        match state with
        | Idle ->
            match line with
            | None -> state
            | Some line ->
                match profile.Bad |> Array.tryFind line.Contains with
                | None -> state
                | Some reason ->
                    let error = { Reason = reason; UpTime = curTime - beginTime |> TimeMs; Log = line }
                    match profile.Tolerance with
                    | None ->
                        Over error
                    | _ ->
                        Active (error, (TimeMs curTime))
        | Active (error, (TimeMs oldTime)) ->
            let { Duration = TimeMs duration; Good = good } = profile.Tolerance.Value
            if curTime - oldTime > duration then
                Over error
            elif line.IsSome && good |> Array.exists line.Value.Contains then
                Idle
            else
                state
        | _ ->
            state

    type private Message =
        | Line of string
        | Reset
        | Stop of AsyncReplyChannel<unit>

    type Agent (profiles, fire) =

        let cur () = currentUnixTimeMs () |> TimeMs

        let update beginTime line states =
            let curTime = cur ()
            let states' = Array.map2 (updateState beginTime curTime line) profiles states
            states'
            |> Array.tryPick (function | Over error -> Some error | _ -> None)
            |> Option.iter fire
            states'

        let initialStates = Array.replicate (profiles |> Array.length) Idle

        let mailbox = MailboxProcessor.Start (fun mailbox ->
            let rec loop beginTime states =
                async {
                    let! msg = mailbox.TryReceive 60000
                    match msg with
                    | None ->
                        let states' = states |> update beginTime None
                        return! loop beginTime states'

                    | Some (Line line) ->
                        let states' = states |> update beginTime (Some line)
                        return! loop beginTime states'

                    | Some Reset ->
                        return! loop (cur ()) initialStates

                    | Some (Stop channel) ->
                        channel.Reply ()
                        return () }

            loop (cur ()) initialStates)

        member this.Update line = mailbox.Post (Line line)
        member this.Reset () = mailbox.Post Reset
        member this.Stop () = mailbox.PostAndReply Stop
