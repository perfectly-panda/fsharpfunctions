namespace FSharpFunctions
open System

type EntryType = CountOnly | ByPriority | ByService | BySubService

type Source = CSharp | FSharp

type SubService = {Name: string; Count: int}
type Service = {Name: string; Count: int; SubServiceCounts: option<Set<SubService>>}

type ServiceCount =
    | Map of option<Map<string, int>> 
    | ServiceSet of  option<Set<Service>>

type IssueDataV2 = {
    Id: string
    Source: Source
    EntryType: EntryType //we know what possible values are
    Timestamp: DateTime
    TotalOpenIssues: int
    MissingTags: option<int>
    CountByPriority: option<Map<string, int>> //immutable key-value store
    CountByService: ServiceCount
}