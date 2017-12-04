namespace polly

open MonitorCommon

module MonitorCheck =

    type private State =
        | Idle
        | Active of (Error * TimeMs)
        | Over of Error

    let private updateState (TimeMs curTime) (line : string option) profile state =
        match state with
        | Idle ->
            match line with
            | None -> state
            | Some line ->
                match profile.Bad |> Array.tryFind line.Contains with
                | None -> state
                | Some reason ->
                    let error = { Reason = reason; Log = line }
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
        let update line states =
            let curTime = NghiaBui.Common.Time.currentUnixTimeMs () |> TimeMs
            let states' = Array.map2 (updateState curTime line) profiles states
            states'
            |> Array.tryPick (function | Over error -> Some error | _ -> None)
            |> Option.iter fire
            states'

        let initialStates = Array.replicate (profiles |> Array.length) Idle

        let mailbox = MailboxProcessor.Start (fun mailbox ->
            let rec loop states =
                async {
                    let! msg = mailbox.TryReceive 60000
                    match msg with
                    | None ->
                        let states' = states |> update None
                        return! loop states'
                    | Some (Line line) ->
                        let states' = states |> update (Some line)
                        return! loop states'
                    | Some Reset ->
                        return! loop initialStates
                    | Some (Stop channel) ->
                        channel.Reply ()
                        return () }
            loop initialStates)

        member this.Update line = mailbox.Post (Line line)
        member this.Reset () = mailbox.Post Reset
        member this.Stop () = mailbox.PostAndReply Stop
