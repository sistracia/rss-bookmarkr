/// Ref: https://stackoverflow.com/a/1248/12976234
/// And thanks to ChatGPT for convert the code for me :)
module Time

open System

let SECOND: int = 1
let MINUTE: int = 60 * SECOND
let HOUR: int = 60 * MINUTE
let DAY: int = 24 * HOUR
let MONTH: int = 30 * DAY

let getTimeAgo (inputDate: DateTime) : string =
    let ts: TimeSpan = TimeSpan(DateTime.UtcNow.Ticks - inputDate.Ticks)
    let delta: float = Math.Abs(ts.TotalSeconds)

    if delta < 1.0 * float MINUTE then
        if ts.Seconds = 1 then
            "one second ago"
        else
            sprintf "%d seconds ago" ts.Seconds
    elif delta < 2.0 * float MINUTE then
        "a minute ago"
    elif delta < 45.0 * float MINUTE then
        sprintf "%d minutes ago" ts.Minutes
    elif delta < 90.0 * float MINUTE then
        "an hour ago"
    elif delta < 24.0 * float HOUR then
        sprintf "%d hours ago" ts.Hours
    elif delta < 48.0 * float HOUR then
        "yesterday"
    elif delta < 30.0 * float DAY then
        sprintf "%d days ago" ts.Days
    elif delta < 12.0 * float MONTH then
        let months = int (float ts.Days / 30.0)

        if months <= 1 then
            "one month ago"
        else
            sprintf "%d months ago" months
    else
        let years = int (float ts.Days / 365.0)

        if years <= 1 then
            "one year ago"
        else
            sprintf "%d years ago" years
