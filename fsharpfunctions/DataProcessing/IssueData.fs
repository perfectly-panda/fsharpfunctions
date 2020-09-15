namespace FSharpFunctions
open System

type EntryType = CountOnly | ByPriority | ByService | BySubService

type Source = CSharp | FSharp

(*
    option = Some<T> | None
*)

type IssueData = {
    id: string
    Source: Source
    EntryType: EntryType //we know what possible values are
    Timestamp: DateTime
    TotalOpenIssues: int
    MissingTags: int
    CountByPriority: option<Map<string, int>> //immutable key-value store
    CountByService: option<Map<string, int>>
}