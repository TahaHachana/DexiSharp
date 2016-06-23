namespace DexiSharp

open HttpClient
open Newtonsoft.Json
open System
open System.Security.Cryptography
open System.Text

module Helpers =

    let md5 (input : string) =
        use md5 = MD5.Create()
        input
        |> Encoding.UTF8.GetBytes
        |> md5.ComputeHash
        |> Seq.map (fun c -> c.ToString("x2"))
        |> Seq.reduce (+)

    let getRequest accessKey accountId url =
        createRequest Get url
        |> withHeader (Custom {name = "X-DexiIO-Access"; value = accessKey})  
        |> withHeader (Custom {name = "X-DexiIO-Account"; value = accountId})
        |> withHeader (Accept "application/json")
        |> withHeader (ContentType "application/json")
//        |> getResponseBody

    let postRequest accessKey accountId url =
        createRequest Post url
        |> withHeader (Custom {name = "X-DexiIO-Access"; value = accessKey})  
        |> withHeader (Custom {name = "X-DexiIO-Account"; value = accountId})
        |> withHeader (Accept "application/json")
        |> withHeader (ContentType "application/json")

    let deleteRequest accessKey accountId url =
        createRequest Delete url
        |> withHeader (Custom {name = "X-DexiIO-Access"; value = accessKey})  
        |> withHeader (Custom {name = "X-DexiIO-Account"; value = accountId})
        |> withHeader (Accept "application/json")
        |> withHeader (ContentType "application/json")
        |> getResponseCode

    let baseUrl = """https://api.dexi.io/"""

    let toDateTime (timestamp:int64) =
        DateTimeOffset.FromUnixTimeMilliseconds(timestamp)
            .UtcDateTime
            .ToLocalTime()

type RawExecution =
    {
        _id: string;
        state: string;
        starts: obj;
        finished: obj
    }

type ExecutionState =
    | Queued
    | Pending
    | Running
    | Failed
    | Stopped
    | Ok

type Execution =
    {
        _id: string;
        state: ExecutionState;
        starts: DateTime;
        finished: DateTime option
    }

    static member FromRawExecution (rawExecution : RawExecution) =
        let state =
            match rawExecution.state with
            | "QUEUED" -> Queued
            | "PENDING" -> Pending
            | "RUNNING" -> Running
            | "FAILED" -> Failed
            | "STOPPED" -> Stopped
            | _ -> Ok
        let starts = Helpers.toDateTime <| unbox<int64>(rawExecution.starts)
        let finished =
            match rawExecution.finished with
            | null -> None
            | timestamp ->
                unbox<int64>(timestamp)
                |> Helpers.toDateTime
                |> Some
        {_id = rawExecution._id
         state = state
         starts = starts
         finished = finished}


type RawExecutionResult =
    {
        rows: RawExecution [];
        offset: int;
        totalRows: int
    }
    
type ExecutionResult =
    {
        rows: Execution [];
        offset: int;
        totalRows: int
    }

type DexiClient(accountId, apiKey) =
    let accessKey = Helpers.md5 <| accountId + apiKey
    
    let getRequest' = Helpers.getRequest accessKey accountId
    let postRequest' = Helpers.postRequest accessKey accountId

    /// <summary>Fetches information about the state of an execution.</summary>
    /// <param name="executionId">The UUID of the execution.</param>
    member this.Get(executionId) =
        let url = Helpers.baseUrl + "executions/" + executionId
        getRequest' url
        |> getResponseBody
        |> fun x -> JsonConvert.DeserializeObject<RawExecution>(x)
        |> Execution.FromRawExecution

    /// <summary>Deletes an execution permanently.</summary>
    /// <param name="executionId">The UUID of the execution.</param>
    member this.Remove(executionId) =
        let url = Helpers.baseUrl + "executions/" + executionId
        Helpers.deleteRequest accessKey accountId url

    /// <summary>Returns the results of an execution. It is advised that you use some method of streaming to parse the result and insert it into your data store of choice.</summary>
    /// <param name="executionId">The UUID of the execution.</param>
    /// <param name="Format">The output format. Valid values are json, csv, xml and scsv.</param>
    member this.GetResult(executionId, streamConsumer, ?Format) =
        let format = defaultArg Format "json"
        let url = "https://api.dexi.io/executions/" + executionId + "/result?format=" + format
        getRequest' url
        |> getResponseStream(streamConsumer)

    /// <summary>Get execution result file contents. Response headers includes the content-type of the file.</summary>
    /// <param name="executionId">UUID of the execution.</param>
    /// <param name="fileId">UUID of the file.</param>
    member this.GetResultFile(executionId, fileId, streamConsumer) =
        let url = "https://api.dexi.io/executions/" + executionId + "/file/" + fileId
        getRequest' url
        |> getResponseStream(streamConsumer)

    /// <summary>Stop execution. Note that an execution does not stop immediately.</summary>
    /// <param name="executionId">The UUID of the execution.</param>
    member this.Stop(executionId) =
        let url = Helpers.baseUrl + "executions/" + executionId + "/stop"
        postRequest' url
        |> getResponseCode







//    member this.GetExecutions(runId, ?Offset, ?Limit) =
//        let offset = defaultArg Offset 0
//        let limit = defaultArg Limit 30
//        let url = Helpers.baseUrl + "runs/" + runId + "/executions?offset=" + string offset + "&limit="+ string limit
//        getRequest' url
//        |> getResponseBody
//        |> fun x -> JsonConvert.DeserializeObject<RawExecutionResult>(x)
//        |> fun x ->
//            let executions = x.rows |> Array.map Execution.FromRawExecution
//            {rows = executions
//             offset = x.offset
//             totalRows = x.totalRows}





//    member this.ExecuteWithInput(runId, body, ?Connect) =
//        let connect = defaultArg Connect false
//        let url = "https://api.dexi.io/runs/" + runId + "/execute/inputs?connect=" + (string connect).ToLower()
//        createRequest Post url
//        |> withHeader (Custom {name = "X-DexiIO-Access"; value = accessKey})  
//        |> withHeader (Custom {name = "X-DexiIO-Account"; value = accountId})
//        |> withHeader (Accept "application/json")
//        |> withHeader (ContentType "application/json")
//        |> withBodyEncoded body "UTF-8"
//        |> getResponseBody
//
//    member this.ExecuteSync(runId, ?Connect, ?Format, ?DeleteAfter) =
//        let connect = defaultArg Connect false
//        let format = defaultArg Format "json"
//        let deleteAfter = defaultArg DeleteAfter true
//        let url = "https://api.dexi.io/runs/" + runId + "/execute/wait?connect=" + (string connect).ToLower() + "&format=" + format + "&deleteAfter=" + (string deleteAfter).ToLower()
//        createRequest Post url
//        |> withHeader (Custom {name = "X-DexiIO-Access"; value = accessKey})  
//        |> withHeader (Custom {name = "X-DexiIO-Account"; value = accountId})
//        |> withHeader (Accept "application/json")
//        |> withHeader (ContentType "application/json")
//        |> getResponseBody
//
//    member this.GetLatestResult(runId, ?Format) =
//        let format = defaultArg Format "json"
//        let url = "https://api.dexi.io/runs/" + runId + "/latest/result?format=" + format
//        createRequest Get url
//        |> withHeader (Custom {name = "X-DexiIO-Access"; value = accessKey})  
//        |> withHeader (Custom {name = "X-DexiIO-Account"; value = accountId})
//        |> withHeader (Accept "application/json")
//        |> withHeader (ContentType "application/json")
//        |> getResponseBody
//
 