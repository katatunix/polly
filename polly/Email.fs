namespace polly

open System
open System.Net
open System.Net.Mail

open Config

module Email =

    let private subject = "Notification - " + Environment.MachineName

    let private send sender (recipients : string []) subject body =
        use message = new MailMessage ()
        message.From <- MailAddress (sender.Address, sender.DisplayedName)
        for toAddress in recipients do
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

    let makeUpTimeText upTimeMs =
        let ts = TimeSpan.TicksPerMillisecond * upTimeMs |> TimeSpan
        sprintf "[Uptime] %s" (ts.ToString @"d\d\ hh\:mm")

    let sendFire senderInfo toAddresses reason upTimeMs action log =
        let title = "FIRE!"
        let reason = sprintf "[Reason] %s" reason
        let upTime = makeUpTimeText upTimeMs
        let action = sprintf "[Action] %s" (action |> Option.defaultValue "<None>")
        let log = log |> List.rev |> String.concat "\n"

        let body = sprintf "%s\n%s\n%s\n%s\n\n%s" title reason upTime action log
        send senderInfo toAddresses subject body

    let sendPublicIp sender recipients ip =
        let body = "New public IP address: " + ip
        send sender recipients subject body
