module polly.Email

open System
open MimeKit
open MailKit.Net.Smtp

let private subject = "Notification - " + Environment.MachineName

let private send (sender: Config.Sender) recipients subject body =
    let message = MimeMessage ()
    message.From.Add (MailboxAddress (sender.DisplayedName, sender.Address))
    for toAddress: string in recipients do
        message.To.Add (MailboxAddress.Parse toAddress)
    message.Subject <- subject
    let tp = TextPart "plain"
    tp.Text <- body
    message.Body <- tp

    use client = new SmtpClient ()
    client.Connect (sender.SmtpHost, sender.SmtpPort, false)
    client.Authenticate (sender.Address, sender.Password)
    client.Send message
    client.Disconnect true

let private makeUpTimeText (upTimeMs: int64) =
    let ts = TimeSpan.FromMilliseconds (float upTimeMs)
    sprintf "[Uptime] %s" (ts.ToString @"d\d\ hh\:mm")

let sendFire senderInfo toAddresses reason upTimeMs action log =
    let title = "FIRE!"
    let reason = sprintf "[Reason] %s" reason
    let upTime = makeUpTimeText upTimeMs
    let action = sprintf "[Action] %s" (action |> Option.defaultValue "<None>")
    let log = log |> String.concat "\n"

    let body = sprintf "%s\n%s\n%s\n%s\n\n%s" title reason upTime action log
    send senderInfo toAddresses subject body

let sendPublicIp sender recipients ip =
    let body = "New public IP address: " + ip
    send sender recipients subject body
