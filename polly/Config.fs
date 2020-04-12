module polly.Config

open System
open System.IO
open FSharp.Configuration

type Sender =
    { SmtpHost: string
      SmtpPort: int
      Address: string
      Password: string
      DisplayedName: string }

let private INDI_SEP = "___"

let private INDI_SEPS = [|INDI_SEP|]

type Indicator =
    { Contain: string
      NotContains: string [] }
    with
    member this.RawText =
        if this.NotContains.Length = 0
        then this.Contain
        else this.Contain + INDI_SEP + (this.NotContains |> String.concat INDI_SEP)

type Tolerance =
    { Duration: TimeMs
      Good: Indicator [] }

type Profile =
    { Bad: Indicator []
      Tolerance: Tolerance option
      Action: string option }

type SpecialProfile =
    { Tolerance: TimeMs
      Action: string option }

type Config =
    { MinerPath: string
      MinerArgs: string
      NoDevFee: bool
      Sender: Sender
      Subscribes: string array
      Profiles: Profile []
      StuckProfile: SpecialProfile
      QuickExitProfile: SpecialProfile
      PublicIpCheck: TimeMs
      MaxLogLines: int }

let private DEFAULT_CONFIG_FILE =
    Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "config.yml")

type YmlConfig = YamlConfig<"config.sample.yml">

let private parseIndicator (indicator: string) =
    let keys = indicator.Split (INDI_SEPS, StringSplitOptions.RemoveEmptyEntries)
    if keys.Length = 0 then
        { Contain = ""; NotContains = [| |] }
    else
        { Contain = keys.[0]; NotContains = keys |> Array.tail }

let private parseTolerance (tole: YmlConfig.Profiles_Item_Type.Tolerance_Type) =
    if tole.DurationMinutes > 0 then
        Some { Duration = tole.DurationMinutes |> fromMinutes
               Good = tole.Good |> Seq.map parseIndicator |> Seq.toArray }
    else None

let private parseProfiles (yml: YmlConfig) =
    yml.Profiles
    |> Seq.map (fun p -> { Bad       = p.Bad |> Seq.map parseIndicator |> Seq.toArray
                           Tolerance = p.Tolerance |> parseTolerance
                           Action    = p.Action |> string2opt })
    |> Seq.toArray

let private parseConfig (yml: YmlConfig) =
    { MinerPath = yml.MinerPath
      MinerArgs = yml.MinerArgs
      NoDevFee = yml.NoDevFee
      Sender = { SmtpHost = yml.Sender.SmtpHost
                 SmtpPort = yml.Sender.SmtpPort
                 Address = yml.Sender.Address
                 Password = yml.Sender.Password
                 DisplayedName = yml.Sender.DisplayedName }
      Subscribes = yml.Subscribes |> Seq.toArray
      Profiles = parseProfiles yml
      StuckProfile =
          { Tolerance = yml.StuckProfile.ToleranceMinutes |> fromMinutes
            Action = yml.StuckProfile.Action |> string2opt }
      QuickExitProfile =
          { Tolerance = yml.QuickExitProfile.ToleranceMinutes |> fromMinutes
            Action = yml.QuickExitProfile.Action |> string2opt }
      PublicIpCheck = yml.PublicIpCheckMinutes |> fromMinutes
      MaxLogLines = yml.MaxLogLines }

let load configFile =
    try
        let yml = YmlConfig()
        configFile
        |> Option.defaultValue DEFAULT_CONFIG_FILE
        |> (fun file -> yml.Load file; yml)
        |> parseConfig
        |> Ok
    with ex ->
        ex.Message |> Error
