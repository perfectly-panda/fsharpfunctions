namespace fsharpfunctions

open System
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Host
open Microsoft.Extensions.Logging

module DataProcessing_1 =
    [<FunctionName("DataProcessing_1")>]
    let run([<TimerTrigger("0 */1 * * * *")>]myTimer: TimerInfo, log: ILogger) =
        let msg = sprintf "F# Time trigger function executed at: %A" DateTime.Now
        log.LogInformation msg
