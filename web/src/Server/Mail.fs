/// Ref: https://mailtrap.io/blog/asp-net-core-send-email/
module Mail

open MimeKit
open MailKit.Net.Smtp

type MailSettings() =
    static member SettingName = "MailSettings"
    member val Server: string = "" with get, set
    member val Port: int = 0 with get, set
    member val SenderName: string = "" with get, set
    member val SenderEmail: string = "" with get, set
    member val UserName: string = "" with get, set
    member val Password: string = "" with get, set


type MailRecipient =
    { EmailToId: string
      EmailToName: string }

type MailData =
    { EmailRecipient: MailRecipient
      EmailSubject: string
      EmailTextBody: string
      EmailHtmlBody: string }

type IMailService =
    abstract member SendMail: mailData: MailData -> unit

type MailService(mailSettings: MailSettings) =

    interface IMailService with
        member __.SendMail(mailData: MailData) =
            use emailMessage = new MimeMessage()
            let emailFrom = MailboxAddress(mailSettings.SenderName, mailSettings.SenderEmail)
            emailMessage.From.Add(emailFrom)

            let emailTo =
                MailboxAddress(mailData.EmailRecipient.EmailToName, mailData.EmailRecipient.EmailToId)

            emailMessage.To.Add(emailTo)

            emailMessage.Subject <- mailData.EmailSubject
            let emailBodyBuilder = BodyBuilder()
            emailBodyBuilder.HtmlBody <- mailData.EmailHtmlBody
            emailBodyBuilder.TextBody <- mailData.EmailTextBody

            emailMessage.Body <- emailBodyBuilder.ToMessageBody()

            use mailClient = new SmtpClient()

            mailClient.Connect(mailSettings.Server, mailSettings.Port, MailKit.Security.SecureSocketOptions.StartTls)

            mailClient.Authenticate(mailSettings.UserName, mailSettings.Password)
            mailClient.Send(emailMessage) |> ignore
            mailClient.Disconnect(true)
