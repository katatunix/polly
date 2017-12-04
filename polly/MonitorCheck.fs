namespace polly

open MonitorCommon

module MonitorCheck =

    type private State =
        | Idle
        | Active of (Error * TimeMs)
        | Over of Error

    let private updateState (TimeMs curTime) (line : string) profile state =
        match state with
        | Idle ->
            match profile.Bad |> Array.tryFind line.Contains with
            | Some reason ->
                let error = { Reason = reason; Log = line }
                match profile.Tolerance with
                | None ->
                    Over error
                | _ ->
                    Active (error, (TimeMs curTime))
            | None ->
                state
        | Active (error, (TimeMs oldTime)) ->
            let { Duration = TimeMs duration; Good = good } = profile.Tolerance.Value
            if curTime - oldTime > duration then
                Over error
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

    type Agent (profiles, fire) =

        let update states line =
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
                    let! msg = mailbox.TryReceive (60000)
                    match msg with
                    | None ->
                        let states' = update states ""
                        return! loop states'
                    | Some (Line line) ->
                        let states' = update states line
                        return! loop states'
                    | Some Reset ->
                        return! loop initialStates
                    | Some (Stop channel) ->
                        channel.Reply ()
                        return () }
            loop initialStates)

        member this.Update line =
            mailbox.Post (Line line)

        member this.Reset () =
            mailbox.Post Reset

        member this.Stop () =
            mailbox.PostAndReply Stop
