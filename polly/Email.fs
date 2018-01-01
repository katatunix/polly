﻿namespace polly

open System
open System.Net
open System.Net.Mail

open Config

module Email =

    let private subject = "Notification - " + Environment.MachineName

    let private send (sender : Sender) (toAddresses : string []) subject body =
        use message = new MailMessage ()
        message.From <- MailAddress (sender.Address, sender.DisplayedName)
        for toAddress in toAddresses do
            message.To.Add (toAddress)
        message.Subject <- subject
        message.Body <- body

        use smtp =
            new SmtpClient (
                Host = sender.SmtpHost,
                Port = sender.SmtpPort,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = NetworkCredential (sender.Address, sender.Password) )

        smtp.Send message

    let private makeUpTimeText upTimeMs =
        let ts = TimeSpan.TicksPerMillisecond * upTimeMs |> TimeSpan
        sprintf "[Up time] %s" (ts.ToString @"dd\.hh\:mm\:ss")

    let sendFire senderInfo toAddresses reason upTimeMs action log =
        let title = "FIRE!"
        let reason = sprintf "[Reason] %s" reason
        let upTime = makeUpTimeText upTimeMs
        let action = sprintf "[Action] %s" (action |> Option.defaultValue "<None>")
        let log = sprintf "[Log] %s" (log |> Option.defaultValue "<None>")
        let body = sprintf "%s\n%s\n%s\n%s\n%s" title reason upTime action log
        send senderInfo toAddresses subject body

    let sendPublicIp senderInfo toAddresses ip =
        let body = "New public IP address: " + ip
        send senderInfo toAddresses subject body
